# GroceryHub — Permissions Seed & RBAC Matrix (`v1.0`)

- **Version:** `1.0` (Frozen at Phase 03 Gate — changes require a Change Request)
- **Target Schema & Tables:** `identity.Roles`, `identity.Permissions`, `identity.RolePermissions`, `identity.UserPermissions`
- **Owner:** `project-manager` (implemented by `database-engineer` in `IdentitySeed` during Phase 04/05)
- **References:** `docs/spec/07-API-SPECIFICATION.md` §2, `docs/spec/02-PROJECT-REQUIREMENTS.md` §3, `docs/progress/DECISIONS.md` (`DEC-011`), `docs/security/threat-model.md` (`T-ID-03`, `T-ADM-01`..`T-ADM-06`)

---

## 1. Seeded System Roles (`identity.Roles`)

| Role Name | NormalizedName | Built-in / Immutable | Description & Default Scope |
|---|---|:-:|---|
| `Customer` | `CUSTOMER` | Yes | Registered storefront shopper. Zero admin/staff permissions (`RolePermissions` is empty). Access to `/me/*`, `/cart/*`, `/checkout/*`, and `/payments/{code}/status` is governed by `auth` policy + strict object-ownership checks (`UserId == CurrentUser.Id`). |
| `Staff` | `STAFF` | Yes | Warehouse picker, packer, and delivery operator. Can read catalog/inventory/purchasing/customers, process and ship orders, cancel unpaid early-stage orders (`PendingPayment`/`Placed`/`Confirmed` per `DEC-011`/`Q-006`), and request refunds (`refunds.request`). May receive per-user extra permission `purchasing.receive` via `identity.UserPermissions`. |
| `Manager` | `MANAGER` | Yes | Store operations manager. Has all `Staff` permissions plus catalog/price/cost management, manual stock adjustment, PO creation & goods receipt, late/paid order cancellation (`orders.cancel`), customer lock/unlock, staff read, payment anomaly review (`payments.review`), refund approval (`refunds.approve` with `RequestedBy != DecidedBy`), promotions, review moderation, revenue/profit reports, and audit log viewing. May receive per-user `staff.create` via `identity.UserPermissions`. |
| `Admin` | `ADMIN` | Yes | System owner. Seeded with **all 29 permissions** (`RolePermissions`) and manages staff accounts (`staff.manage`), role permission assignments (`roles.manage`), customer PII anonymization, and store/SePay/shipping settings (`settings.manage`). |

> [!IMPORTANT]
> **Guest (Unauthenticated):** Not a database role. Endpoints accessible to Guests explicitly declare `.AllowAnonymous()` (enforced by Architecture Test #3 in `docs/spec/05-SYSTEM-ARCHITECTURE.md` §15). Guest carts use the `gh_cart` `HttpOnly; Secure; SameSite=Lax` cookie; coupon application (`POST/DELETE /cart/coupon`) and checkout (`POST /checkout/*`) require an authenticated `Customer` (`DEC-011` / `Q-008`, `BR-14`).

---

## 2. Complete Permission Catalog & Role Matrix (29 Permissions)

Every permission row below is idempotently seeded into `identity.Permissions(Code, Group, Description)` and mapped in `identity.RolePermissions(RoleId, PermissionCode)`.

| # | Permission Code | Group | Staff | Manager | Admin | Per-User Grant (`UserPermissions`) Allowed? | Security & Business Rationale |
|---:|---|---|:-:|:-:|:-:|:-:|---|
| 1 | `catalog.read` | `Catalog` | ✓ | ✓ | ✓ | Yes | View admin product/category/brand lists (excluding `CostPrice`, which requires `catalog.cost.read`). Needed by Staff to look up SKUs/barcodes during picking (`FR-STF-004`). |
| 2 | `catalog.write` | `Catalog` | – | ✓ | ✓ | Yes | Create/update products, categories, brands, product images, and publish/hide status (`FR-ADM-002`, `FR-ADM-003`). Restricted to Manager/Admin to prevent unauthorized catalog tampering. |
| 3 | `catalog.price.write` | `Catalog` | – | ✓ | ✓ | No | Update `ListPrice` and `SellingPrice` (`PUT /admin/products/{id}/price`). Separated from `catalog.write` because price changes directly affect revenue and write to `catalog.ProductPriceHistory` + `audit.AuditLogs` (`BR-01`, `FR-ADM-004`). |
| 4 | `catalog.cost.read` | `Catalog` | – | ✓ | ✓ | No | View `CostPrice` (moving-average cost) on products and inventory movements (`BR-01`, `T-ADM-06`). Staff must not see supplier cost margins unless promoted to Manager. |
| 5 | `inventory.read` | `Inventory` | ✓ | ✓ | ✓ | Yes | View `StockItems` (`OnHand`, `Reserved`, `Available`), low-stock alerts, near-expiry lots, and stock movement ledger (`FR-ADM-005`, `FR-STF-004`). |
| 6 | `inventory.adjust` | `Inventory` | – | ✓ | ✓ | No | Perform manual stock adjustments (`POST /admin/inventory/{productId}/adjust` with mandatory reason) and update `ReorderLevel` (`BR-12`, `T-ADM-02`). Restricted to Manager/Admin to prevent unapproved shrinkage write-offs. |
| 7 | `purchasing.read` | `Purchasing` | ✓ | ✓ | ✓ | Yes | View suppliers, purchase orders (`PurchaseOrders`), and goods receipts (`GoodsReceipts`) (`FR-ADM-006`). Allows Staff to inspect expected deliveries at the warehouse dock. |
| 8 | `purchasing.write` | `Purchasing` | – | ✓ | ✓ | No | Create/edit suppliers and Purchase Orders, transition PO to `Ordered` or `Cancelled` (`FR-ADM-006`, `BR-11`). Financial commitment restricted to Manager/Admin. |
| 9 | `purchasing.receive` | `Purchasing` | *Grant per user* | ✓ | ✓ | **Yes (Staff)** | Post immutable `GoodsReceipts` (`POST /admin/goods-receipts`) which increase `OnHand` and recompute moving-average `CostPrice` (`FR-STF-005`, `BR-01`, `BR-11`). Granted by default to Manager/Admin and selectively granted via `UserPermissions` to designated receiving Staff. |
| 10 | `orders.read` | `Orders` | ✓ | ✓ | ✓ | Yes | View admin order queue, order detail, pick list (`GET /admin/orders/{id}/pick-list`), and delivery note (`FR-ADM-007`, `FR-STF-001`). |
| 11 | `orders.process` | `Orders` | ✓ | ✓ | ✓ | Yes | Confirm `COD` orders (`Placed -> Confirmed`), advance `Confirmed -> Preparing -> Packed`, assign orders, add internal notes, and cancel **unpaid** early-stage orders (`PendingPayment`, `Placed`, `Confirmed`) with mandatory reason (`FR-STF-002`, `DEC-011` / `Q-006`). |
| 12 | `orders.ship` | `Orders` | ✓ | ✓ | ✓ | Yes | Transition `Packed -> Shipping` (commits stock reservation and snapshots `UnitCostSnapshot`), `Shipping -> Delivered` (records `CodCollection` for COD), `Shipping -> DeliveryFailed` (with reason), and `DeliveryFailed -> Shipping` retry (`FR-STF-003`, `BR-02`, `BR-08`). |
| 13 | `orders.cancel` | `Orders` | – | ✓ | ✓ | No | Cancel orders from `Preparing`, `Packed`, or `DeliveryFailed` (returning committed stock if already in `DeliveryFailed`), or cancel any **Paid** order (automatically creating a `Refund` request in `Requested` state) (`BR-07`, `DEC-011` / `Q-006`). |
| 14 | `customers.read` | `Customers` | ✓ | ✓ | ✓ | Yes | View customer list, profile, delivery addresses, and order history (`FR-ADM-008`) for customer support and delivery coordination. |
| 15 | `customers.lock` | `Customers` | – | ✓ | ✓ | No | Lock or unlock customer accounts (`POST /admin/customers/{id}/lock|unlock`) and revoke their active refresh token families (`FR-ADM-008`, `T-ADM-01`). |
| 16 | `staff.read` | `Staff` | – | ✓ | ✓ | No | View staff and manager accounts, employee codes, roles, and extra permissions (`FR-ADM-009`). |
| 17 | `staff.create` | `Staff` | – | *Grant per user* | ✓ | **Yes (Manager)** | Create new `Staff` accounts (`POST /admin/staff`) (`FR-ADM-009`, `BR-14`). Admin has this by default; Admin may grant `staff.create` to a trusted Manager via `UserPermissions` (Manager can only create `Staff`, never `Manager` or `Admin`). |
| 18 | `staff.manage` | `Staff` | – | – | ✓ | No | Create/disable `Manager` or `Staff` accounts, assign roles/extra permissions (`PUT /admin/staff/{id}/roles|permissions`), and execute customer PII anonymization (`POST /admin/customers/{id}/anonymize`) (`FR-ADM-009`, `NFR-010`, `T-ID-03`). Strictly Admin-only. |
| 19 | `roles.manage` | `Roles` | – | – | ✓ | No | View and update the role-permission matrix (`PUT /admin/roles/{id}/permissions`) (`FR-ADM-010`, `T-ID-03`). Strictly Admin-only; writes before/after JSON to `audit.AuditLogs` and invalidates permission cache. |
| 20 | `payments.read` | `Payments` | – | ✓ | ✓ | No | View `Payments`, `PaymentAttempts`, `PaymentTransactions`, `CodCollections`, and `Refunds` (`FR-ADM-011`). |
| 21 | `payments.review` | `Payments` | – | ✓ | ✓ | No | Resolve anomalous `PaymentTransactions` (`NeedsReview`: `UnknownCode`, `WrongAccount`, `Underpaid`, `Overpaid`, `LateAfterExpiry`, `DuplicatePayment`) via `AttachToOrder`, `Refund`, or `Dismiss`, and confirm COD cash handover (`FR-ADM-011`, `T-PAY-05`). |
| 22 | `refunds.request` | `Payments` | ✓ | ✓ | ✓ | Yes | Create a refund request (`POST /admin/refunds`, status `Requested`) for a paid order (`FR-ADM-011`, `BR-15`). |
| 23 | `refunds.approve` | `Payments` | – | ✓ | ✓ | No | Approve, reject, or mark completed a refund (`POST /admin/refunds/{id}/approve|reject|complete`). Enforces **Four-Eyes Rule (`BR-15`, `T-ADM-03`)**: `DecidedBy != RequestedBy` (otherwise returns `409 REFUND_SAME_APPROVER`). |
| 24 | `promotions.manage` | `Promotions` | – | ✓ | ✓ | No | CRUD coupons (`Coupons`, `CouponScopes`), view coupon redemptions, and CRUD homepage banners (`Banners`) (`FR-ADM-012`). |
| 25 | `reviews.moderate` | `Reviews` | – | ✓ | ✓ | Yes | View pending/all product reviews and `approve`, `reject`, or `reply` (`FR-ADM-013`, `BR-09`). |
| 26 | `reports.revenue` | `Reporting` | – | ✓ | ✓ | No | Access admin dashboard KPIs (`GET /admin/reports/dashboard`), revenue reports (`/admin/reports/revenue`), and top-products reports (`/admin/reports/top-products`) (`FR-ADM-001`, `FR-ADM-014`). |
| 27 | `reports.profit` | `Reporting` | – | ✓ | ✓ | No | Access gross profit reports (`GET /admin/reports/profit`: net revenue minus COGS from `UnitCostSnapshot`) and stock valuation (`/admin/reports/stock-valuation`) (`FR-ADM-015`, `BR-13`, `T-ADM-06`). |
| 28 | `audit.read` | `Audit` | – | ✓ | ✓ | No | Query immutable `audit.AuditLogs` (`GET /admin/audit-logs`) (`FR-ADM-016`, `T-ADM-05`). |
| 29 | `settings.manage` | `Settings` | – | – | ✓ | No | Manage store settings (`shared.Settings`: receiving bank account metadata, free-shipping threshold, COD limit, QR expiry, low-stock threshold) and shipping zones/rates (`ordering.ShippingZones`, `ShippingRates`) (`FR-ADM-017`). Strictly Admin-only. |

---

## 3. Resolution & Enforcement Rules (`PermissionAuthorizationHandler`)

1. **Effective Permission Formula:**
   $$\text{EffectivePermissions}(u) = \left(\bigcup_{r \in \text{Roles}(u)} \text{RolePermissions}(r)\right) \cup \text{UserPermissions}(u)$$
2. **Server-Side Resolution with Short Cache TTL:**
   - Access JWT contains `sub` (`UserId`), `role` (role names), and `jti` — **never** the permission array (`docs/spec/05-SYSTEM-ARCHITECTURE.md` §8).
   - `PermissionAuthorizationHandler` checks `u.Status == UserStatus.Active` and `u.DeletedAt == null` and resolves `EffectivePermissions(u)` via `IMemoryCache` keyed by `perm:user:{userId}` with TTL `60 seconds`.
   - Any mutation to `UserRoles`, `RolePermissions`, `UserPermissions`, or `Users.Status` immediately evicts the affected user(s) (or increments the global permission stamp) and logs an entry in `audit.AuditLogs`.
3. **Privilege-Escalation Guardrails (`T-ID-03`, `T-ADM-01`):**
   - Only `Admin` (`roles.manage` / `staff.manage`) can modify `RolePermissions`, assign the `Manager`/`Admin` roles, or grant `UserPermissions`.
   - A `Manager` granted `staff.create` via `UserPermissions` can only create users with the `Staff` role and cannot grant any `UserPermissions`.
   - The `Admin` role's 29 permissions are protected against removal: `PUT /admin/roles/{adminRoleId}/permissions` rejects any payload that omits a permission from `Admin`.
