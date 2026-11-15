# ADR-002: Single `GroceryHubDbContext` with Schema-Per-Module

- **Status:** Accepted
- **Date:** 2026-11-15
- **Deciders:** `backend-developer`, `database-engineer`, `project-manager`
- **References:** `DEC-001`, `DEC-010`, `docs/spec/06-DATABASE-ERD.md`, `docs/spec/05-SYSTEM-ARCHITECTURE.md`

---

## Context

In our modular monolith (`ADR-001`), 12 modules persist data in SQL Server (`DEC-001`, `DEC-010`). We must decide how EF Core 10 `DbContext` instances, database schemas, migrations, and cross-module transactions are structured.

Key constraints:
- Placing an order (`POST /checkout/orders`), processing a verified SePay webhook (`POST /payments/sepay/webhook`), and posting a goods receipt (`POST /admin/goods-receipts`) must commit changes across multiple modules (`ordering`, `inventory`, `promotions`, `payments`, `catalog`, `audit`, `shared.OutboxMessages`) in a **single atomic SQL Server transaction** (`BR-02`, `BR-04`, `BR-06`, `NFR-008`).
- `database-engineer` is the single owner of `src/GroceryHub.Persistence/**` (entity configurations, migrations, seeders) to prevent migration merge conflicts across parallel developer agents (`docs/spec/09-AGENT-ORCHESTRATION.md`).
- Module isolation must still be preserved at the database level so tables are logically grouped and cross-module foreign keys / EF navigation properties are prevented.

---

## Decision

We use a **single `GroceryHubDbContext`** in `src/GroceryHub.Persistence` with **12 logical SQL Server schemas** (schema-per-module):

1. **Database Schemas:**
   - `identity`: `Users`, `Roles`, `UserRoles`, `Permissions`, `RolePermissions`, `UserPermissions`, `RefreshTokens`, `StaffProfiles`
   - `customers`: `Addresses`
   - `catalog`: `Categories`, `Brands`, `Products`, `ProductImages`, `ProductPriceHistory`, `Banners`
   - `inventory`: `StockItems`, `StockMovements`, `StockReservations`, `ExpiryLots`
   - `purchasing`: `Suppliers`, `PurchaseOrders`, `PurchaseOrderLines`, `GoodsReceipts`, `GoodsReceiptLines`
   - `promotions`: `Coupons`, `CouponScopes`, `CouponRedemptions`
   - `cart`: `Carts`, `CartItems`
   - `ordering`: `Orders`, `OrderItems`, `OrderStatusHistory`, `ShippingZones`, `ShippingZoneProvinces`, `ShippingRates`, `IdempotencyKeys`, plus `SEQUENCE ordering.OrderNumberSeq`
   - `payments`: `Payments`, `PaymentAttempts`, `WebhookEvents`, `PaymentTransactions`, `Refunds`, `CodCollections`, plus `SEQUENCE payments.PaymentCodeSeq`
   - `reviews`: `Reviews`
   - `audit`: `AuditLogs`
   - `shared`: `Provinces`, `Wards`, `AdminUnitDataVersion`, `OutboxMessages`, `Settings`
   - *(Note: `Reporting` is a read-only query module with no dedicated write schema; it queries read views/projections across `ordering`, `payments`, `inventory`, and `catalog`.)*

2. **No Cross-Module Navigation Properties or Foreign Keys:**
   - Entities in one module schema reference entities in another module schema **by scalar `Guid` (or code) ID only** (e.g., `ordering.OrderItems.ProductId` has an index `IX_OrderItems_ProductId`, but no EF navigation property to `Catalog.Product` and no cross-schema FK constraint).
   - Foreign keys and EF navigation properties are permitted **only within the same module schema** (plus reference lookups to `shared.Provinces` / `shared.Wards`).

3. **Unified Migration History & Transaction Scope:**
   - All EF Core migrations are generated into `src/GroceryHub.Persistence/Migrations` and bundled into a single self-contained `efbundle` executable for Docker deployment.
   - `READ_COMMITTED_SNAPSHOT ON` is enabled at database creation time to prevent reader/writer blocking.
   - Cross-module operations invoked within a single HTTP request or background job share the same scoped `GroceryHubDbContext` (and `IDbContextTransaction` when explicit atomic SQL `UPDATE` statements are combined with EF changes, plus `shared.OutboxMessages`).

---

## Alternatives Considered

1. **Multiple `DbContext` classes (one `<Module>DbContext` per module):**
   - *Rejected:* Requires coordinating 11+ separate `__EFMigrationsHistory` tables, manual `DbConnection` + `DbTransaction` sharing across multiple `DbContext` instances on every checkout/webhook/receipt call, and complicated cross-schema reporting queries in `Reporting`.
2. **Single `dbo` Schema for all tables:**
   - *Rejected:* Loses clear table ownership visibility in SQL Server Management Studio / logs and makes it harder to audit schema-level access patterns.
3. **Separate SQL Server Databases per Module:**
   - *Rejected:* Requires MS DTC or distributed sagas on Linux SQL Server containers (where MS DTC is unsupported for cross-database EF transactions) and risks exceeding SQL Server Express resource budgets (`DEC-010`).

---

## Consequences

- **Positive:**
  - Atomic ACID transactions across modules (`Ordering` + `Inventory` + `Promotions` + `Payments` + `OutboxMessages`) work natively on a single connection without distributed transaction managers.
  - Single migration bundle (`efbundle`) simplifies CI/CD and production upgrades (`DEC-010`).
  - Clear ownership: `database-engineer` manages all `IEntityTypeConfiguration<T>` classes in `src/GroceryHub.Persistence/Configurations/<Module>/`.
- **Negative / Trade-offs:**
  - Developers must discipline themselves not to query another module's `DbSet<T>` directly from an application service; this is prevented by keeping domain entities `internal` to their owning module and exposing module access via `I<Module>Repository` / contract interfaces, verified by architecture tests (`ADR-001`).
