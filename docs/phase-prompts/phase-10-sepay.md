# Phase 10 — SePay VietQR + Webhook

**Goal:** Secure online payments exactly as specified in `docs/spec/11-SEPAY-INTEGRATION.md`, including anomaly review, expiry, reconciliation and refunds, with frontend payment page and admin queue.
**Agents:** payment-specialist (lead) → backend-developer (order contracts) → frontend-developer → qa-engineer → security-reviewer (mandatory); devops (proxy/IP allowlist); PM.
**Inputs:** FR-CUS-013, FR-ADM-011, BR-06, BR-15, NFR-003, NFR-008; docs 11, `11-SEPAY-DIFFS.md`; user credentials (sandbox) in `.env`.
**Outputs:** Payments module complete, payment page, admin payments UI, jobs, tests T-PAY-01..24, `docs/ops/payments-runbook.md`, sandbox evidence.
**Dependencies:** Phase 09 GO; user has completed the SePay preconditions (doc 11 §2) or accepted sandbox-less verification.

## Prompt to paste into Antigravity
```text
You are the Project Manager. PHASE 10 — SEPAY. Requirements: FR-CUS-013, FR-ADM-011, BR-06, BR-15. Load skills sepay-integration, dotnet-api, efcore-sqlserver, security-review. Obey .agents/rules/payments.md.

0. Before any code: payment-specialist re-opens the official SePay pages (doc 11 §10), updates docs/spec/11-SEPAY-DIFFS.md and, if reality differs from doc 11, opens a CR to the PM (do not silently diverge). Ask the user (Vietnamese) to confirm SePay preconditions: receiving bank/account, payment-code prefix configured as GH..., webhook URL + HMAC secret placed in .env (never in chat), API token. If the user cannot provide sandbox credentials, continue with automated tests only and mark sandbox verification as NOT DONE in the report.
1. payment-specialist: implement per doc 11: payment-code sequence/generator, PaymentAttempt + QR URL builder (server-side), enable method SEPAY_QR (flag Payments:SepayEnabled=true only when config validates), POST /me/orders/{id}/payment-attempts, GET /payments/{code}/status (owner-only, read-only), webhook endpoint (raw-body HMAC + timestamp + API-key fallback + body size limit), inbox idempotency, validation/matching table, anomaly records, PaymentExpiryService (+grace), ReconciliationService (verify the SePay transactions API in docs; Bearer token from env), COD collection with mismatch guard, refund workflow with two-person rule, review-queue service (AttachToOrder with revive rules, Refund, Dismiss), audit entries, configuration + startup validation, development-only simulator (/dev/sepay/simulate; 404 outside Development).
2. database-engineer: payments schema configurations/migrations (unique constraints), indexes, concurrency tokens.
3. backend-developer: Ordering side of the port (Confirm on payment, Cancel on expiry, revive, refund trigger on paid-cancel), OpenAPI update.
4. frontend-developer: Payment page per docs/spec/08 recipe (QR image from server URL, copyable content/code, bank table, countdown, polling with backoff, success/expired/anomaly states, 'Tôi đã chuyển khoản' only forces a status refetch), checkout QR option, order tracking payment states, admin Payments (transactions, review queue, refunds with approval screens).
5. qa-engineer: implement T-PAY-01..24 from doc 11 §11 as real integration tests with signed requests built by a test helper that re-implements the documented HMAC independently of production code; E2E J2/J3/J5 (QR happy path with signed webhook posted to the running API, expiry, anomaly + refund approval by a different user).
6. devops-engineer: nginx webhook location (IP allowlist placeholder, body limit, rate limit), .env.example keys.
7. security-reviewer: /wf-security-review focused on payments: negative webhook tests with real requests, secret handling, no client path to paid, logging review, simulator unreachable in Production config. Mandatory sign-off.
8. If sandbox credentials exist: perform a real sandbox payment end-to-end and save sanitized logs/screenshots; otherwise state explicitly 'sandbox verification NOT DONE'.
9. You: integrate, clean-checkout, verify-reality (must show no client-side paid path), /wf-progress-review, write phase-10-report.md and docs/ops/payments-runbook.md.
```

## Acceptance criteria
- T-PAY-01..24 all pass with real SQL Server (summary lines saved); mutation sanity: removing the amount check or the signature check makes tests fail (evidence).
- Frontend contains no code path that marks payment paid (search evidence); payment page reflects only server state.
- Unauthenticated/invalid webhook never changes the DB; duplicates are no-ops; concurrent duplicates produce one state change.
- Expiry cancels order and releases stock + coupon; late-payment cases land in the review queue.
- Reconciliation recovers a missing transaction in a test.
- Production config refuses to start with missing/placeholder SePay secrets (test).
- Security reviewer sign-off recorded; sandbox status stated truthfully.

## How to verify
Post a correctly signed sample payload with your secret (use the dev simulator or a script), watch order go Confirmed; repeat → nothing changes; change one byte of the body → 401; pay a different amount → lands in review queue.

## Gate to Phase 11
All PASS with evidence incl. security sign-off; PM GO.
