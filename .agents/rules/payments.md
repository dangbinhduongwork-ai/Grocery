---
trigger: glob
globs: "src/GroceryHub.Modules.Payments/**, src/**/Payments/**, web/**/payment/**, web/**/checkout/**"
description: "Rules for the SePay payment flow, webhook, idempotency, refunds."
---

# Payment rules (details: docs/spec/11-SEPAY-INTEGRATION.md)

- Webhook response contract (SePay): HTTP 200/201 with body exactly `{"success": true}` within 30 s, otherwise SePay treats it as failed and retries (8 deliveries over ~33 minutes). Return `{"success": true}` for duplicates and for events intentionally ignored (e.g. outgoing transfers); return non-2xx only for genuine processing failures that should be retried or for authentication failures.
- Verify authentication first: HMAC-SHA256 over `{timestamp}.{rawBody}` using `X-SePay-Timestamp` (reject if older than 5 minutes) and compare `X-SePay-Signature` (`sha256=<hex>`) in constant time; fall back to `Authorization: Apikey <key>` only if configured. Read the RAW body bytes before JSON parsing.
- Persist the raw event first (unique on SePay transaction `id`), then process. Reprocessing the same `id` must be a no-op that still returns success.
- Match order by `code`; validate `transferType = in`, `accountNumber` equals configured account, `transferAmount` equals the order amount exactly, order is `PendingPayment` and not expired. Anything else is recorded with a match status (`UnknownCode`, `AmountMismatch`, `Overpaid`, `Underpaid`, `LateAfterExpiry`, `Duplicate`) and routed to the manual review queue; never auto-accept.
- State changes (payment -> Paid, order -> Confirmed) happen in one DB transaction with optimistic concurrency. No partial updates.
- The frontend only reads status from the server. No client-side "mark as paid" path may exist in code or API.
- A reconciliation job (every 15–30 min) pulls transactions from the SePay API and feeds missing ones through the same pipeline. Credentials come from env; never log them.
- Refunds are a workflow (request -> manager approval -> bank transfer executed manually -> completed with bank reference). Requester and approver must differ. Everything is audited.
- Test vs production: separate SePay accounts/keys/webhook URLs; environment switch in configuration; a development-only simulator may call the real signed webhook endpoint but is compiled out/unregistered outside Development.
