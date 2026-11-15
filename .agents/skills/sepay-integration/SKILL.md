---
name: sepay-integration
description: Implements and reviews the SePay VietQR payment flow for GroceryHub - payment code, QR URL, webhook authentication (HMAC-SHA256 or API key), response contract, idempotency, amount/account/code validation, late/duplicate/mismatch handling, reconciliation, refunds, sandbox vs production. Use for any payment, order-paid, webhook, QR or refund work.
---

# sepay-integration

## Goal
Money-safe payments: an order becomes paid only through a verified, matching SePay transaction. Spec: `docs/spec/11-SEPAY-INTEGRATION.md`. Official docs: https://developer.sepay.vn (re-check before coding; behavior may change).

## Facts verified in official docs (Nov 2026 research - re-verify)
- Webhook is an HTTP POST with fields: `id` (stable across retries), `gateway`, `transactionDate` (VN time), `accountNumber`, `subAccount`, `code` (payment code, may be null), `content`, `transferType` (`in`/`out`), `description`, `transferAmount` (integer VND), `accumulated`, `referenceCode`.
- Success response must be HTTP 200/201 with JSON `{"success": true}` within 30 seconds; anything else is failure. 8 delivery attempts (1 + 7 retries) over ~33 minutes when auto-retry is enabled. Webhooks can be lost if the endpoint is down longer; reconcile via the transactions API (poll every 15-30 min).
- Auth options: HMAC-SHA256 (recommended; headers `X-SePay-Signature: sha256=<hex>`, `X-SePay-Timestamp`; signed string `{timestamp}.{rawBody}`; reject if timestamp is > 5 min off), API key (`Authorization: Apikey <key>`), OAuth 2.0, none (test only). Use raw body bytes for HMAC, not re-serialized JSON.
- Payment code is extracted by SePay from the transfer content using the prefix/structure configured in SePay dashboard (Company -> General settings -> Payment code structure). Webhook may be configured to skip transactions without a code.
- QR image: `https://qr.sepay.vn/img?acc=<account>&bank=<bank short name>&amount=<vnd>&des=<url-encoded code>`; bank short names list: `https://qr.sepay.vn/banks.json`. Some banks need special memo/VA rules (e.g. VietinBank personal accounts need `SEVQR` in the memo; some banks use virtual-account/TKP). Read the bank-specific rules before choosing the receiving bank.
- Sandbox: `my.dev.sepay.vn` (needs activation by SePay). Webhook URLs must be public HTTPS (use a tunnel in dev).
- Security checklist from SePay: HTTPS, HMAC, IP whitelist, dedupe by transaction id, validate amount/account/code, store raw payload, reconcile periodically.

## Process
1. Read spec 11 and the current official pages (use web fetch if available; otherwise ask the user to paste). Note any difference in `docs/progress/QUESTIONS.md`.
2. Implement in order: payment code generator (unique, `GH` + 10 digits, matching SePay prefix config) -> PaymentAttempt + expiry -> QR payload endpoint -> webhook (verify -> persist inbox -> match -> transition) -> status endpoint -> expiry job -> reconciliation job -> anomaly review queue -> refund workflow.
3. Write the tests listed in spec 11 section "Test matrix" against real SQL Server; include signed requests built with the same HMAC algorithm.
4. Verify against SePay sandbox with the user's credentials when available; save real request/response logs (secrets redacted).

## Best practices
- Insert the raw event with a unique constraint on SePay `id` before any business logic; process inside one DB transaction; duplicates return success.
- Match by `code`; fall back to parsing `content` with the configured prefix regex when `code` is null.
- Treat `out` transactions as informational (refund matching only if the bank supports it).
- Keep a state transition table for Payment; every transition audited.

## Common mistakes
- Parsing JSON then recomputing HMAC (signature mismatch); comparing signatures with `==` (timing leak).
- Returning `{"status":"ok"}` or 202 (SePay treats as failure and retries).
- Accepting an amount that is "close enough"; ignoring `accountNumber`.
- Letting the frontend call an endpoint that marks the order paid.
- Auto-accepting late payments after stock release.
- Logging webhook secrets/headers.

## Checklist
- [ ] Signature + timestamp tests (valid, tampered body, old timestamp, missing header)
- [ ] Duplicate delivery test (same `id` twice, concurrently)
- [ ] Amount mismatch / unknown code / late arrival / overpay / underpay tests
- [ ] Reconciliation recovers a missed transaction
- [ ] No client-side confirmation path exists (code search evidence)
- [ ] Sandbox evidence or explicit note that it was not possible
