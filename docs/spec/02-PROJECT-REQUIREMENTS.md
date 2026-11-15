# 02 — Project Requirements (GroceryHub)

Authoritative list of functional (FR), non-functional (NFR) requirements and business rules (BR). Every phase prompt cites these IDs. Traceability (`docs/progress/TRACEABILITY.md`) maps each ID to code, test and evidence. Priority: **M** = must for v1, **S** = should, **C** = could (only after all M done).

## 1. Actors and roles
| Actor | Description | Notes |
|---|---|---|
| Guest | Unauthenticated visitor | Browse, search, cart (server-side guest cart), register/login. Cannot checkout. |
| Customer | Registered buyer | Profile, addresses, checkout, orders, reviews. |
| Staff | Warehouse/order operator | Process orders, prepare, update delivery, check stock; purchasing only if granted `purchasing.*` permissions. |
| Manager | Store supervisor | Staff capabilities + approvals (refunds, price/cost, promotions, reports, manual payment review). |
| Admin | System owner | Everything incl. staff accounts, roles/permissions, settings. |

## 2. Functional requirements — Customer
| ID | Requirement | Pri |
|---|---|---|
| FR-CUS-001 | Home page: banners, category (aisle) navigation, featured/new/best-selling/promo sections. | M |
| FR-CUS-002 | Category tree browsing with product counts; breadcrumbs. | M |
| FR-CUS-003 | Search (Vietnamese accent-insensitive), filter (category, brand, price range, in-stock, promo), sort (relevance, price, newest, best-selling), pagination; filters reflected in URL. | M |
| FR-CUS-004 | Product detail: gallery, price (list/selling, discount), unit & pack size, stock status, description, specs, brand, reviews summary, related products. | M |
| FR-CUS-005 | Cart for Guest (cookie token) and Customer; add/update/remove/clear; stock and price revalidation; merge guest cart on login. | M |
| FR-CUS-006 | Register (email + password, optional phone), email verification, login, logout, forgot/reset password. | M |
| FR-CUS-007 | Profile: name, phone, email (read-only after verify), change password. | M |
| FR-CUS-008 | Multiple delivery addresses (max 10): recipient, phone, province/city, ward/commune (2-level VN model), street/house details, note, default flag. | M |
| FR-CUS-009 | Checkout: select address, shipping fee quote, coupon, payment method (COD / SePay QR), order review, place order idempotently. | M |
| FR-CUS-010 | Shipping fee calculation by province zone and subtotal (free-shipping threshold). | M |
| FR-CUS-011 | Coupon apply/remove with clear validation messages. | M |
| FR-CUS-012 | COD payment. | M |
| FR-CUS-013 | SePay VietQR payment page: QR, amount, content (payment code), bank info, countdown, live status from server, retry after expiry. | M |
| FR-CUS-014 | Order tracking: status timeline, items, totals, address, payment status; cancel while allowed. | M |
| FR-CUS-015 | Order history list with filters and reorder ("mua lại"). | M |
| FR-CUS-016 | Product reviews (rating 1–5 + text) for verified purchases; edit own within 7 days; display with moderation. | M |
| FR-CUS-017 | Responsive UI (360–1920 px), accessible (WCAG 2.2 AA target). | M |
| FR-CUS-018 | Wishlist / favorites. | C |

## 3. Functional requirements — Admin / Manager
| ID | Requirement | Pri |
|---|---|---|
| FR-ADM-001 | Dashboard: today/7d/30d revenue, orders by status, low stock, pending reviews, payment anomalies, top products. | M |
| FR-ADM-002 | Product management: CRUD, images (upload, order, primary), SKU, barcode, unit, brand, category, selling price, list price, cost price (permission), publish/hide, search text. | M |
| FR-ADM-003 | Category (tree) and brand management. | M |
| FR-ADM-004 | Price management with history and audit; scheduled promo price (S). | M |
| FR-ADM-005 | Inventory: stock list, movements ledger, manual adjustment with reason, low-stock threshold, near-expiry list, stocktake (S). | M |
| FR-ADM-006 | Purchasing: suppliers CRUD, purchase orders, goods receipt (qty, unit cost, invoice no., expiry date), cost update (moving average). | M |
| FR-ADM-007 | Order management: list/filter/search, detail, status transitions per permission, internal notes, cancel with reason, print pick list/delivery note. | M |
| FR-ADM-008 | Customer management: list/detail/orders, lock/unlock. | M |
| FR-ADM-009 | Staff management: create/disable staff and managers, assign role and per-user extra permissions. | M |
| FR-ADM-010 | Role and permission management (matrix editor) with audit. | M |
| FR-ADM-011 | Payment management: transactions list, match status, manual review queue, COD collection records, refund workflow (request → approve → execute → complete). | M |
| FR-ADM-012 | Promotions: coupons CRUD + usage report; banners/home sections (S). | M |
| FR-ADM-013 | Review moderation (approve/reject/reply). | M |
| FR-ADM-014 | Revenue report (day/week/month, by category/product/payment method). | M |
| FR-ADM-015 | Profit report (net revenue − COGS), stock valuation, export CSV. | M |
| FR-ADM-016 | Audit log viewer with filters (actor, entity, action, date). | M |
| FR-ADM-017 | Settings: store info, bank account shown on QR page, shipping zones/rates, free-ship threshold, COD limit, QR expiry minutes, low-stock default. | M |

## 4. Functional requirements — Staff
| ID | Requirement | Pri |
|---|---|---|
| FR-STF-001 | Order queue with filters (status, payment, date), assigned-to-me view. | M |
| FR-STF-002 | Confirm order, mark Preparing, generate pick list (grouped by aisle/category), mark Packed. | M |
| FR-STF-003 | Update delivery: Shipping, Delivered (record COD cash collected), DeliveryFailed with reason. | M |
| FR-STF-004 | Check stock for a product (search/barcode); see reserved vs on-hand. | M |
| FR-STF-005 | Goods receipt entry when granted `purchasing.receive`. | M |
| FR-STF-006 | Mobile/tablet-friendly "picking mode". | S |

## 5. Business rules
| ID | Rule |
|---|---|
| BR-01 | Money is integer VND (`long`, `bigint`). `SellingPrice ≤ ListPrice`. Cost is moving average, visible only with `catalog.cost.read`. All price/cost changes are audited and stored in history. |
| BR-02 | `available = onHand − reserved`. Reserve at order placement; release on cancel/expiry; deduct at transition to Shipping (movement `Sale` with unit-cost snapshot). Reservation and deduction are atomic and race-safe. |
| BR-03 | Totals computed server-side only: `total = subtotal − couponDiscount + shippingFee`. Line prices snapshotted at placement. |
| BR-04 | One coupon per order. Types: Percent (with max discount), FixedAmount, FreeShipping. Constraints: min subtotal, start/end, total usage limit, per-user limit, optional category/product scope. Redemption recorded in the same transaction as the order. |
| BR-05 | Shipping fee = zone rate by province + free-shipping threshold; only configured provinces are serviceable; rates editable by Manager/Admin. |
| BR-06 | Payment methods: `COD` (allowed up to configurable max order value) and `SEPAY_QR`. QR payment expires after a configurable window (default 30 min): order auto-cancels, stock released. |
| BR-07 | Customer may cancel in `PendingPayment`, `Placed`, `Confirmed`. From `Preparing` onward only Staff/Manager (with reason). Cancelling a paid order creates a refund request. |
| BR-08 | Order statuses: `PendingPayment`, `Placed`, `Confirmed`, `Preparing`, `Packed`, `Shipping`, `Delivered`, `Cancelled`, `DeliveryFailed`. Allowed transitions are a table in spec 05; invalid transitions are rejected. Payment status is independent: `Unpaid`, `Pending`, `Paid`, `Failed`, `Expired`, `PartiallyRefunded`, `Refunded`. |
| BR-09 | Reviews only by customers with a `Delivered` order containing the product; one per order line; moderated (`Pending` → `Approved`/`Rejected`); aggregates recomputed. |
| BR-10 | Address uses Vietnam's 2-level administrative model effective 01/07/2025 (34 provinces/cities; wards/communes/special zones; no district level). Dataset is vendored with a version stamp; license verified. Address snapshot is copied to the order. |
| BR-11 | Purchase order states: `Draft → Ordered → PartiallyReceived → Received → Closed`, or `Cancelled`. Goods receipts are immutable; corrections via adjustment with reason. |
| BR-12 | Manual stock adjustments require reason and are audited. |
| BR-13 | Revenue = Σ net line revenue (after coupon allocation) of orders that reached `Delivered`, by delivery date, timezone `Asia/Ho_Chi_Minh`. Gross profit = revenue − Σ COGS snapshot. Shipping fees and refunds are reported on separate lines. |
| BR-14 | Guest cannot checkout. Staff accounts are created by Admin (Manager may create Staff if granted `staff.create`). |
| BR-15 | A refund requires: request (reason, amount ≤ paid) → approval by a different user with `refunds.approve` → manual bank transfer → completion with bank reference. |
| BR-16 | Max quantity per cart line and per order line is configurable (default 20). Out-of-stock items cannot be added; stock is re-validated at checkout. |
| BR-17 | Vietnamese phone validation (10 digits starting 0, or +84 format); normalized storage. |

## 6. Non-functional requirements
| ID | Requirement | Target |
|---|---|---|
| NFR-001 | Performance (API) | p95 < 300 ms for catalog reads, < 600 ms for checkout at 50 concurrent users on dev hardware |
| NFR-002 | Performance (web) | Mobile Lighthouse: Performance ≥ 85, LCP < 2.5 s, CLS < 0.1 on home and product pages |
| NFR-003 | Security | OWASP ASVS L2 aligned; spec in `10-SKILLS-AND-RULES.md` / security rules |
| NFR-004 | Availability | Health checks, graceful shutdown, restart policy; restore drill documented |
| NFR-005 | Observability | Structured logs with correlation id; payment/webhook events traceable end-to-end |
| NFR-006 | Accessibility | WCAG 2.2 AA on key flows; axe has no serious/critical violations |
| NFR-007 | i18n | Vietnamese UI; all strings externalized; VND/`vi-VN` formats; dates `Asia/Ho_Chi_Minh` |
| NFR-008 | Data integrity | Unique constraints, transactions and concurrency tests for stock/payments/coupons |
| NFR-009 | Maintainability | Module boundaries enforced by architecture tests; CI green; no commercial-license dependencies |
| NFR-010 | Privacy | Collect minimum PII; password hashing; ability to delete/anonymize a customer on request (Admin tool) |

## 7. Out of scope for v1 (do not build unless the user approves a Change Request)
Marketplace/multi-vendor, multi-warehouse, variable-weight pricing, native mobile apps, third-party courier API integration (manual delivery by store staff), loyalty points, multi-language UI, card payments, invoicing (e-invoice).

## 8. Open questions the PM must put to the user in Phase 1 (record answers in `docs/progress/DECISIONS.md`)
1. Receiving bank account for SePay (bank, account) and whether the account supports SePay webhooks with payment-code detection.
2. Delivery area (provinces served) and initial shipping rates; free-shipping threshold; COD maximum.
3. Product catalog source (manual entry vs CSV import) and initial categories.
4. Brand assets (logo, name usage, colors) if any; otherwise the proposed design system applies.
5. Production hosting target (VPS/cloud) and SQL Server edition.
