# Phase 07 — Customers/Addresses, Shipping, Coupons, Cart, Orders

**Goal:** Complete COD purchasing flow on the backend: addresses (2-level VN model), shipping fees, coupons, server-side carts, checkout with stock reservation, order state machine, idempotency. QR payments are enabled in Phase 10 (until then `SEPAY_QR` is reported as unavailable by the API with a clear error/flag — not faked).
**Agents:** database-engineer ‖ backend-developer → qa-engineer → security-reviewer; PM.
**Inputs:** FR-CUS-005, 008, 009, 010, 011, 012, 014, 015; FR-ADM-007, FR-ADM-012 (coupons), FR-ADM-017 (shipping settings); FR-STF-001..003; BR-02..BR-08, BR-10, BR-16, BR-17; spec 05 §9–11.
**Outputs:** Customers, Promotions, Cart, Ordering (+ minimal Payments for COD) modules; migrations; vendored admin-unit dataset with licence record; tests; evidence.
**Dependencies:** Phase 06 GO.

## Prompt to paste into Antigravity
```text
You are the Project Manager. PHASE 07 — CART & ORDER. Implement in four internal steps, each with its own mini-gate (build+tests green before the next). Load skills ecommerce-business, dotnet-api, efcore-sqlserver.

7A Customers & shipping (database-engineer + backend-developer):
 - Import the Vietnam 2-level administrative dataset (34 provinces/cities, ~3,321 wards/communes) from a source whose licence you verify (candidate: ThangLeQuoc/vietnamese-provinces-database SQL Server/JSON). Record source, version, licence in shared.AdminUnitDataVersion and docs. Seed idempotently. Endpoints GET /locations/provinces and /locations/provinces/{code}/wards (cacheable).
 - Addresses CRUD (max 10, one default enforced by filtered unique index, VN phone validation, ownership checks), profile endpoints.
 - Shipping zones/rates (admin CRUD, settings), POST /shipping/quote with free-shipping threshold and serviceable check.
7B Promotions: coupons CRUD (admin), validation rules BR-04, redemption tracking, usage report; coupon preview in cart/checkout quote.
7C Cart: guest cart cookie gh_cart, user cart, merge on login (hook into login), stock/price revalidation, max quantity, coupon on cart; endpoints per doc 07.
7D Ordering: POST /checkout/quote; POST /checkout/orders with Idempotency-Key (store key + request hash + response), single transaction: validate cart, re-price, reserve stock atomically, redeem coupon atomically, snapshot address/prices, compute totals server-side, create order (Placed for COD) + Payment(COD) + history, clear cart, outbox event. Order number via SEQUENCE. State machine per docs/spec/state-machines.md with guards/permissions; customer cancel rules BR-07 (release stock & coupon); staff transitions (confirm, preparing, packed, shipping (stock commit -> Sale movements with cost snapshot), delivered (COD cash record), delivery failed); admin order list/detail/assign/note/pick-list/delivery-note data; reservation sweeper for abandoned states; reorder endpoint. For paymentMethod=SEPAY_QR return 409 PAYMENT_METHOD_UNAVAILABLE until Phase 10 enables it by configuration (feature flag Payments:SepayEnabled=false) and expose availablePaymentMethods in the quote.
QA: concurrency tests (last unit across 20 parallel checkouts; coupon usage limit=1 with 10 parallel; duplicate Idempotency-Key returns same order, different body -> 409), full state-machine matrix (allowed/denied transitions per role), totals/discount allocation fixtures, cancel restores stock and coupon, delivered order enables review eligibility query, guest cart merge cases, address 11th rejected, non-serviceable province rejected, COD limit, ownership/IDOR tests for carts/addresses/orders, RBAC matrix extension.
Security-reviewer: IDOR/price-tampering review (client cannot send prices/totals), idempotency abuse, rate limits.
You: integrate, clean-checkout, verify-reality, /wf-progress-review, report.
```

## Acceptance criteria
- A scripted API run (saved HTTP log) performs: login → add to cart → quote → checkout COD → staff confirm → … → Delivered; stock and movements match expectations at each step.
- Concurrency and idempotency tests pass (numbers shown).
- Client-supplied totals/prices are ignored (tamper test).
- All transitions are covered by tests; invalid ones return `ORDER_INVALID_TRANSITION`.
- Address data version/licence recorded; ward count and province count asserted in a test (34 provinces; ward count equals dataset).
- Coupon rules BR-04 each have a test.
- `SEPAY_QR` returns the documented unavailable error (not a fake success).

## How to verify
Replay the saved HTTP script; query DB for the order, items snapshot, movements, reservation statuses; try to tamper with a price in the request body.

## Gate to Phase 08
All PASS with evidence; security sign-off; OpenAPI regenerated and committed; PM GO.
