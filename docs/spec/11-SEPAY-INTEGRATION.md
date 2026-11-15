# 11 — SePay Integration (VietQR + Webhook)

Official docs: https://developer.sepay.vn (older mirror https://docs.sepay.vn). Facts below were read on 14 Nov 2026; **Phase 10 starts by re-reading the pages listed in §10 and recording differences in `docs/progress/QUESTIONS.md`.**

## 1. Principles
1. The only way an order becomes paid online is a **verified SePay webhook (or reconciliation record) that matches payment code, amount and receiving account**.
2. The frontend only displays server-provided status. No endpoint, UI action or query string can mark a payment paid.
3. Everything abnormal is recorded and routed to a human review queue; nothing is silently accepted or dropped.
4. Processing is idempotent and race-safe.
5. Secrets live only in environment variables.

## 2. Preconditions the user must complete (PM tells the user in Vietnamese)
1. Create a SePay account (sandbox: `my.dev.sepay.vn`, needs activation by SePay support; production: `my.sepay.vn`) and link the receiving bank account. Check the bank-specific notes for QR (e.g. VietinBank personal accounts need `SEVQR` in the memo; some banks use virtual accounts/TKP).
2. In SePay: **Company → General settings → Payment code structure**: configure prefix `GH` and the code format matching `GH` + 10 digits (confirm the options the dashboard offers; adjust §3 if different).
3. Create a **Webhook**: URL `https://<domain>/api/v1/payments/sepay/webhook`, event "money in", filter by prefix `GH`, authentication **HMAC-SHA256** (copy the secret once into `SEPAY_WEBHOOK_SECRET`), content type JSON, enable auto-retry. Dev: expose the local API through an HTTPS tunnel (SePay rejects non-HTTPS/private URLs).
4. Create an **API token** for reconciliation (`SEPAY_API_TOKEN`).
5. Add SePay's published IP list to the proxy allowlist (check https://developer.sepay.vn/vi/dia-chi-ip).

## 3. Payment code and QR
- Code format: `GH` + 10 digits from SEQUENCE `payments.PaymentCodeSeq` (start `1000000001`), no separators (banks may strip them). One unique code per **payment attempt**.
- QR URL (server builds it, never the client): `https://qr.sepay.vn/img?acc={AccountNumber}&bank={BankShortName}&amount={Amount}&des={PaymentCode}` (URL-encode values; bank short name from `https://qr.sepay.vn/banks.json`). The API returns `{url, bank, accountNumber, accountName, amount, content}`; the page shows both the image and the text details (copyable) because some customers type manually.
- CSP allows `img-src https://qr.sepay.vn`. (Optional later: generate the QR locally from the VietQR EMV payload to remove the third-party image dependency.)
- Expiry: `QrExpiryMinutes` (default 30) shown to the customer; the server cancels only after an extra `ExpiryGraceMinutes` (default 5) to tolerate bank/webhook latency.

## 4. Data model (see doc 06 §payments)
`Payments` (1 per order) → `PaymentAttempts` (many; unique `PaymentCode`) ; `WebhookEvents` (inbox; unique `(Provider, ProviderTransactionId)`) → `PaymentTransactions` (normalized transaction + `MatchStatus` + `ReviewStatus`) ; `Refunds` ; `CodCollections`. `MatchStatus` values: `Matched, UnknownCode, WrongAccount, AmountMismatch, Underpaid, Overpaid, LateAfterExpiry, DuplicatePayment, OutgoingIgnored`.

## 5. Webhook endpoint algorithm
`POST /api/v1/payments/sepay/webhook` (anonymous at the HTTP-auth layer, authenticated by signature)
1. Enforce body size limit (e.g. 64 KB) and read the **raw bytes**.
2. **Authenticate** (outside Development this is mandatory):
   - Mode `Hmac`: require headers `X-SePay-Timestamp` (Unix seconds) and `X-SePay-Signature` (`sha256=<hex>`). Reject if |now − timestamp| > 300 s. Compute HMAC-SHA256 over `"{timestamp}.{rawBody}"` with `SEPAY_WEBHOOK_SECRET`; compare in constant time (`CryptographicOperations.FixedTimeEquals`).
   - Mode `ApiKey` (fallback only): `Authorization: Apikey <key>` constant-time compare.
   - Failure → 401 `{"success": false}`; log a security event (no secrets), no DB writes except an optional rate-limited security log.
3. Parse JSON → DTO (`id, gateway, transactionDate, accountNumber, subAccount, code, content, transferType, description, transferAmount, accumulated, referenceCode`); invalid → 400.
4. **Inbox insert**: `INSERT WebhookEvents(Provider='SEPAY', ProviderTransactionId=id, RawBody, SignatureValid, Status='Received')`. On unique violation: load existing; if `Processed`/`Ignored` → return 200 `{"success": true}` (duplicate). If `Received`/`Failed` → continue (re-process) guarded by a conditional status update to `Processing` (only one worker wins).
5. If `transferType != "in"` → mark `Ignored` (`OutgoingIgnored`) → success.
6. Determine payment code: `code` if present else regex `GH\d{10}` over `content` (case-insensitive, banks may uppercase/strip).
7. Validate and decide inside **one DB transaction** (rowversion on Payment/Order/Stock):

| Condition | Result | MatchStatus | Review |
|---|---|---|---|
| no code found | record only | UnknownCode | NeedsReview |
| `accountNumber` ≠ configured account | record only | WrongAccount | NeedsReview |
| code not found in attempts | record only | UnknownCode | NeedsReview |
| attempt (Open or Superseded) of an order in `PendingPayment`, not expired, `transferAmount == payment.Amount` | **Payment=Paid, attempt=Paid, order=Confirmed**, history + audit + outbox event | Matched | None |
| order already Paid / attempt already Paid | record only | DuplicatePayment | NeedsReview (refund candidate) |
| `transferAmount < Amount` | record only; order stays PendingPayment | Underpaid | NeedsReview |
| `transferAmount > Amount` | record only; order stays PendingPayment | Overpaid | NeedsReview |
| order Cancelled/Expired (after grace) | record only | LateAfterExpiry | NeedsReview |

8. Mark event `Processed`, return 200 `{"success": true}` **well inside 30 s**. On unexpected exception: mark `Failed`, return 500 (SePay retries: 8 deliveries over ~33 min).
9. Never log the secret or full headers; raw body only in `WebhookEvents`.

## 6. Anomaly review queue (admin UI, permission `payments.review`)
Actions on a `NeedsReview` transaction (each audited; requires note):
- **AttachToOrder** (Manager): accept the transaction against a specific order (Underpaid top-up, Overpaid accept, or Late payment **revive** — allowed only if the order can be reopened and stock can be re-reserved; otherwise the system refuses and suggests Refund).
- **Refund**: creates a refund request for the transaction amount.
- **Dismiss**: mark resolved (e.g. unrelated deposit) with reason.

## 7. Expiry and reconciliation jobs
- `PaymentExpiryService` (every minute): attempts with `ExpiresAt + grace < now` and order `PendingPayment` → attempt `Expired`, payment `Expired`, order `Cancelled(reason=PaymentExpired)`, stock reservation released, coupon redemption released. Single-winner via conditional updates.
- `ReconciliationService` (every `Reconciliation:IntervalMinutes`, default 15): fetch recent transactions from the SePay transactions API (verify endpoint/params in official docs; Bearer token), and for each id missing from `WebhookEvents` feed it through steps 4–8 with `source=Reconciliation` (no signature). Also detect "Failed" events older than 10 min and retry them. Record counts in logs/metrics. This covers outages longer than SePay's retry window.

## 8. COD and refunds
- **COD**: allowed when `total ≤ CodMaxAmount`. Payment `Unpaid`. On transition to `Delivered` the staff enters collected cash; if equal to total → `CodCollection` row, Payment `Paid`; if different → blocked with `COD_AMOUNT_MISMATCH` and needs Manager. Manager marks cash handover.
- **Refund workflow** (BR-15): `Requested` (by `refunds.request`: reason, amount ≤ paid − refunded) → `Approved` or `Rejected` (by a **different** user with `refunds.approve`) → store manually transfers money back through the bank → `Completed` with `BankReference` → Payment becomes `PartiallyRefunded`/`Refunded`. SePay does not execute payouts; never claim automatic refunds. Optional (C): if the bank reports outgoing transfers via webhook, suggest matching a refund by memo `HT<refundNumber>` — never auto-complete.
- Cancelling a **Paid** order creates a refund request automatically.

## 9. Configuration (appsettings + env; validated on start)
```
SePay:Environment           = Sandbox | Production
SePay:AccountNumber, BankShortName, AccountName     # receiving account shown on QR page
SePay:PaymentCodePrefix     = GH
SePay:Webhook:AuthMode      = Hmac | ApiKey          # Production: must not be None
SePay:Webhook:HmacSecret    -> env SEPAY_WEBHOOK_SECRET
SePay:Webhook:ApiKey        -> env SEPAY_WEBHOOK_APIKEY (fallback)
SePay:Webhook:TimestampToleranceSeconds = 300
SePay:Webhook:AllowedIps    = [ ... ]                # enforced at proxy; optional second check in app
SePay:Api:BaseUrl, Token    -> env SEPAY_API_TOKEN
SePay:QrExpiryMinutes = 30, ExpiryGraceMinutes = 5
SePay:Reconciliation:IntervalMinutes = 15, LookbackHours = 24
Payments:CodMaxAmount = 2000000
```
Startup validation fails if `Environment=Production` and any secret/account value is missing or placeholder. Sandbox and Production use **different** accounts, secrets and webhook URLs. A `GET /health/ready` check reports payments configuration state (not secrets).
Development-only simulator: `POST /dev/sepay/simulate` (registered only when `IsDevelopment()`; 404 otherwise — tested) builds a payload and signs it with the configured secret, then calls the **real** webhook pipeline. It never bypasses validation.

## 10. Re-verify before coding (official pages)
- Webhooks overview/quick start, integrate webhook, create webhook, authentication, security, error handling (retry), reconciliation, QR generation, test mode (sandbox) — all under https://developer.sepay.vn/vi/sepay-webhooks/… and https://developer.sepay.vn/vi/tien-ich-khac/tao-qr-code. Also the transactions API under "SePay API". Note any change in header names, retry schedule, response contract or QR parameters.

## 11. Test matrix (all automated against real SQL Server; signed requests built with the real algorithm)
| ID | Scenario | Expected |
|---|---|---|
| T-PAY-01 | valid signed webhook, exact amount | Payment Paid, Order Confirmed, 1 history row, audit entry, response `{"success":true}` |
| T-PAY-02 | same webhook twice sequentially | second is no-op, success response, DB unchanged |
| T-PAY-03 | same webhook ×10 concurrently | exactly one state change |
| T-PAY-04 | tampered body | 401, no DB rows |
| T-PAY-05 | wrong secret / missing signature | 401 |
| T-PAY-06 | timestamp 10 min old | 401 |
| T-PAY-07 | API-key mode right/wrong key | 200 / 401 |
| T-PAY-08 | amount lower / higher | recorded Underpaid / Overpaid, order unchanged, NeedsReview |
| T-PAY-09 | unknown code, no code, wrong account | recorded with proper MatchStatus, NeedsReview |
| T-PAY-10 | `transferType=out` | Ignored, success |
| T-PAY-11 | arrives after expiry+grace | LateAfterExpiry, order stays Cancelled |
| T-PAY-12 | arrives within grace after nominal expiry | Matched |
| T-PAY-13 | superseded attempt code paid | Matched |
| T-PAY-14 | second payment on paid order | DuplicatePayment, NeedsReview |
| T-PAY-15 | DB failure mid-processing (fault injection at boundary) | 500, event Failed, retry succeeds once |
| T-PAY-16 | reconciliation recovers missed transaction | order confirmed once |
| T-PAY-17 | expiry job cancels expired order, releases stock and coupon | verified in DB |
| T-PAY-18 | refund: same user requests and approves | `REFUND_SAME_APPROVER` |
| T-PAY-19 | refund amount > paid − refunded | `REFUND_INVALID_AMOUNT` |
| T-PAY-20 | `/payments/{code}/status` by non-owner | 404/403; by owner reflects server state |
| T-PAY-21 | search codebase for any client-side paid path | none (script output saved) |
| T-PAY-22 | `/dev/sepay/simulate` in Production config | 404 |
| T-PAY-23 | webhook oversize body | 413/400 |
| T-PAY-24 | COD delivered with wrong cash amount | blocked |
Sandbox verification with real SePay (user credentials) is recorded as evidence in Phase 10; if impossible, state it explicitly in the phase report.

## 12. Operational runbook (docs/ops/payments-runbook.md, written in Phase 10/12)
How to rotate the HMAC secret (two-secret overlap window), how to replay a failed event (admin tool/CLI using stored raw payload), how to handle a customer who paid but the order expired, how to read the review queue, daily reconciliation checks.
