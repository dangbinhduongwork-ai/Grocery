# GroceryHub — Phase 10 Completion Report: SePay VietQR + Webhook Integration

**Date:** 2026-11-15  
**Phase:** 10 — SePay VietQR + Webhook  
**Lead Agent:** `project-manager`  
**Participating Roles:** `payment-specialist`, `backend-developer`, `database-engineer`, `frontend-developer`, `qa-engineer`, `security-reviewer`, `devops-engineer`  
**Status:** **GO** (All acceptance criteria satisfied with real execution evidence)

---

## 1. Executive Summary

Phase 10 delivers full online payment capabilities for GroceryHub using **SePay VietQR** and cryptographic webhook notifications, strictly adhering to the fundamental security rule: **Zero Client-Side Confirmation** (no client endpoint or frontend view can ever mark an order or payment as paid).

Key accomplishments:
- **Database Schema Deepening:** Created and migrated `PaymentAttempt`, `WebhookEvent`, `PaymentTransaction`, and `Refund` tables with strict unique constraints (`UQ_WebhookEvents_Provider_TxId`, `UQ_PaymentAttempts_PaymentCode`) and a SQL Server CHECK constraint (`CK_Refunds_TwoPersonRule`) enforcing `[DecidedBy] IS NULL OR [DecidedBy] <> [RequestedBy]`.
- **Cryptographic Webhook Security:** Implemented `SePaySignatureValidator` verifying `X-SePay-Signature` (HMAC-SHA256) and `X-SePay-Timestamp` (300-second replay window), supporting zero-downtime secret rotation (`HmacSecretPrevious`) and `ApiKey` fallback. Webhook payloads $> 64$ KB are rejected upfront with `413 Payload Too Large`.
- **Anomaly Detection & Review Queue:** Implemented 8-case matching matrix (`Matched`, `DuplicatePayment`, `LateAfterExpiry`, `Underpaid`, `Overpaid`, `UnknownCode`, `WrongAccount`, `OutgoingIgnored`). Anomalous transactions land in `payments.PaymentTransactions` with `ReviewStatus = NeedsReview` for Manager resolution.
- **Background Automation:**
  - `PaymentExpiryService`: Periodically sweeps payment attempts beyond 30-minute window plus 5-minute grace period, cancels uncompleted orders, and releases stock reservations and coupon claims.
  - `ReconciliationService`: Polls SePay API v2 (`/v2/transactions/list`) every 15 minutes to ingest missing transactions and reconcile discrepancies.
- **Two-Person Refund Workflow (`BR-15`):** 4-step workflow (`Requested` $\rightarrow$ `Approved` $\rightarrow$ Bank Transfer $\rightarrow$ `Completed`) with non-negotiable four-eyes enforcement: the requester cannot approve or complete their own refund request.
- **Storefront & Admin UI:**
  - `/payment/:paymentCode`: Dynamic VietQR payment page with server QR image, copyable transfer content, bank details, countdown timer, exponential backoff status polling, and retry attempt generation.
  - `/admin/payments`: Back-office transaction queue, audit logs, and anomaly resolution modal (`AttachToOrder`, `Refund`, `Dismiss`).
  - `/admin/refunds`: Two-person refund approval queue and manual bank reference entry.
- **DevOps & Operations:** Added `deploy/nginx/sepay-webhook.conf` with SePay egress IP allowlist and rate limiting; authored operational runbook `docs/ops/payments-runbook.md`.

---

## 2. Requirements & Business Rules Verification

| Requirement / Rule | Description | Verification Method | Status |
|---|---|---|---|
| `FR-CUS-013` | VietQR payment page (QR image, amount, bank info, countdown, live status polling, retry) | `PaymentPage.tsx`, Vite build, API integration tests | **Verified** |
| `FR-ADM-011` | Admin payment management: transaction list, match status, review queue, refund workflow | `AdminPaymentsPage.tsx`, `AdminRefundsPage.tsx`, API tests | **Verified** |
| `BR-06` | Payment methods `COD` and `SEPAY_QR` with auto-expiry and stock release | `PaymentExpiryService.cs`, `T_PAY_17` | **Verified** |
| `BR-15` | Two-person rule for refunds (`refunds.approve` $\neq$ requester) | `CK_Refunds_TwoPersonRule`, `PaymentService.cs`, `T_PAY_18` | **Verified** |
| `NFR-003` | Security: HMAC-SHA256, replay window, secret rotation, zero client-paid paths | `T_PAY_04..07`, `T_PAY_21`, static scan evidence | **Verified** |
| `NFR-008` | Data integrity: inbox idempotency, unique constraints, concurrency safety | `T_PAY_02..03` (10 concurrent duplicate webhooks) | **Verified** |

---

## 3. Automated Test Results (Real Runs)

Every test was executed against real SQL Server (LocalDB with transaction rollbacks and constraints) and compiled C#/.NET 10 assemblies:

1. **Unit Tests:**
   ```text
   dotnet test tests/GroceryHub.UnitTests/
   Passed! - Failed: 0, Passed: 241, Skipped: 0, Total: 241, Duration: 1 s
   ```
2. **Architecture Tests:**
   ```text
   dotnet test tests/GroceryHub.ArchitectureTests/
   Passed! - Failed: 0, Passed: 6, Skipped: 0, Total: 6, Duration: 18 s
   ```
3. **Integration Tests (Full Suite):**
   ```text
   dotnet test tests/GroceryHub.IntegrationTests/
   Passed! - Failed: 0, Passed: 201, Skipped: 0, Total: 201, Duration: 5 m 18 s
   ```
4. **Frontend TypeScript & Production Build:**
   ```text
   cd web && npm run build
   ✓ 2273 modules transformed.
   dist/assets/PaymentPage-RJPb3Ufg.js       15.00 kB │ gzip: 4.24 kB
   dist/assets/AdminPaymentsPage-DVDjvGdK.js  13.96 kB │ gzip: 4.03 kB
   dist/assets/AdminRefundsPage-B9gH7e5d.js   17.06 kB │ gzip: 4.60 kB
   ✓ built in 590ms (0 errors)
   ```

**Total Backend Test Suite:** 448 tests, **100% GREEN**.

---

## 4. T-PAY Test Matrix (docs/spec/11-SEPAY-INTEGRATION.md §11)

| ID | Scenario | Result | Evidence Ref |
|---|---|---|---|
| `T-PAY-01` | Valid signed webhook $\rightarrow$ `Paid` & `Confirmed` | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-02` | Sequential duplicate webhook hits unique constraint $\rightarrow$ Idempotent 200 OK | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-03` | 10 concurrent duplicate webhooks $\rightarrow$ 1 state change, 10x 200 OK | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-04` | Tampered raw JSON body fails signature | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-05` | Wrong HMAC secret or missing headers fails validation | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-06` | Timestamp older than 300s fails replay protection | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-07` | ApiKey fallback mode: valid key vs invalid key | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-08` | Transfer amount mismatch (Underpaid/Overpaid) lands in review queue | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-09` | Unknown payment code or wrong account lands in review queue | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-10` | Outgoing transfer (`transferType="out"`) ignored, returns 200 OK | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-11` | Late webhook after expiry+grace lands in review queue | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-12` | Webhook within 5m grace period confirms pending order | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-13` | Superseded attempt code transitions to paid; 5-attempt limit enforced (409) | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-14` | Second transfer for already paid order lands in review queue | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-15` | Failed webhook recorded with RawBody, retry claims and succeeds | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-16` | ReconciliationService polls SePay API and recovers missed transaction | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-17` | PaymentExpiryService sweeps expired attempts and cancels order with stock release | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-18` | Two-person refund rule: same user cannot approve own refund (409 Conflict) | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-19` | Refund amount exceeding remaining paid amount rejected | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-20` | Customer can only query their own payment status (owner-only access) | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-21` | Static codebase scan proves zero client-callable mark-paid paths | **PASS** | `docs/evidence/phase-10/no-client-paid-scan.txt` |
| `T-PAY-22` | Development-only simulator returns 404 in Production configuration | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-23` | Oversized webhook payload (>64KB) rejected with 413 Payload Too Large | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |
| `T-PAY-24` | Staff COD order delivery blocked if codAmount does not match order total | **PASS** | `docs/evidence/phase-10/test-matrix-evidence.txt` |

---

## 5. Mutation Sanity Check Evidence

To prove that the tests actively defend the critical business invariants and do not merely pass vacuously, two deliberate mutations were introduced and verified:

1. **Mutation A (Bypassing Signature Validation):**
   - In `SePaySignatureValidator.ValidateHmac`, returned `true` unconditionally.
   - Run: `dotnet test tests/GroceryHub.UnitTests/ --filter "FullyQualifiedName~T_PAY_04"`
   - Output:
     ```text
     [FAIL] GroceryHub.UnitTests.Payments.SePaySignatureAndUrlTests.T_PAY_04_TamperedRawJsonBody_SignatureValidationFails
     Assert.False() Failure. Expected: False, Actual: True.
     ```
   - Restored and re-verified: **Passed**.

2. **Mutation B (Bypassing Amount Check):**
   - In `PaymentService.ProcessWebhookAsync`, disabled `payload.TransferAmount < attempt.Payment.Amount`.
   - Run: `dotnet test tests/GroceryHub.IntegrationTests/ --filter "FullyQualifiedName~T_PAY_08"`
   - Output:
     ```text
     [FAIL] GroceryHub.IntegrationTests.Payments.SePayPaymentIntegrationTests.T_PAY_08_AmountMismatch_UnderpaidAndOverpaidRecordedForReview
     Assert.Equal() Failure: Strings differ. Expected: "PendingPayment", Actual: "Confirmed".
     ```
   - Restored and re-verified: **Passed**.

---

## 6. Truthfulness Disclosure: Sandbox Verification Status

Per Phase 10 charter rule 2 and acceptance criteria item 7:
- All automated test matrix scenarios (`T-PAY-01` through `T-PAY-24`), signature verifications, idempotency locks, anomaly queues, and refund constraints have been 100% verified against real SQL Server and the signed webhook simulator.
- Because live SePay merchant banking credentials (account number and live API token) were not provided by the user in `.env` for this execution session, live external banking verification against `https://qr.sepay.vn` was not conducted against real money transfers.
- **Status:** **AUTOMATED TEST SIMULATOR VERIFIED / LIVE SANDBOX CREDENTIALS NOT PROVIDED (EXPLICITLY DISCLOSED)**.

---

## 7. Deliverables & Documentation Created

1. `src/GroceryHub.Modules.Payments/`:
   - `Configuration/SePayOptions.cs` + `SePayOptionsValidator.cs`
   - `Contracts/PaymentDtos.cs`
   - `Application/VietQrUrlBuilder.cs`
   - `Application/SePaySignatureValidator.cs`
   - `Application/PaymentService.cs`
   - `Infrastructure/PaymentExpiryService.cs`
   - `Infrastructure/ReconciliationService.cs`
   - `Api/PaymentsEndpoints.cs`
   - `Api/PaymentsModuleExtensions.cs`
2. `src/GroceryHub.Persistence/`:
   - `Entities/{PaymentAttempt.cs, WebhookEvent.cs, PaymentTransaction.cs, Refund.cs}`
   - `Configurations/{PaymentAttemptConfiguration.cs, WebhookEventConfiguration.cs, PaymentTransactionConfiguration.cs, RefundConfiguration.cs}`
   - Migration `20261115144628_AddPaymentsDeepening.cs`
3. `web/src/`:
   - `storefront/pages/PaymentPage.tsx`
   - `admin/pages/AdminPaymentsPage.tsx`
   - `admin/pages/AdminRefundsPage.tsx`
   - `shared/ui/Card.tsx`
4. `deploy/nginx/sepay-webhook.conf`
5. `docs/ops/payments-runbook.md`
6. `docs/evidence/phase-10/`:
   - `test-matrix-evidence.txt`
   - `no-client-paid-scan.txt`
7. Progress & Traceability:
   - `docs/progress/PROGRESS.md`
   - `docs/progress/TRACEABILITY.md`

---

## 8. Gate 10 Decision: GO

All criteria defined in `docs/phase-prompts/phase-10-sepay.md` have been met. Code compiles with zero warnings-as-errors, all 448 tests pass, security checks and mutation tests are green, and the operational runbook is complete.

Phase 10 is signed off as **GO**. The project is ready for **Phase 11 (Testing and Security Hardening)**.
