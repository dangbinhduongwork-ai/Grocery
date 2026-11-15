# Database Engineer Notes — Phase 06

## Summary
Implemented schemas, entity configurations, EF Core migrations, performance benchmarks, and atomic persistence primitives for the `catalog`, `inventory`, and `purchasing` schemas in accordance with `docs/spec/06-DATABASE-ERD.md` §3.3, §3.4, §3.5 and task `T06-01`.

## Deliverables Completed

### 1. Catalog, Inventory & Purchasing Entities & Configurations
- **Schema `catalog`:**
  - `Category` (`catalog.Categories`): Self-referencing tree structure, `CK_Categories_NoSelfParent` check constraint, filtered index on active categories.
  - `Brand` (`catalog.Brands`): Unique constraints on `Name` and `Slug`, filtered index on active brands.
  - `Product` (`catalog.Products`): Money columns `bigint` (VND), `RowVersion` concurrency token, check constraints:
    - `CHECK (SellingPrice >= 0)`
    - `CHECK (ListPrice >= 0)`
    - `CHECK (SellingPrice <= ListPrice)`
    - `CHECK (CostPrice >= 0)`
    - `CHECK (Status IN ('Draft', 'Active', 'Archived'))`
    - `CHECK (RatingAvg >= 0.00 AND RatingAvg <= 5.00 AND RatingCount >= 0 AND SoldCount >= 0)`
    - `CHECK (SpecsJson IS NULL OR ISJSON(SpecsJson) = 1)`
    - Covering index `IX_Products_SearchText` filtered on active non-deleted products.
  - `ProductImage` (`catalog.ProductImages`): Unique primary thumbnail constraint per product.
  - `ProductPriceHistory` (`catalog.ProductPriceHistory`): Price audit trail.
  - `Banner` (`catalog.Banners`): Schedule check constraint `CK_Banners_Schedule`.
- **Schema `inventory`:**
  - `StockItem` (`inventory.StockItems`): ProductId as PK, optimistic `RowVersion`, check constraint:
    - `CHECK (OnHand >= 0 AND Reserved >= 0 AND Reserved <= OnHand)`
  - `StockMovement` (`inventory.StockMovements`): Immutable ledger with bigint identity PK, check constraints:
    - `CHECK (Type IN ('PurchaseReceipt', 'Sale', 'Adjustment', 'Return', 'Waste'))`
    - `CHECK (OnHandBefore >= 0 AND OnHandAfter >= 0 AND UnitCost >= 0)`
  - `StockReservation` (`inventory.StockReservations`): Status check constraint (`Active`, `Released`, `Committed`), quantity check (`> 0`), unique constraint `(OrderId, ProductId)`.
- **Schema `purchasing`:**
  - `Supplier` (`purchasing.Suppliers`): Unique Code and Name, soft-delete support.
  - `PurchaseOrder` (`purchasing.PurchaseOrders`): Sequence-backed `PoNumber`, `RowVersion` concurrency token, check constraints:
    - `CHECK (Status IN ('Draft', 'Submitted', 'PartiallyReceived', 'Received', 'Closed', 'Cancelled'))`
    - `CHECK (TotalAmount >= 0)`
  - `PurchaseOrderItem` (`purchasing.PurchaseOrderItems`): Quantity and cost check constraints.
  - `GoodsReceipt` (`purchasing.GoodsReceipts`): Immutable header.
  - `GoodsReceiptItem` (`purchasing.GoodsReceiptItems`): Received lines with quantity and unit cost constraints.

### 2. Atomic Stock & Purchasing Primitives
- `StockPersistenceService`:
  - `ReserveStockAsync`: Atomic conditional `ExecuteUpdateAsync` (`WHERE ProductId = @id AND OnHand - Reserved >= @qty`). Deadlock-free item ordering. Rolls back on any affected rows < 1. Inserts `StockReservations`.
  - `ReleaseStockReservationAsync`: Atomically decrements `Reserved` and marks reservations `Released`.
  - `CommitStockReservationAsync`: Atomically deducts `OnHand` and `Reserved`, logs `Sale` ledger movement with cost snapshot, and marks reservations `Committed`.
- `PurchasingPersistenceService`:
  - `ExecuteGoodsReceiptIntakeAsync`: Atomic goods receipt intake transaction. Creates `GoodsReceipt` & items, updates `PurchaseOrderItems.ReceivedQuantity`, computes moving-average cost per BR-11 (`NewCost = CurrentOnHand <= 0 ? ReceivedUnitCost : (CurrentOnHand * CurrentCost + ReceivedQty * ReceivedUnitCost) / (CurrentOnHand + ReceivedQty)` with round-to-nearest integer VND), updates `catalog.Products.CostPrice`, increments `inventory.StockItems.OnHand`, appends `PurchaseReceipt` movement to ledger, and transitions PO status (`Received` or `PartiallyReceived`).

### 3. Benchmark & Verification Evidence
- **Seed & Benchmark Run:** 20,000 realistic Vietnamese products seeded into MSSQLLocalDB.
- **Search Queries Evaluated:** "sua tuoi", "rau cu", "nuoc mam".
  - "sua tuoi": p95 = 2.20 ms (Target < 300 ms — PASS)
  - "rau cu": p95 = 1.86 ms (Target < 300 ms — PASS)
  - "nuoc mam": p95 = 2.73 ms (Target < 300 ms — PASS)
- **Evidence File:** `docs/evidence/phase-06/01-perf-seed-search-benchmark.txt`.
- **Concurrency Test Drill:** 2 concurrent tasks racing to reserve 7 units each on an item with OnHand = 10. Exactly 1 succeeded, 1 failed. Zero overselling.

### 4. Migration
- Migration generated: `20261115071009_CatalogInventoryPurchasing`.
