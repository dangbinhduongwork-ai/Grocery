# GroceryHub — Requirements Traceability Matrix

Maps every Functional Requirement (`FR-*`), Business Rule (`BR-*`), and Non-Functional Requirement (`NFR-*`) from `docs/spec/02-PROJECT-REQUIREMENTS.md` to its implementation code path, automated test(s), and verification evidence.

Status values: `NotStarted` | `InProgress` | `Implemented` | `Verified` (only QA or PM may set `Verified` after a real test run).

## 1. Customer Functional Requirements (`FR-CUS-*`)

| ID | Pri | Requirement Summary | Target Phase | Code Path | Test Name | Evidence File | Status |
|---|---|---|---|---|---|---|---|
| FR-CUS-001 | M | Home page: banners, category (aisle) navigation, featured/new/best-selling/promo sections | 06, 08 | — | — | — | NotStarted |
| FR-CUS-002 | M | Category tree browsing with product counts; breadcrumbs | 06, 08 | — | — | — | NotStarted |
| FR-CUS-003 | M | Search (VN accent-insensitive), filter, sort, pagination in URL | 06, 08 | — | — | — | NotStarted |
| FR-CUS-004 | M | Product detail: gallery, prices, unit/pack size, stock, specs, brand, reviews, related | 06, 08 | — | — | — | NotStarted |
| FR-CUS-005 | M | Cart for Guest (`gh_cart` cookie) & Customer; CRUD; stock/price revalidation; merge on login | 07, 08 | — | — | — | NotStarted |
| FR-CUS-006 | M | Register, email verification, login, logout, forgot/reset password | 05, 08 | — | — | — | NotStarted |
| FR-CUS-007 | M | Profile: name, phone, email (read-only after verify), change password | 05, 08 | — | — | — | NotStarted |
| FR-CUS-008 | M | Multiple delivery addresses (max 10): 2-level VN model (province/city + ward/commune) | 07, 08 | — | — | — | NotStarted |
| FR-CUS-009 | M | Checkout: address, shipping quote, coupon, payment method (COD / SePay QR), idempotent order placement | 07, 08 | — | — | — | NotStarted |
| FR-CUS-010 | M | Shipping fee calculation by province zone and subtotal (free-shipping threshold) | 07 | — | — | — | NotStarted |
| FR-CUS-011 | M | Coupon apply/remove with validation messages | 07, 08 | — | — | — | NotStarted |
| FR-CUS-012 | M | COD payment | 07, 08 | — | — | — | NotStarted |
| FR-CUS-013 | M | SePay VietQR payment page: QR, amount, payment code, bank info, countdown, live status, retry | 10 | — | — | — | NotStarted |
| FR-CUS-014 | M | Order tracking: timeline, items, totals, address, payment status; cancel while allowed | 07, 08 | — | — | — | NotStarted |
| FR-CUS-015 | M | Order history list with filters and reorder ("mua lại") | 07, 08 | — | — | — | NotStarted |
| FR-CUS-016 | M | Product reviews (1–5 + text) for verified purchases; edit within 7d; moderation | 08 | — | — | — | NotStarted |
| FR-CUS-017 | M | Responsive UI (360–1920 px), WCAG 2.2 AA accessible | 08, 11 | — | — | — | NotStarted |
| FR-CUS-018 | C | Wishlist / favorites | Backlog | — | — | — | NotStarted |

## 2. Admin / Manager Functional Requirements (`FR-ADM-*`)

| ID | Pri | Requirement Summary | Target Phase | Code Path | Test Name | Evidence File | Status |
|---|---|---|---|---|---|---|---|
| FR-ADM-001 | M | Dashboard: today/7d/30d revenue, orders by status, low stock, pending reviews, payment anomalies, top products | 09 | — | — | — | NotStarted |
| FR-ADM-002 | M | Product management: CRUD, images, SKU, barcode, unit, brand, category, prices, publish/hide | 06, 09 | — | — | — | NotStarted |
| FR-ADM-003 | M | Category (tree) and brand management | 06, 09 | — | — | — | NotStarted |
| FR-ADM-004 | M | Price management with history and audit; scheduled promo price (S) | 06, 09 | — | — | — | NotStarted |
| FR-ADM-005 | M | Inventory: stock list, movements ledger, manual adjustment with reason, low-stock, near-expiry | 06, 09 | — | — | — | NotStarted |
| FR-ADM-006 | M | Purchasing: suppliers CRUD, POs, goods receipt, moving-average cost update | 06, 09 | — | — | — | NotStarted |
| FR-ADM-007 | M | Order management: list/filter/search, detail, status transitions, internal notes, cancel, print pick/delivery | 07, 09 | — | — | — | NotStarted |
| FR-ADM-008 | M | Customer management: list/detail/orders, lock/unlock | 05, 09 | — | — | — | NotStarted |
| FR-ADM-009 | M | Staff management: create/disable staff & managers, assign role and extra permissions | 05, 09 | — | — | — | NotStarted |
| FR-ADM-010 | M | Role and permission management (matrix editor) with audit | 05, 09 | — | — | — | NotStarted |
| FR-ADM-011 | M | Payment management: transactions list, match status, manual review queue, COD collection, refund workflow | 10 | — | — | — | NotStarted |
| FR-ADM-012 | M | Promotions: coupons CRUD + usage report; banners/home sections (S) | 07, 09 | — | — | — | NotStarted |
| FR-ADM-013 | M | Review moderation (approve/reject/reply) | 08, 09 | — | — | — | NotStarted |
| FR-ADM-014 | M | Revenue report (day/week/month, by category/product/payment method) | 09 | — | — | — | NotStarted |
| FR-ADM-015 | M | Profit report (net revenue − COGS), stock valuation, export CSV | 09 | — | — | — | NotStarted |
| FR-ADM-016 | M | Audit log viewer with filters (actor, entity, action, date) | 09 | — | — | — | NotStarted |
| FR-ADM-017 | M | Settings: store info, bank account, shipping zones/rates, free-ship threshold, COD limit, QR expiry, low-stock | 07, 09, 10 | — | — | — | NotStarted |

## 3. Staff Functional Requirements (`FR-STF-*`)

| ID | Pri | Requirement Summary | Target Phase | Code Path | Test Name | Evidence File | Status |
|---|---|---|---|---|---|---|---|
| FR-STF-001 | M | Order queue with filters (status, payment, date), assigned-to-me view | 07, 09 | — | — | — | NotStarted |
| FR-STF-002 | M | Confirm order, mark Preparing, generate pick list (grouped by aisle/category), mark Packed | 07, 09 | — | — | — | NotStarted |
| FR-STF-003 | M | Update delivery: Shipping, Delivered (record COD cash collected), DeliveryFailed with reason | 07, 09 | — | — | — | NotStarted |
| FR-STF-004 | M | Check stock for a product (search/barcode); see reserved vs on-hand | 06, 09 | — | — | — | NotStarted |
| FR-STF-005 | M | Goods receipt entry when granted `purchasing.receive` | 06, 09 | — | — | — | NotStarted |
| FR-STF-006 | S | Mobile/tablet-friendly "picking mode" | 09 | — | — | — | NotStarted |

## 4. Business Rules (`BR-*`)

| ID | Rule Summary | Target Phase | Code Path | Test Name | Evidence File | Status |
|---|---|---|---|---|---|---|
| BR-01 | Money is integer VND (`long`/`bigint`); `SellingPrice <= ListPrice`; moving-average cost (`catalog.cost.read`); audited price/cost history | 06 | — | — | — | NotStarted |
| BR-02 | `available = onHand - reserved`; atomic reserve at placement, release on cancel/expiry, deduct on Shipping with unit-cost snapshot | 06, 07 | — | — | — | NotStarted |
| BR-03 | Server-side totals only (`subtotal - couponDiscount + shippingFee`); line prices snapshotted at placement | 07 | — | — | — | NotStarted |
| BR-04 | One coupon per order (Percent/FixedAmount/FreeShipping, min subtotal, dates, total/per-user limits, scope) in same transaction | 07 | — | — | — | NotStarted |
| BR-05 | Shipping fee by configured province zone + free-shipping threshold | 07 | — | — | — | NotStarted |
| BR-06 | Payment methods `COD` (up to max order value) and `SEPAY_QR` (auto-expires after window + grace, releases stock) | 07, 10 | — | — | — | NotStarted |
| BR-07 | Cancellation rules by status/role; cancelling paid order creates refund request | 07, 10 | — | — | — | NotStarted |
| BR-08 | Order state machine & independent Payment status state machine | 07, 10 | — | — | — | NotStarted |
| BR-09 | Verified-purchase reviews (`Delivered` order, 1 per order line, moderated, aggregates recomputed) | 08 | — | — | — | NotStarted |
| BR-10 | 2-level VN address model (34 provinces/cities + wards/communes effective 01/07/2025); snapshotted on order | 07 | — | — | — | NotStarted |
| BR-11 | Purchase order states (`Draft -> Ordered -> PartiallyReceived -> Received -> Closed` / `Cancelled`); immutable goods receipts | 06 | — | — | — | NotStarted |
| BR-12 | Manual stock adjustments require reason and audit log | 06 | — | — | — | NotStarted |
| BR-13 | Revenue & gross profit (`revenue - COGS`) definitions on `Delivered` orders in `Asia/Ho_Chi_Minh` | 09 | — | — | — | NotStarted |
| BR-14 | Guest cannot checkout; Staff created by Admin (or Manager with `staff.create`) | 05, 07 | — | — | — | NotStarted |
| BR-15 | 4-step refund workflow with four-eyes approval (`refunds.approve` != requester) | 10 | — | — | — | NotStarted |
| BR-16 | Max qty per cart/order line configurable (default 20); stock re-validated at checkout | 07 | — | — | — | NotStarted |
| BR-17 | Vietnamese phone validation (`0xxxxxxxxx` or `+84...`) and normalized storage | 05, 07 | — | — | — | NotStarted |

## 5. Non-Functional Requirements (`NFR-*`)

| ID | Requirement Summary | Target | Target Phase | Evidence File | Status |
|---|---|---|---|---|---|
| NFR-001 | API Performance | p95 < 300 ms catalog reads, < 600 ms checkout @ 50 VUs | 06, 11 | — | NotStarted |
| NFR-002 | Web Performance | Mobile Lighthouse Perf >= 85, LCP < 2.5 s, CLS < 0.1 | 08, 11 | — | NotStarted |
| NFR-003 | Security | OWASP ASVS L2 aligned; security rules & gates | 05, 07, 10, 11, 12 | — | NotStarted |
| NFR-004 | Availability | Health checks, graceful shutdown, restart policy, restore drill | 04, 12 | — | NotStarted |
| NFR-005 | Observability | Structured JSON logs with correlation id; traceable webhooks | 04, 10 | — | NotStarted |
| NFR-006 | Accessibility | WCAG 2.2 AA on key flows; axe 0 serious/critical | 08, 09, 11 | — | NotStarted |
| NFR-007 | i18n | Vietnamese UI (`vi.ts`), VND integer formatting, `Asia/Ho_Chi_Minh` | 08, 09 | — | NotStarted |
| NFR-008 | Data Integrity | Unique constraints, ACID transactions, concurrency tests | 06, 07, 10, 11 | — | NotStarted |
| NFR-009 | Maintainability | Architecture tests, green CI, zero commercial-license packages | 04, 11, 12 | — | NotStarted |
| NFR-010 | Privacy | Minimum PII, Identity password hashing, customer anonymization tool | 05, 09 | — | NotStarted |
