---
name: ecommerce-business
description: GroceryHub business rules for a Vietnamese online grocery store - catalog and pricing, stock reservation, order state machine, coupons, shipping fees, COD vs QR payment, Vietnamese 2-level addresses, purchasing and cost, reviews, revenue/profit definitions, roles. Use whenever implementing or reviewing any feature that encodes a business decision, or when requirements are unclear.
---

# ecommerce-business

## Goal
Keep every module consistent with the agreed business rules (`docs/spec/02-PROJECT-REQUIREMENTS.md` section "Business rules" BR-*). If a rule is missing or ambiguous, do not invent it: add a question to `docs/progress/QUESTIONS.md`.

## Process
1. Find the BR-* IDs relevant to the task. Quote them in the PR description.
2. Encode invariants in the domain model, not the UI. Add a unit test per rule.
3. When two rules conflict, stop and ask the user.

## Core rules (summary - spec 02 is authoritative)
- Money is integer VND. Order totals are computed only on the server: `total = subtotal - couponDiscount + shippingFee`. Prices are snapshotted on order lines.
- Stock: `available = onHand - reserved`. Reserve on order placement; release on cancel/expiry; deduct (movement `Sale`, with unit-cost snapshot) when the order moves to Shipping.
- Order status: `PendingPayment` (QR only) -> `Placed` (COD) / `Confirmed` -> `Preparing` -> `Packed` -> `Shipping` -> `Delivered`; terminal alternatives `Cancelled`, `DeliveryFailed`. Payment status is separate: `Unpaid`, `Pending`, `Paid`, `Failed`, `Expired`, `PartiallyRefunded`, `Refunded`.
- QR payment expires after a configurable window (default 30 min): order auto-cancels and stock is released. Late payments are never auto-accepted (review queue).
- COD: allowed up to a configurable order value; payment becomes `Paid` only when staff records cash collected at delivery.
- One coupon per order; types Percent (with cap), FixedAmount, FreeShipping; min subtotal, validity window, total/per-user limits; redemption recorded atomically with order placement.
- Shipping fee = zone rate by province + free-shipping threshold; only configured provinces are serviceable.
- Address: Vietnam 2-level administrative model since 01/07/2025 (34 provinces/cities, wards/communes; no district level) + street detail + recipient + VN phone; legacy 3-level data is not used for new addresses.
- Reviews: only customers with a Delivered order containing the product; one review per order line; moderated before publishing.
- Cost: moving-average cost updated at goods receipt. Profit = net product revenue of delivered orders - COGS snapshot; shipping and refunds reported separately.
- Roles: Guest browses and keeps a cart; checkout requires login. Staff executes operations by permission; Manager supervises and approves; Admin manages staff/roles/settings.

## Best practices
- Express each rule as a named domain method (`Order.Cancel(reason, actor)`), covered by tests.
- Prefer explicit state machines (allowed transition table) over ad-hoc status updates.
- All business-significant changes are audited (who, when, before/after).

## Common mistakes
- Computing totals in the frontend and trusting them.
- Deducting stock at checkout and again at shipping (double count).
- Treating payment status as order status.
- Floating-point money; time zone mistakes in daily reports (use Asia/Ho_Chi_Minh).
- Letting Staff see cost/profit data without permission.

## Checklist
- [ ] BR-* IDs cited
- [ ] Invariant implemented in domain + tested
- [ ] Audit entry created for sensitive changes
- [ ] Edge cases: out-of-stock mid-checkout, price change in cart, coupon exhausted, expired QR, partial delivery failure
