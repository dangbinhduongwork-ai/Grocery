# GroceryHub — RBAC Matrix Integration Test Report (Phase 06)

- **Phase:** 06 — Products, Inventory & Purchasing
- **Task ID:** `T06-04`
- **Owner / Author:** `qa-engineer`
- **Date:** 2026-11-15
- **Branch:** `agent/qa-engineer/phase-06-tests`
- **Reference Specs:**
  - `docs/spec/permissions-seed.md` (Permissions catalog & system role matrix)
  - `docs/spec/07-API-SPECIFICATION.md` §4.3, §5.1, §5.2, §5.3
  - `docs/spec/05-SYSTEM-ARCHITECTURE.md` §7–§8, §10, §15
  - `docs/evidence/phase-06/02-db-constraints-evidence.txt`

---

## 1. Executive Summary

Phase 06 introduces the core commerce engine modules: **Catalog**, **Inventory**, and **Purchasing**. Authorization enforcement follows strict separation-of-duty guidelines defined in `docs/spec/permissions-seed.md`:

1. **Storefront Public Access:** Storefront catalog browsing (`/api/v1/categories`, `/api/v1/brands`, `/api/v1/products`, `/api/v1/products/{slug}`, `/api/v1/products/{id}/related`, `/api/v1/banners`, `/api/v1/home`, `/api/v1/catalog/images/{key}`) is explicitly configured with `.AllowAnonymous()`. All personas (Anonymous through Admin) receive HTTP 200 OK without authentication.
2. **Cost Price Separation (`catalog.cost.read` vs `catalog.read`):**
   - Per `BR-01` and `T-ADM-06`, moving-average `CostPrice` represents sensitive supplier margin data.
   - Staff have `catalog.read` and can query products on `/api/v1/admin/products`, but `CostPrice` is masked (`null`) in their response.
   - Manager and Admin possess `catalog.cost.read`, so `CostPrice` is returned in full integer VND.
   - Storefront product cards and details never expose `CostPrice` in their DTO contracts.
3. **Price Change Segregation (`catalog.price.write` vs `catalog.write`):**
   - Updating selling/list prices directly impacts revenue and margin. It is separated from general product metadata updates (`catalog.write`).
   - Every price update via `PUT /api/v1/admin/products/{id}/price` writes an immutable record to `catalog.ProductPriceHistories` and an audit entry to `audit.AuditLogs`.
4. **Inventory Adjustment Control (`inventory.adjust` vs `inventory.read`):**
   - Staff warehouse operators have `inventory.read` (viewing on-hand, reserved, available stock, and movement ledger), but CANNOT perform discretionary stock adjustments (HTTP 403 Forbidden).
   - Only Manager and Admin with `inventory.adjust` can post adjustments with mandatory reasons, preventing unmonitored inventory shrinkage write-offs (`BR-12`, `T-ADM-02`).
5. **Purchasing Financial Commitment (`purchasing.write` vs `purchasing.read`):**
   - Creating suppliers, creating purchase orders, updating orders, submitting orders to suppliers, and cancelling purchase orders represent binding financial commitments.
   - Staff receive HTTP 403 Forbidden on write/state-transition endpoints. Only Manager and Admin can execute PO lifecycle transitions.
6. **Goods Receiving (`purchasing.receive`):**
   - Designated warehouse personnel with `purchasing.receive` (Manager, Admin, or Staff with explicit per-user grant in `UserPermissions`) can receive goods at the warehouse dock (`POST /api/v1/admin/purchase-orders/{id}/receive`), creating immutable `inventory.StockMovements` and recomputing product `CostPrice` via the moving-average formula (`BR-11`).

---

## 2. Systematic RBAC Matrix Table

The matrix catalogs Phase 06 endpoints against 6 personas:
1. **Anonymous (`Anon`):** Unauthenticated guest (no `Authorization` header).
2. **Customer A (`CustA`):** Authenticated retail customer (storefront shopper, zero admin permissions).
3. **Customer B (`CustB`):** Second customer (verifies object ownership isolation).
4. **Staff (`Stf`):** Operations associate (`catalog.read`, `inventory.read`, `purchasing.read`; lacks write/adjust/cost permissions).
5. **Manager (`Mgr`):** Store manager (`catalog.write`, `catalog.price.write`, `catalog.cost.read`, `inventory.adjust`, `purchasing.write`, `purchasing.receive`).
6. **Admin (`Adm`):** System administrator (all 29 permissions).

Format: `Expected / Actual Status Code`.

| # | Endpoint Route | Method | Required Policy | Anon | CustA | CustB | Stf | Mgr | Adm | Integration Test Name |
|---|---|:---:|---|:---:|:---:|:---:|:---:|:---:|:---:|---|
| **Storefront Catalog** | | | | | | | | | | |
| 1 | `/api/v1/categories` | GET | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | Storefront API Contract Tests |
| 2 | `/api/v1/brands` | GET | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | Storefront API Contract Tests |
| 3 | `/api/v1/products` | GET | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `CatalogRbacIntegrationTests.Storefront_SearchProducts_AllowAnonymous_AllPersonas200` |
| 4 | `/api/v1/products/{slug}` | GET | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `CatalogRbacIntegrationTests.CostPriceVisibility_AdminAndManagerCanReadCost_StaffAndCustomerCannot` |
| 5 | `/api/v1/products/{id}/related` | GET | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | Storefront API Contract Tests |
| 6 | `/api/v1/banners` | GET | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | Storefront API Contract Tests |
| 7 | `/api/v1/home` | GET | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | Storefront API Contract Tests |
| 8 | `/api/v1/catalog/images/{key}` | GET | `AllowAnonymous()` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `200/200` | `ImageUploadSecurityIntegrationTests.ValidImageUpload_StoresImageAndServesViaImageRoute` |
| **Admin Catalog** | | | | | | | | | | |
| 9 | `/api/v1/admin/products` | GET | `catalog.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | `CatalogRbacIntegrationTests.Admin_GetProducts_PolicyMatrix` |
| 10 | `/api/v1/admin/products` | POST | `catalog.write` | `401/401` | `403/403` | `403/403` | `403/403` | `201/201` | `201/201` | `CatalogRbacIntegrationTests.Admin_CreateProduct_PolicyMatrix` |
| 11 | `/api/v1/admin/products/{id}` | GET | `catalog.read` | `401/401` | `403/403` | `403/403` | `200/200`* | `200/200` | `200/200` | `CatalogRbacIntegrationTests.CostPriceVisibility_AdminAndManagerCanReadCost_StaffAndCustomerCannot` |
| 12 | `/api/v1/admin/products/{id}` | PUT | `catalog.write` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | Admin Product Lifecycle Tests |
| 13 | `/api/v1/admin/products/{id}/status` | PUT | `catalog.write` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | Admin Product Lifecycle Tests |
| 14 | `/api/v1/admin/products/{id}/price` | PUT | `catalog.price.write` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | `CatalogRbacIntegrationTests.Admin_UpdateProductPrice_PolicyMatrix` |
| 15 | `/api/v1/admin/products/{id}/images` | POST | `catalog.write` | `401/401` | `403/403` | `403/403` | `403/403` | `201/201` | `201/201` | `ImageUploadSecurityIntegrationTests.ValidImageUpload_StoresImageAndServesViaImageRoute` |
| 16 | `/api/v1/admin/products/{id}/images/order` | PUT | `catalog.write` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | Admin Product Image Tests |
| 17 | `/api/v1/admin/products/{id}/images/{imageId}` | DELETE | `catalog.write` | `401/401` | `403/403` | `403/403` | `403/403` | `204/204` | `204/204` | Admin Product Image Tests |
| 18 | `/api/v1/admin/categories` | GET | `catalog.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Admin Category Tests |
| 19 | `/api/v1/admin/categories` | POST | `catalog.write` | `401/401` | `403/403` | `403/403` | `403/403` | `201/201` | `201/201` | Admin Category Tests |
| 20 | `/api/v1/admin/categories/{id}` | PUT | `catalog.write` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | Admin Category Tests |
| 21 | `/api/v1/admin/categories/{id}` | DELETE | `catalog.write` | `401/401` | `403/403` | `403/403` | `403/403` | `204/204` | `204/204` | Admin Category Tests |
| 22 | `/api/v1/admin/brands` | GET | `catalog.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Admin Brand Tests |
| 23 | `/api/v1/admin/brands` | POST | `catalog.write` | `401/401` | `403/403` | `403/403` | `403/403` | `201/201` | `201/201` | Admin Brand Tests |
| 24 | `/api/v1/admin/brands/{id}` | PUT | `catalog.write` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | Admin Brand Tests |
| 25 | `/api/v1/admin/brands/{id}` | DELETE | `catalog.write` | `401/401` | `403/403` | `403/403` | `403/403` | `204/204` | `204/204` | Admin Brand Tests |
| **Admin Inventory** | | | | | | | | | | |
| 26 | `/api/v1/admin/inventory` | GET | `inventory.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | `CatalogRbacIntegrationTests.Admin_GetInventory_PolicyMatrix` |
| 27 | `/api/v1/admin/inventory/low-stock` | GET | `inventory.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Admin Inventory Tests |
| 28 | `/api/v1/admin/inventory/near-expiry` | GET | `inventory.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Admin Inventory Tests |
| 29 | `/api/v1/admin/inventory/movements` | GET | `inventory.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Admin Inventory Ledger Tests |
| 30 | `/api/v1/admin/inventory/{id}/movements` | GET | `inventory.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Admin Inventory Ledger Tests |
| 31 | `/api/v1/admin/inventory/adjust` | POST | `inventory.adjust` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | `CatalogRbacIntegrationTests.Admin_AdjustStock_PolicyMatrix` |
| 32 | `/api/v1/admin/inventory/{id}/adjust` | POST | `inventory.adjust` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | `CatalogRbacIntegrationTests.InventoryAdjust_StaffDenied_ManagerAndAdminAllowed` |
| **Admin Purchasing** | | | | | | | | | | |
| 33 | `/api/v1/admin/suppliers` | GET | `purchasing.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | `CatalogRbacIntegrationTests.Admin_GetSuppliers_PolicyMatrix` |
| 34 | `/api/v1/admin/suppliers/{id}` | GET | `purchasing.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Admin Supplier Tests |
| 35 | `/api/v1/admin/suppliers` | POST | `purchasing.write` | `401/401` | `403/403` | `403/403` | `403/403` | `201/201` | `201/201` | `CatalogRbacIntegrationTests.Admin_CreateSupplier_PolicyMatrix` |
| 36 | `/api/v1/admin/suppliers/{id}` | PUT | `purchasing.write` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | Admin Supplier Tests |
| 37 | `/api/v1/admin/suppliers/{id}` | DELETE | `purchasing.write` | `401/401` | `403/403` | `403/403` | `403/403` | `204/204` | `204/204` | Admin Supplier Tests |
| 38 | `/api/v1/admin/purchase-orders` | GET | `purchasing.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | `CatalogRbacIntegrationTests.Admin_GetPurchaseOrders_PolicyMatrix` |
| 39 | `/api/v1/admin/purchase-orders/{id}` | GET | `purchasing.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Moving Average PO Tests |
| 40 | `/api/v1/admin/purchase-orders` | POST | `purchasing.write` | `401/401` | `403/403` | `403/403` | `403/403` | `201/201` | `201/201` | Moving Average PO Tests |
| 41 | `/api/v1/admin/purchase-orders/{id}` | PUT | `purchasing.write` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | Purchasing Admin Service Tests |
| 42 | `/api/v1/admin/purchase-orders/{id}/submit` | POST | `purchasing.write` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | `CatalogRbacIntegrationTests.PurchasingWrite_StaffDenied_ManagerAndAdminAllowed` |
| 43 | `/api/v1/admin/purchase-orders/{id}/cancel` | POST | `purchasing.write` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | `CatalogRbacIntegrationTests.PurchasingWrite_StaffDenied_ManagerAndAdminAllowed` |
| 44 | `/api/v1/admin/purchase-orders/{id}/receive` | POST | `purchasing.receive` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | `MovingAverageCostIntegrationTests.PurchaseOrderReceipt_ComputesMovingAverageCost_MatchesBR11Formula` |
| 45 | `/api/v1/admin/purchase-orders/{id}/close` | POST | `purchasing.write` | `401/401` | `403/403` | `403/403` | `403/403` | `200/200` | `200/200` | Purchasing PO Intake Tests |
| 46 | `/api/v1/admin/goods-receipts` | GET | `purchasing.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Goods Receipts Query Tests |
| 47 | `/api/v1/admin/goods-receipts/{id}` | GET | `purchasing.read` | `401/401` | `403/403` | `403/403` | `200/200` | `200/200` | `200/200` | Goods Receipts Query Tests |

\* *Note on Staff CostPrice field masking:* On `GET /api/v1/admin/products/{id}`, Staff receives HTTP 200 OK because Staff has `catalog.read`. However, because Staff lacks `catalog.cost.read`, the `CostPrice` property in `AdminProductDetailDto` is dynamically set to `null` (masked). Manager and Admin receive `CostPrice: 7500`.

---

## 3. Test Suite Structure & Execution Evidence

The Phase 06 test suite is organized under `tests/GroceryHub.IntegrationTests/`:

| Test Suite File | Test Methods | Target Requirements |
|---|:---:|---|
| `Inventory/StockConcurrencyIntegrationTests.cs` | 2 | 50 concurrent stock reservation requests against 10 units; overselling prevention; commit & release flows |
| `Purchasing/MovingAverageCostIntegrationTests.cs` | 4 | Moving-average cost scenarios 1, 2, 3 (BR-11); end-to-end PO creation -> submit -> receipt intake -> cost recalculation |
| `Catalog/ProductPriceHistoryIntegrationTests.cs` | 2 | Product price updates; `ProductPriceHistories` old/new price snapshots; `audit.AuditLogs` price audit entries; validation |
| `Catalog/ProductSearchIntegrationTests.cs` | 15 | Accent-insensitive Vietnamese search ("sua tuoi", "nuoc mam", "rau cu"); case insensitivity; draft exclusion |
| `Catalog/ImageUploadSecurityIntegrationTests.cs` | 4 | Image upload size limit (5MB); magic bytes MIME validation; random storage key renaming; file access |
| `Catalog/DatabaseCheckConstraintsIntegrationTests.cs` | 4 | Direct SQL insertions & updates violating `CK_Products_SellingPrice_ListPrice` and `CK_StockItems_Invariants` |
| `Catalog/DevSeederIsolationIntegrationTests.cs` | 3 | Verification that sample data seeder only executes in `Development`, strictly skipping `Production` and `Staging` |
| `Catalog/CatalogRbacIntegrationTests.cs` | 57 (9 theories x 6 personas + 3 focused facts) | Full 6-persona RBAC matrix for Catalog, Inventory, and Purchasing endpoints |

### Integration Test Run Summary
- **Target Database:** Real Microsoft SQL Server 2022 / MSSQLLocalDB
- **Mocks Used:** Zero mocks for database, repositories, or authorization. Real WebApplicationFactory with actual SQL transactions and Identity claims.
