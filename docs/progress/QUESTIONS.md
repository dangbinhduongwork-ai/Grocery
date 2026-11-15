# GroceryHub — Open Questions, Spec Gaps & Requirement Coverage Audit

All unresolved requirement questions, external dependency questions, and cross-spec ambiguities are tracked here. Every `TODO(Q-xxx)` in source code must reference an active ID in this file.

---

## 1. User Open Questions (`docs/spec/02-PROJECT-REQUIREMENTS.md` §8)

| ID | Phase | Raised By | Question | Suggested Default | Status |
|---|---|---|---|---|---|
| **Q-001** | 01 | `project-manager` | **Receiving bank account for SePay** (bank short name, account number, account holder name) and whether the account supports SePay webhooks with payment-code (`GH` + 10 digits) detection? | In dev/test, use placeholder `MBBank` / `0000000000` / `CONG TY TNHH GROCERYHUB` with the dev simulator (`POST /dev/sepay/simulate`); in production, configure real account via `.env` (`SePay__BankShortName`, `SePay__AccountNumber`, `SePay__AccountName`) and Admin Settings (`FR-ADM-017`). | Closed (`DEC-006`) |
| **Q-002** | 01 | `project-manager` | **Delivery area (provinces served), initial shipping rates, free-shipping threshold, and COD maximum?** | Serve **TP. Hồ Chí Minh** (`25.000 ₫`, Zone 1) and **Bình Dương / Đồng Nai** (`35.000 ₫`, Zone 2) initially; free-shipping threshold `300.000 ₫`; COD max `2.000.000 ₫` (all editable in Admin Settings `FR-ADM-017`). | Closed (`DEC-007`) |
| **Q-003** | 01 | `project-manager` | **Product catalog source** (manual entry vs CSV import) and initial categories? | Manual CRUD in Admin (`FR-ADM-002`) + Development seeder (~200 sample products across 8 Vietnamese grocery aisles: *Rau củ quả*, *Thịt cá trứng*, *Sữa & chế phẩm*, *Đồ khô & gia vị*, *Đồ uống*, *Bánh kẹo & ăn vặt*, *Chăm sóc cá nhân*, *Vệ sinh nhà cửa*). | Closed (`DEC-008`) |
| **Q-004** | 01 | `project-manager` | **Brand assets** (logo, brand name usage, custom colors) if any? | Use the **"Sổ chợ"** design system in `docs/spec/08-UI-DESIGN-SYSTEM.md` (`#FAF7F0` paper, `#1F3A2D` leaf green, `#D9432F`/`#B8321F` tomato price tag, `#E3A21A` turmeric badge, fonts `Be Vietnam Pro` + `Fraunces`). | Closed (`DEC-009`) |
| **Q-005** | 01 | `project-manager` | **Production hosting target** (VPS vs cloud) and **SQL Server edition**? | **Option A**: Single Linux VPS with Docker Compose + reverse proxy TLS (`docs/spec/13-DEPLOYMENT-STRATEGY.md`); **SQL Server Express** (free, 10 GB limit) for initial production launch (`Developer` edition strictly limited to local dev/test). | Closed (`DEC-010`) |

---

## 2. Spec Line-by-Line Review — Gaps, Contradictions & Ambiguities Found (Phase 01 Step B)

| ID | Specs Involved | Finding (Gap / Contradiction / Ambiguity) | Proposed Resolution (Default) | Status |
|---|---|---|---|---|
| **Q-006** | `02` (`BR-07`), `05` §10, `07` §2 | **Staff Order Cancellation Permission:** `BR-07` states *"From `Preparing` onward only Staff/Manager (with reason)"* and `05` §10 lists `staff / manager` for `PendingPayment/Placed/Confirmed/Preparing -> Cancelled`, but `07` §2 permission matrix sets `orders.cancel` to `–` for `Staff` (only `Manager` and `Admin`). | Grant `Staff` (via `orders.process`) the right to cancel unpaid orders in `PendingPayment`, `Placed`, `Confirmed` with a required reason; cancelling from `Preparing`, `Packed`, or `DeliveryFailed` (or cancelling any `Paid` order) requires `orders.cancel` (`Manager`/`Admin`, or a Staff user explicitly granted `orders.cancel`). | Closed (`DEC-011`) |
| **Q-007** | `02` (`BR-16`), `06` §cart | **Max Cart Line Quantity (`20` vs `99`):** `BR-16` specifies configurable max quantity per cart/order line (default `20`), whereas `06-DATABASE-ERD.md` §cart specifies `CHECK Quantity BETWEEN 1 AND 99`. | Keep DB constraint `CHECK (Quantity BETWEEN 1 AND 99)` as the hard database ceiling, and enforce the configurable `MaxQuantityPerLine` (default `20` in `shared.Settings`) in application domain validation (`QUANTITY_LIMIT_EXCEEDED`). | Closed (`DEC-011`) |
| **Q-008** | `02` (`FR-CUS-005`, `BR-04`, `BR-14`), `05` §4, `07` §4 | **Guest Cart Coupon Application:** `05` §4 says `Cart` calls `Promotions (preview)`, whereas `07` §4 marks `POST /cart/coupon` and `DELETE /cart/coupon` as `auth` (authenticated only). | Require `auth` (logged-in Customer) for `POST/DELETE /cart/coupon` as specified in `07` §4, because `BR-04` enforces `PerUserLimit` (which requires `UserId`) and `BR-14` prohibits Guest checkout. | Closed (`DEC-011`) |
| **Q-009** | `02` (`BR-03`, `BR-04`, `BR-13`), `06` §ordering | **`FreeShipping` Coupon vs Net Line Revenue (`BR-13`):** `BR-03` defines `total = subtotal - couponDiscount + shippingFee`, and `BR-13` defines `Revenue = Σ net line revenue (after coupon allocation)`. If a `FreeShipping` coupon is allocated across product lines, product revenue would be artificially reduced by the shipping fee. | For `FreeShipping` coupons, set order-level `CouponDiscount = ShippingFee` (so `Total = Subtotal - CouponDiscount + ShippingFee` holds), and set `OrderItems.DiscountAllocated = 0` on product lines so `BR-13` net product revenue is unaffected by shipping subsidies. | Closed (`DEC-011`) |
| **Q-010** | `02` (`FR-CUS-013`, `BR-06`), `07` §4, `11` §3/§5 | **QR Payment Retry (`POST /me/orders/{id}/payment-attempts`):** `FR-CUS-013` says *"retry after expiry"* while `07` §4 says *"new QR for an expired/unpaid QR order still `PendingPayment`"*. Once `PaymentExpiryService` cancels the order (`ExpiresAt + ExpiryGraceMinutes`), the order is `Cancelled`. | While the order is still in `PendingPayment` (including during the grace window or when the user requests a fresh QR timer, capped at 5 attempts per order), `POST /me/orders/{id}/payment-attempts` marks the prior attempt `Superseded` and issues a new `GH##########` code + `ExpiresAt`. Once the order is `Cancelled`, the user uses `POST /me/orders/{id}/reorder` ("Mua lại", `FR-CUS-015`). | Closed (`DEC-011`) |

---

## 3. Original Feature Brief → Requirement ID Coverage Matrix (Phase 01 Step B)

Every item from the original GroceryHub product feature brief has been checked against `docs/spec/02-PROJECT-REQUIREMENTS.md` (`68` total requirement IDs: `18 FR-CUS`, `17 FR-ADM`, `6 FR-STF`, `17 BR`, `10 NFR`; verified in `docs/evidence/phase-01/pm-requirements-coverage.txt`).

| Domain / Actor | Original Feature Item | Mapped Requirement ID(s) | Coverage Status |
|---|---|---|---|
| **Roles (5)** | Guest, Customer, Staff, Manager, Admin with permission-based RBAC | `02` §1, `FR-ADM-009`, `FR-ADM-010`, `BR-14`, `NFR-003` | **Covered (100%)** |
| **Customer Storefront** | Home page (banners, aisle nav, featured/new/best-selling/promo) | `FR-CUS-001` | **Covered (100%)** |
| **Customer Storefront** | Category tree browsing with counts & breadcrumbs | `FR-CUS-002` | **Covered (100%)** |
| **Customer Storefront** | Vietnamese accent-insensitive search, filters, sorting, URL-synced pagination | `FR-CUS-003`, `NFR-001`, `NFR-007` | **Covered (100%)** |
| **Customer Storefront** | Product detail (gallery, list/selling price, unit/pack size, stock, specs, reviews, related) | `FR-CUS-004`, `BR-01` | **Covered (100%)** |
| **Customer Cart** | Guest cookie cart & Customer cart, CRUD, price/stock revalidation, merge on login | `FR-CUS-005`, `BR-16` | **Covered (100%)** |
| **Customer Auth & Profile** | Register, email verification, login, logout, forgot/reset/change password, profile | `FR-CUS-006`, `FR-CUS-007`, `BR-17`, `NFR-003`, `NFR-010` | **Covered (100%)** |
| **Customer Addresses** | Up to 10 delivery addresses using Vietnam 2-level model (34 provinces/cities + wards/communes) | `FR-CUS-008`, `BR-10` | **Covered (100%)** |
| **Checkout & Promotions** | Address selection, province zone shipping fee + free-ship threshold, coupon apply/remove, idempotent order placement | `FR-CUS-009`, `FR-CUS-010`, `FR-CUS-011`, `BR-02`, `BR-03`, `BR-04`, `BR-05` | **Covered (100%)** |
| **Customer Payments** | COD payment & SePay VietQR payment page (QR, amount, `GH` code, bank info, countdown, live server status, retry) | `FR-CUS-012`, `FR-CUS-013`, `BR-06` | **Covered (100%)** |
| **Customer Orders & Reviews** | Order tracking timeline, cancel while allowed, order history, reorder ("mua lại"), verified-purchase reviews, wishlist (Could) | `FR-CUS-014`, `FR-CUS-015`, `FR-CUS-016`, `FR-CUS-018`, `BR-07`, `BR-08`, `BR-09` | **Covered (100%)** |
| **Storefront UX & Quality** | Responsive UI (360–1920 px), WCAG 2.2 AA accessibility, mobile Lighthouse performance | `FR-CUS-017`, `NFR-002`, `NFR-006`, `NFR-007` | **Covered (100%)** |
| **Admin / Manager** | Executive dashboard (today/7d/30d revenue, orders by status, low stock, pending reviews, payment anomalies, top products) | `FR-ADM-001` | **Covered (100%)** |
| **Admin / Manager** | Product, category tree, brand, and price management with history & cost permission protection | `FR-ADM-002`, `FR-ADM-003`, `FR-ADM-004`, `BR-01` | **Covered (100%)** |
| **Admin / Manager** | Inventory ledger, stock movements, manual adjustments with reason, low-stock & near-expiry | `FR-ADM-005`, `BR-02`, `BR-12` | **Covered (100%)** |
| **Admin / Manager** | Purchasing (suppliers CRUD, purchase orders, goods receipts, moving-average cost update) | `FR-ADM-006`, `BR-01`, `BR-11` | **Covered (100%)** |
| **Admin / Manager** | Order management, transitions, internal notes, cancellation, pick list & delivery note printing | `FR-ADM-007`, `BR-07`, `BR-08` | **Covered (100%)** |
| **Admin / Manager** | Customer lock/unlock/anonymize, Staff CRUD, Role & Permission matrix editor | `FR-ADM-008`, `FR-ADM-009`, `FR-ADM-010`, `BR-14`, `NFR-010` | **Covered (100%)** |
| **Admin / Manager** | Payment transactions, anomaly review queue, COD cash reconciliation, 4-eyes refund workflow | `FR-ADM-011`, `BR-06`, `BR-15` | **Covered (100%)** |
| **Admin / Manager** | Promotions (coupons CRUD + usage, banners), review moderation, revenue & profit reports + CSV export, audit logs, store settings | `FR-ADM-012`, `FR-ADM-013`, `FR-ADM-014`, `FR-ADM-015`, `FR-ADM-016`, `FR-ADM-017`, `BR-13` | **Covered (100%)** |
| **Staff Operations** | Order queue, confirm/preparing/pick-list/packed, shipping/delivered (COD cash)/failed, stock lookup, goods receipt, mobile picking mode | `FR-STF-001`, `FR-STF-002`, `FR-STF-003`, `FR-STF-004`, `FR-STF-005`, `FR-STF-006` | **Covered (100%)** |

**Uncovered items count:** `0`.
