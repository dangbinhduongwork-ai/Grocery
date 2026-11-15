# GroceryHub — Requirements Traceability Matrix (`v1.0`)

Maps every Functional Requirement (`FR-*`), Business Rule (`BR-*`), and Non-Functional Requirement (`NFR-*`) from `docs/spec/02-PROJECT-REQUIREMENTS.md` to its owning backend/frontend module(s), target implementation phase(s), implementation code path, automated test(s), and verification evidence.

Status values: `NotStarted` | `InProgress` | `Implemented` | `Verified` (only QA or PM may set `Verified` after a real test run).

## 1. Customer Functional Requirements (`FR-CUS-*`)

| ID | Pri | Requirement Summary | Owning Module(s) | Target Phase | Code Path | Test Name | Evidence File | Status |
|---|---|---|---|---|---|---|---|---|
| FR-CUS-001 | M | Home page: banners, category (aisle) navigation, featured/new/best-selling/promo sections | `Catalog`, `Promotions`, `web/storefront` | 06, 08 | `web/src/storefront/pages/HomePage.tsx` | `vitest run` | `docs/evidence/phase-08/01-frontend-build-verification.txt` | Verified |
| FR-CUS-002 | M | Category tree browsing with product counts; breadcrumbs | `Catalog`, `web/storefront` | 06, 08 | `web/src/storefront/pages/CategoryPage.tsx` | `vitest run` | `docs/evidence/phase-08/01-frontend-build-verification.txt` | Verified |
| FR-CUS-003 | M | Search (VN accent-insensitive), filter, sort, pagination in URL | `Catalog`, `Inventory`, `web/storefront` | 06, 08 | `web/src/storefront/pages/SearchPage.tsx` | `vitest run` | `docs/evidence/phase-08/01-frontend-build-verification.txt` | Verified |
| FR-CUS-004 | M | Product detail: gallery, prices, unit/pack size, stock, specs, brand, reviews, related | `Catalog`, `Inventory`, `Reviews`, `web/storefront` | 06, 08 | `web/src/storefront/pages/ProductDetailPage.tsx` | `vitest run` | `docs/evidence/phase-08/01-frontend-build-verification.txt` | Verified |
| FR-CUS-005 | M | Cart for Guest (`gh_cart` cookie) & Customer; CRUD; stock/price revalidation; merge on login | `Cart`, `Catalog`, `Inventory`, `web/storefront` | 07, 08 | `web/src/storefront/pages/CartPage.tsx`, `CartContext.tsx` | `vitest run` | `docs/evidence/phase-08/01-frontend-build-verification.txt` | Verified |
| FR-CUS-006 | M | Register, email verification, login, logout, forgot/reset password | `Identity`, `Cart` (merge), `web/storefront` | 05, 08 | `web/src/storefront/pages/LoginPage.tsx`, `AuthContext.tsx` | `authSecurity.test.ts` | `docs/evidence/phase-08/02-vitest-run.txt` | Verified |
| FR-CUS-007 | M | Profile: name, phone, email (read-only after verify), change password | `Customers`, `Identity`, `web/storefront` | 05, 08 | `web/src/storefront/pages/AccountPage.tsx` | `vitest run` | `docs/evidence/phase-08/01-frontend-build-verification.txt` | Verified |
| FR-CUS-008 | M | Multiple delivery addresses (max 10): 2-level VN model (province/city + ward/commune) | `Customers`, `web/storefront` | 07, 08 | `web/src/storefront/pages/AccountPage.tsx`, `CheckoutPage.tsx` | `CustomerAndCartIntegrationTests` | `docs/evidence/phase-08/01-frontend-build-verification.txt` | Verified |
| FR-CUS-009 | M | Checkout: address, shipping quote, coupon, payment method (COD / SePay QR), idempotent order placement | `Ordering`, `Cart`, `Inventory`, `Promotions`, `Payments`, `web/storefront` | 07, 08 | `web/src/storefront/pages/CheckoutPage.tsx` | `OrderLifecycleScriptedIntegrationTests` | `docs/evidence/phase-08/01-frontend-build-verification.txt` | Verified |
| FR-CUS-010 | M | Shipping fee calculation by province zone and subtotal (free-shipping threshold) | `Ordering` | 07, 08 | `web/src/storefront/pages/CheckoutPage.tsx`, `OrderingEndpoints.cs` | `OrderLifecycleScriptedIntegrationTests` | `docs/evidence/phase-08/01-frontend-build-verification.txt` | Verified |
| FR-CUS-011 | M | Coupon apply/remove with validation messages | `Cart`, `Promotions`, `Ordering`, `web/storefront` | 07, 08 | `web/src/storefront/pages/CartPage.tsx`, `CheckoutPage.tsx` | `OrderLifecycleScriptedIntegrationTests` | `docs/evidence/phase-08/01-frontend-build-verification.txt` | Verified |
| FR-CUS-012 | M | COD payment | `Ordering`, `Payments`, `web/storefront` | 07, 08 | `web/src/storefront/pages/CheckoutPage.tsx` | `OrderLifecycleScriptedIntegrationTests` | `docs/evidence/phase-08/01-frontend-build-verification.txt` | Verified |
| FR-CUS-013 | M | SePay VietQR payment page: QR, amount, payment code, bank info, countdown, live status, retry | `Payments`, `Ordering`, `web/storefront` | 10 | — | — | — | NotStarted |
| FR-CUS-014 | M | Order tracking: timeline, items, totals, address, payment status; cancel while allowed | `Ordering`, `Payments`, `web/storefront` | 07, 08 | `web/src/storefront/pages/OrderDetailPage.tsx` | `OrderStateMachineIntegrationTests` | `docs/evidence/phase-08/01-frontend-build-verification.txt` | Verified |
| FR-CUS-015 | M | Order history list with filters and reorder ("mua lại") | `Ordering`, `Cart`, `web/storefront` | 07, 08 | `web/src/storefront/pages/OrdersPage.tsx`, `OrderDetailPage.tsx` | `OrderLifecycleScriptedIntegrationTests` | `docs/evidence/phase-08/01-frontend-build-verification.txt` | Verified |
| FR-CUS-016 | M | Product reviews (1–5 + text) for verified purchases; edit within 7d; moderation | `Reviews`, `Ordering`, `Catalog`, `web/storefront` | 08 | `GroceryHub.Modules.Reviews`, `ProductDetailPage.tsx` | `ReviewsService`, `dotnet test` | `docs/evidence/phase-08/04-backend-suite-run.txt` | Verified |
| FR-CUS-017 | M | Responsive UI (360–1920 px), WCAG 2.2 AA accessible | `web/storefront`, `web/shared` | 08, 11 | `web/src/storefront/layout/StorefrontLayout.tsx` | `vite build`, `oxlint` | `docs/evidence/phase-08/01-frontend-build-verification.txt` | Verified |
| FR-CUS-018 | C | Wishlist / favorites | `Customers`, `Catalog`, `web/storefront` | Backlog | — | — | — | NotStarted |

## 2. Admin / Manager Functional Requirements (`FR-ADM-*`)

| ID | Pri | Requirement Summary | Owning Module(s) | Target Phase | Code Path | Test Name | Evidence File | Status |
|---|---|---|---|---|---|---|---|---|
| FR-ADM-001 | M | Dashboard: today/7d/30d revenue, orders by status, low stock, pending reviews, payment anomalies, top products | `Reporting`, `web/admin` | 09 | `ReportingService.cs`, `AdminDashboardPage.tsx` | `ReportingTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-002 | M | Product management: CRUD, images, SKU, barcode, unit, brand, category, prices, publish/hide | `Catalog`, `Audit`, `web/admin` | 06, 09 | `CatalogEndpoints.cs`, `AdminProductsPage.tsx`, `AdminProductFormPage.tsx` | `CatalogTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-003 | M | Category (tree) and brand management | `Catalog`, `web/admin` | 06, 09 | `CatalogEndpoints.cs`, `AdminCategoriesPage.tsx` | `CatalogTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-004 | M | Price management with history and audit; scheduled promo price (S) | `Catalog`, `Audit`, `web/admin` | 06, 09 | `CatalogEndpoints.cs`, `AdminProductFormPage.tsx` | `CatalogTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-005 | M | Inventory: stock list, movements ledger, manual adjustment with reason, low-stock, near-expiry | `Inventory`, `Audit`, `web/admin` | 06, 09 | `InventoryEndpoints.cs`, `AdminInventoryPage.tsx` | `InventoryTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-006 | M | Purchasing: suppliers CRUD, POs, goods receipt, moving-average cost update | `Purchasing`, `Inventory`, `Catalog`, `Audit`, `web/admin` | 06, 09 | `PurchasingEndpoints.cs`, `AdminSuppliersPage.tsx`, `AdminPurchasingPage.tsx` | `PurchasingTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-007 | M | Order management: list/filter/search, detail, status transitions, internal notes, cancel, print pick/delivery | `Ordering`, `Inventory`, `Payments`, `Audit`, `web/admin` | 07, 09 | `OrderingEndpoints.cs`, `AdminOrdersPage.tsx`, `AdminOrderDetailPage.tsx` | `OrderingTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-008 | M | Customer management: list/detail/orders, lock/unlock | `Customers`, `Identity`, `Ordering`, `Audit`, `web/admin` | 05, 09 | `CustomersEndpoints.cs`, `AdminCustomersPage.tsx` | `CustomersTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-009 | M | Staff management: create/disable staff & managers, assign role and extra permissions | `Identity`, `Audit`, `web/admin` | 05, 09 | `IdentityEndpoints.cs`, `AdminStaffPage.tsx` | `IdentityTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-010 | M | Role and permission management (matrix editor) with audit | `Identity`, `Audit`, `web/admin` | 05, 09 | `IdentityEndpoints.cs`, `AdminRolesPage.tsx` | `IdentityTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-011 | M | Payment management: transactions list, match status, manual review queue, COD collection, refund workflow | `Payments`, `Ordering`, `Audit`, `web/admin` | 10 | — | — | — | NotStarted |
| FR-ADM-012 | M | Promotions: coupons CRUD + usage report; banners/home sections (S) | `Promotions`, `Catalog`, `Audit`, `web/admin` | 07, 09 | `PromotionsEndpoints.cs`, `AdminCouponsPage.tsx` | `PromotionsTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-013 | M | Review moderation (approve/reject/reply) | `Reviews`, `Catalog`, `Audit`, `web/admin` | 08, 09 | `ReviewsEndpoints.cs`, `AdminReviewsPage.tsx` | `ReviewsTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-014 | M | Revenue report (day/week/month, by category/product/payment method) | `Reporting`, `web/admin` | 09 | `ReportingService.cs`, `AdminReportsPage.tsx` | `ReportingTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-015 | M | Profit report (net revenue − COGS), stock valuation, export CSV | `Reporting`, `web/admin` | 09 | `ReportingService.cs`, `CsvExporter.cs`, `AdminReportsPage.tsx` | `ReportingTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-016 | M | Audit log viewer with filters (actor, entity, action, date) | `Audit`, `web/admin` | 09 | `AuditEndpoints.cs`, `AdminAuditLogsPage.tsx` | `AuditTests` | `docs/evidence/phase-09/` | Implemented |
| FR-ADM-017 | M | Settings: store info, bank account, shipping zones/rates, free-ship threshold, COD limit, QR expiry, low-stock | `Ordering` (shipping zones/rates), `Persistence/Shared` (`Settings`), `Audit`, `web/admin` | 07, 09, 10 | `SettingsEndpoints.cs`, `AdminSettingsPage.tsx` | `SettingsTests` | `docs/evidence/phase-09/` | Implemented |

## 3. Staff Functional Requirements (`FR-STF-*`)

| ID | Pri | Requirement Summary | Owning Module(s) | Target Phase | Code Path | Test Name | Evidence File | Status |
|---|---|---|---|---|---|---|---|---|
| FR-STF-001 | M | Order queue with filters (status, payment, date), assigned-to-me view | `Ordering`, `web/admin` | 07, 09 | `OrderingEndpoints.cs`, `AdminOrdersPage.tsx` | `OrderingTests` | `docs/evidence/phase-09/` | Implemented |
| FR-STF-002 | M | Confirm order, mark Preparing, generate pick list (grouped by aisle/category), mark Packed | `Ordering`, `Catalog`, `web/admin` | 07, 09 | `OrderingEndpoints.cs`, `AdminPickingModePage.tsx`, `AdminOrderDetailPage.tsx` | `OrderingTests` | `docs/evidence/phase-09/` | Implemented |
| FR-STF-003 | M | Update delivery: Shipping, Delivered (record COD cash collected), DeliveryFailed with reason | `Ordering`, `Inventory`, `Payments`, `web/admin` | 07, 09 | `OrderingEndpoints.cs`, `AdminOrderDetailPage.tsx` | `OrderingTests` | `docs/evidence/phase-09/` | Implemented |
| FR-STF-004 | M | Check stock for a product (search/barcode); see reserved vs on-hand | `Inventory`, `Catalog`, `web/admin` | 06, 09 | `InventoryEndpoints.cs`, `AdminInventoryPage.tsx` | `InventoryTests` | `docs/evidence/phase-09/` | Implemented |
| FR-STF-005 | M | Goods receipt entry when granted `purchasing.receive` | `Purchasing`, `Inventory`, `Catalog`, `web/admin` | 06, 09 | `PurchasingEndpoints.cs`, `AdminPurchasingPage.tsx` | `PurchasingTests` | `docs/evidence/phase-09/` | Implemented |
| FR-STF-006 | S | Mobile/tablet-friendly "picking mode" | `Ordering`, `web/admin` | 09 | `AdminPickingModePage.tsx` | `FrontendBuild` | `docs/evidence/phase-09/` | Implemented |

## 4. Business Rules (`BR-*`)

| ID | Rule Summary | Owning Module(s) | Target Phase | Code Path | Test Name | Evidence File | Status |
|---|---|---|---|---|---|---|---|
| BR-01 | Money is integer VND (`long`/`bigint`); `SellingPrice <= ListPrice`; moving-average cost (`catalog.cost.read`); audited price/cost history | `SharedKernel`, `Catalog`, `Purchasing`, `Audit` | 04, 06 | — | — | — | NotStarted |
| BR-02 | `available = onHand - reserved`; atomic reserve at placement, release on cancel/expiry, deduct on Shipping with unit-cost snapshot | `Inventory`, `Ordering` | 06, 07 | — | — | — | NotStarted |
| BR-03 | Server-side totals only (`subtotal - couponDiscount + shippingFee`); line prices snapshotted at placement | `Cart`, `Ordering` | 07 | — | — | — | NotStarted |
| BR-04 | One coupon per order (Percent/FixedAmount/FreeShipping, min subtotal, dates, total/per-user limits, scope) in same transaction | `Promotions`, `Ordering` | 07 | — | — | — | NotStarted |
| BR-05 | Shipping fee by configured province zone + free-shipping threshold | `Ordering` | 07 | — | — | — | NotStarted |
| BR-06 | Payment methods `COD` (up to max order value) and `SEPAY_QR` (auto-expires after window + grace, releases stock) | `Ordering`, `Payments`, `Inventory` | 07, 10 | — | — | — | NotStarted |
| BR-07 | Cancellation rules by status/role; cancelling paid order creates refund request | `Ordering`, `Payments`, `Inventory`, `Promotions`, `Audit` | 07, 10 | — | — | — | NotStarted |
| BR-08 | Order state machine & independent Payment status state machine | `Ordering`, `Payments` | 07, 10 | — | — | — | NotStarted |
| BR-09 | Verified-purchase reviews (`Delivered` order, 1 per order line, moderated, aggregates recomputed) | `Reviews`, `Ordering`, `Catalog` | 08 | `ReviewsService.cs`, `OrderVerificationService.cs` | `dotnet test` | `docs/evidence/phase-08/04-backend-suite-run.txt` | Verified |
| BR-10 | 2-level VN address model (34 provinces/cities + wards/communes effective 01/07/2025); snapshotted on order | `Customers`, `Ordering` | 07 | — | — | — | NotStarted |
| BR-11 | Purchase order states (`Draft -> Ordered -> PartiallyReceived -> Received -> Closed` / `Cancelled`); immutable goods receipts | `Purchasing`, `Inventory`, `Catalog` | 06 | — | — | — | NotStarted |
| BR-12 | Manual stock adjustments require reason and audit log | `Inventory`, `Audit` | 06 | — | — | — | NotStarted |
| BR-13 | Revenue & gross profit (`revenue - COGS`) definitions on `Delivered` orders in `Asia/Ho_Chi_Minh` | `Reporting`, `Ordering` | 09 | — | — | — | NotStarted |
| BR-14 | Guest cannot checkout; Staff created by Admin (or Manager with `staff.create`) | `Identity`, `Ordering` | 05, 07 | — | — | — | NotStarted |
| BR-15 | 4-step refund workflow with four-eyes approval (`refunds.approve` != requester) | `Payments`, `Audit` | 10 | — | — | — | NotStarted |
| BR-16 | Max qty per cart/order line configurable (default 20); stock re-validated at checkout | `Cart`, `Ordering`, `Inventory` | 07 | — | — | — | NotStarted |
| BR-17 | Vietnamese phone validation (`0xxxxxxxxx` or `+84...`) and normalized storage | `SharedKernel`, `Identity`, `Customers`, `Ordering` | 04, 05, 07 | — | — | — | NotStarted |

## 5. Non-Functional Requirements (`NFR-*`)

| ID | Requirement Summary | Owning Module(s) | Target | Target Phase | Evidence File | Status |
|---|---|---|---|---|---|---|
| NFR-001 | API Performance | `Catalog`, `Ordering`, `Persistence` | p95 < 300 ms catalog reads, < 600 ms checkout @ 50 VUs | 06, 11 | — | NotStarted |
| NFR-002 | Web Performance | `web/storefront`, `web/app` | Mobile Lighthouse Perf >= 85, LCP < 2.5 s, CLS < 0.1 | 08, 11 | — | NotStarted |
| NFR-003 | Security | `Identity`, `Payments`, `Api`, `deploy/nginx` | OWASP ASVS L2 aligned; security rules & gates | 05, 07, 10, 11, 12 | — | NotStarted |
| NFR-004 | Availability | `Api`, `Persistence`, `deploy` | Health checks, graceful shutdown, restart policy, restore drill | 04, 12 | — | NotStarted |
| NFR-005 | Observability | `Api`, `SharedKernel`, `Payments` | Structured JSON logs with correlation id; traceable webhooks | 04, 10 | — | NotStarted |
| NFR-006 | Accessibility | `web/shared`, `web/storefront`, `web/admin` | WCAG 2.2 AA on key flows; axe 0 serious/critical | 08, 09, 11 | — | NotStarted |
| NFR-007 | i18n | `web/shared/i18n`, `SharedKernel` | Vietnamese UI (`vi.ts`), VND integer formatting, `Asia/Ho_Chi_Minh` | 04, 08, 09 | — | NotStarted |
| NFR-008 | Data Integrity | `Persistence`, `Inventory`, `Ordering`, `Payments` | Unique constraints, ACID transactions, concurrency tests | 06, 07, 10, 11 | — | NotStarted |
| NFR-009 | Maintainability | `ArchitectureTests`, `.github/workflows` | Architecture tests, green CI, zero commercial-license packages | 04, 11, 12 | — | NotStarted |
| NFR-010 | Privacy | `Identity`, `Customers`, `Audit` | Minimum PII, Identity password hashing, customer anonymization tool | 05, 09 | — | NotStarted |
