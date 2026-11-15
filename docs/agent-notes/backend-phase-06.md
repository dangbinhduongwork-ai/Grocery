# Backend Developer Phase 06 Report: Modules & Endpoints (`T06-02`)

- **Role:** `backend-developer`
- **Branch:** `agent/backend-developer/phase-06-catalog-inventory-modules`
- **Date:** 2026-11-15
- **Status:** Complete & Verified

---

## 1. Scope & Deliverables

Implemented the core commerce domain modules (Catalog, Inventory, Purchasing), secure image storage, business rules (`BR-01`, `BR-02`, `BR-11`, `BR-12`), permission-based API endpoints, development seeder (`DEC-008`), comprehensive unit tests, and regenerated `docs/contracts/openapi.json`.

### A. File Storage & Image Upload Security (`NFR-008`)
- **`IFileStorage` & `LocalFileStorage`:** Saves image assets to `uploads/products` (outside web root).
- **`ImageUploadService`:**
  - MIME allowlist: `image/jpeg`, `image/png`, `image/webp`. Blocks SVG, executables, and polyglots.
  - Strict magic byte validation:
    - JPEG: `FF D8 FF`
    - PNG: `89 50 4E 47 0D 0A 1A 0A`
    - WebP: `RIFF....WEBP`
  - Max file size limit: 5 MB (5,242,880 bytes).
  - Cryptographically random storage keys (`Guid.NewGuid().ToString("N") + extension`). Client-provided filenames are never used for storage.
  - Image re-encoding via `SkiaSharp` 2.88.9 (MIT license) to strip EXIF/metadata and defeat polyglot attacks.
  - Recorded dependency in `docs/progress/DEPENDENCIES.md`.

### B. Catalog Module (`src/GroceryHub.Modules.Catalog`)
- **Storefront Endpoints (§4.3):**
  - `GET /api/v1/categories`: hierarchical tree (parent/children) with active product counts.
  - `GET /api/v1/brands`: active brands list.
  - `GET /api/v1/products`: search, filter (`categorySlug`, `brandSlug`, `minPrice`, `maxPrice`, `inStockOnly`), sort (`relevance`, `price_asc`, `price_desc`, `newest`, `best_selling`), pagination. Uses `VietnameseTextNormalizer.NormalizeForSearch` against `SearchText`.
  - `GET /api/v1/products/{slug}`: product detail with images, pack size, unit, brand, description, stock status.
  - `GET /api/v1/products/{id}/related`: related products query.
  - `GET /api/v1/banners` & `GET /api/v1/home`: storefront homepage data.
  - `GET /api/v1/catalog/images/{key}`: secure binary image streaming.
- **Admin Endpoints (§5.1):**
  - `GET /api/v1/admin/products` (`catalog.read`): includes `CostPrice` only if caller has `catalog.cost.read` (`BR-01`).
  - `POST /api/v1/admin/products` (`catalog.write`): enforces `SellingPrice <= ListPrice` (`BR-01`).
  - `PUT /api/v1/admin/products/{id}` (`catalog.write`)
  - `PUT /api/v1/admin/products/{id}/price` (`catalog.price.write`): creates `ProductPriceHistory` and writes audit log via `IAuditLogPort`.
  - `POST /api/v1/admin/products/{id}/images` (`catalog.write`): secure multipart file upload.
  - `DELETE /api/v1/admin/products/{id}/images/{imageId}` (`catalog.write`)
  - `PUT /api/v1/admin/products/{id}/images/reorder` (`catalog.write`)
  - `POST /api/v1/admin/categories`, `PUT /api/v1/admin/categories/{id}`, `DELETE /api/v1/admin/categories/{id}` (`catalog.write`)
  - `POST /api/v1/admin/brands`, `PUT /api/v1/admin/brands/{id}`, `DELETE /api/v1/admin/brands/{id}` (`catalog.write`)
- **Sample Development Seeder (`DEC-008`):**
  - Populates 200 realistic Vietnamese grocery products across 8 aisles (Rau củ quả, Thịt cá trứng, Sữa bơ phô mai, v.v.) with authentic VND prices, units, and stock levels.
  - Strictly gated by `IHostEnvironment.IsDevelopment()` so it cannot run in Staging or Production.

### C. Inventory Module (`src/GroceryHub.Modules.Inventory`)
- **Admin Endpoints (§5.2):**
  - `GET /api/v1/admin/inventory` (`inventory.read`): returns stock items with `OnHand`, `Reserved`, `Available = OnHand - Reserved`, and `ReorderLevel`.
  - `GET /api/v1/admin/inventory/low-stock` (`inventory.read`): items where `Available <= ReorderLevel`.
  - `GET /api/v1/admin/inventory/near-expiry` (`inventory.read`)
  - `POST /api/v1/admin/inventory/{productId}/adjust` & `POST /api/v1/admin/inventory/adjust` (`inventory.adjust`):
    - Validates `OnHand + delta >= Reserved` (`BR-02`, `BR-12`). Returns `INSUFFICIENT_STOCK` conflict if violated.
    - Requires non-empty reason (`Damage`, `CountCorrection`, `Expiry`, `Theft`).
    - Updates `OnHand`, records `StockMovement` (`Type = Adjustment`), writes `AuditLogs`.
  - `GET /api/v1/admin/inventory/movements` & `GET /api/v1/admin/inventory/{productId}/movements` (`inventory.read`): paged ledger queries.
  - `PUT /api/v1/admin/inventory/{productId}/reorder-level` (`inventory.adjust`)
- **Contracts (`IInventoryModuleContract`):**
  - Exposes `GetAvailableStockAsync`, `IsInStockAsync`, `GetAvailableStocksAsync`.

### D. Purchasing Module (`src/GroceryHub.Modules.Purchasing`)
- **Domain Logic:**
  - `PurchaseOrderStateMachine`: manages transitions between `Draft`, `Submitted`/`Ordered`, `PartiallyReceived`, `Received`, `Closed`, and `Cancelled`.
  - `MovingAverageCostCalculator`: calculates moving-average cost with integer VND rounding (`BR-11`).
- **Admin Endpoints (§5.3):**
  - Suppliers: `GET/POST/PUT/DELETE /api/v1/admin/suppliers` & `/api/v1/admin/purchasing/suppliers` (`purchasing.read`, `purchasing.write`).
  - Purchase Orders:
    - `GET /api/v1/admin/purchase-orders` & `/api/v1/admin/purchasing/orders` (`purchasing.read`)
    - `POST /api/v1/admin/purchase-orders` & `/api/v1/admin/purchasing/orders` (`purchasing.write`): creates PO in `Draft`.
    - `GET/PUT /api/v1/admin/purchase-orders/{id}` & `/api/v1/admin/purchasing/orders/{id}` (`purchasing.read`, `purchasing.write`)
    - `POST .../{id}/submit` & `POST .../{id}/order` (`purchasing.write`): transitions `Draft` -> `Submitted`.
    - `POST .../{id}/cancel` (`purchasing.write`): allowed only when `TotalReceived == 0`.
    - `POST .../{id}/receive` (`purchasing.receive`): executes atomic goods receipt intake via `IPurchasingPersistenceService`.
    - `POST .../{id}/close` (`purchasing.write`): closes received orders.
  - Goods Receipts:
    - `GET /api/v1/admin/goods-receipts` & `GET /api/v1/admin/goods-receipts/{id}` (`purchasing.read`)
    - `POST /api/v1/admin/goods-receipts` (`purchasing.receive`)

---

## 2. Test Evidence

### A. Unit Tests (`tests/GroceryHub.UnitTests`)
- **Total Tests:** 151 passed, 0 failed, 0 skipped.
- **Coverage Highlights:**
  - `ImageUploadValidatorTests`: 8 tests verifying magic bytes, MIME allowlist, size limits, and WebP/PNG/JPEG validation.
  - `PurchaseOrderStateMachineTests`: 30 tests exhaustively checking every transition in the lifecycle matrix.
  - `MovingAverageCostCalculatorTests`: 6 tests checking VND integer rounding and on-hand <= 0 edge cases.
  - `InventoryInvariantsTests` & `InventoryValidatorsTests`: 17 tests checking stock adjustment math, reservation invariants, and FluentValidation rules.
  - `PurchasingValidatorsTests`: 9 tests checking supplier, purchase order, and goods receipt request validations.

### B. Architecture Tests (`tests/GroceryHub.ArchitectureTests`)
- **Total Tests:** 6 passed, 0 failed.
  - `Modules_Must_Only_Depend_On_SharedKernel_And_Other_Module_Contracts` (Passed)
  - `Endpoints_Must_Be_Explicitly_Secured_Or_AllowAnonymous` (Passed — default-deny verified for all new endpoints)
  - `Production_Code_Must_Not_Call_DateTime_Now_UtcNow_Or_Thread_Sleep` (Passed)
  - Assembly boundary rules (Passed)

### C. Build & Format Checks
- `dotnet build -warnaserror`: 0 warnings, 0 errors.
- `dotnet format --verify-no-changes`: 0 violations.

---

## 3. OpenAPI Contract
Regenerated `docs/contracts/openapi.json`. Every new endpoint is registered with proper summary, parameter descriptions, request/response schemas, ProblemDetails mappings, and permission policies.
