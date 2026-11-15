# Phase 07 — Customers/Addresses, Shipping, Coupons, Cart, Orders Completion Report

- **Phase:** 07 — Customers/Addresses, Shipping, Coupons, Cart, Orders
- **Status:** **DONE / GO**
- **Date:** 2026-11-15
- **Lead / Integration Owner:** `project-manager`
- **Contributing Role Agents:** `database-engineer`, `backend-developer`, `qa-engineer`, `security-reviewer`
- **Inputs:** `FR-CUS-005`, `FR-CUS-008`, `FR-CUS-009`, `FR-CUS-010`, `FR-CUS-011`, `FR-CUS-012`, `FR-CUS-014`, `FR-CUS-015`, `FR-ADM-007`, `FR-ADM-012`, `FR-ADM-017`, `FR-STF-001`..`003`, `BR-02`..`BR-08`, `BR-10`, `BR-16`, `BR-17`, `docs/spec/05-SYSTEM-ARCHITECTURE.md` §9–11, `docs/spec/06-DATABASE-ERD.md`, `docs/spec/07-API-SPECIFICATION.md`.
- **Requirements Satisfied:** All above functional and non-functional requirements.

---

## 1. Executive Summary

Phase 07 successfully implemented the complete backend ordering and COD purchasing flow in four internal steps (7A Customers & Shipping, 7B Promotions & Coupons, 7C Shopping Cart, 7D Checkout & Order State Machine):

1. **Step 7A: Customers, Administrative Units & Shipping (`database-engineer` + `backend-developer`):**
   - Vendored Vietnam 2-level administrative dataset (34 provinces, 3,321 wards) seeded idempotently with metadata tracked in `shared.AdminUnitDataVersion` (`VN-2LEVEL-2025-07-01`, `MIT`).
   - Public read endpoints `GET /api/v1/locations/provinces` and `GET /api/v1/locations/provinces/{code}/wards` marked `.AllowAnonymous()`.
   - Customer address management (`GET/POST/PUT/DELETE /api/v1/me/addresses*`): enforces maximum 10 addresses per user, strict single default address via filtered unique index `UQ_Addresses_UserId_Default`, and Vietnamese phone number validation per `BR-17`.
   - Shipping fee calculation engine with shipping zones (Zone 1 HCM 25,000 VND, Zone 2 neighboring provinces 35,000 VND) and free shipping threshold at 300,000 VND (`BR-05`). Admin zone/rate management restricted to `settings.manage`.

2. **Step 7B: Promotions & Coupons (`database-engineer` + `backend-developer`):**
   - Full promotion schema in `promotions` (`Coupons`, `CouponScopes`, `CouponRedemptions`) with DB check constraints on coupon types (`Percent`, `FixedAmount`, `FreeShipping`), `StartsAt < EndsAt`, `MinSubtotal`, and usage limits.
   - Coupon validation engine implementing `BR-04`: validates date range, min subtotal, global usage limit, per-user redemption limit, and single coupon per order rule (`DEC-011`/`Q-009`).
   - Proportional item discount allocation engine (`BR-13`) distributing order-level discounts fairly across product lines while guaranteeing integer VND arithmetic without rounding loss.
   - Admin coupon CRUD restricted to `promotions.manage`.

3. **Step 7C: Shopping Cart (`backend-developer`):**
   - Server-side shopping cart with guest tracking via signed HTTP-only cookie (`gh_cart`) and user-linked cart for authenticated shoppers.
   - Seamless cart merge upon customer login in `AuthService`, capping items at `min(available, 20)` per line (`BR-16`).
   - Real-time stock availability and catalog price revalidation on cart retrieval (`GET /api/v1/cart`).
   - Applying coupons to carts strictly requires authenticated customer session (`DEC-011`/`Q-008`).

4. **Step 7D: Checkout, Order State Machine & COD Payments (`backend-developer` + `database-engineer`):**
   - Authoritative checkout quote (`POST /api/v1/checkout/quote`) computing real-time subtotal, shipping fee, coupon discount, and payable total. Client-supplied prices and totals are strictly ignored.
   - Idempotent order placement (`POST /api/v1/checkout/orders`) requiring `Idempotency-Key` header with SHA256 request body hash checking: cached 201 Created on identical replay, HTTP 409 Conflict with `IDEMPOTENCY_CONFLICT` on mismatched payload.
   - Atomic stock reservation (`IStockPersistenceService.ReserveStockAsync`) executing conditional `ExecuteUpdateAsync` with rollback on insufficient stock (`INSUFFICIENT_STOCK`).
   - Vietnamese order numbers (`ORD-YYYYMMDD-NNNNNN`) and payment codes (`GHNNNNNNNNNN`) generated sequentially from SQL Server sequence objects (`ordering.OrderNumberSeq`, `payments.PaymentCodeSeq`).
   - Order state machine with strict role and permission segregation:
     - `orders.process`: `Placed -> Confirmed -> Preparing -> Packed`, early unpaid cancel.
     - `orders.ship`: `Packed -> Shipping` (commits stock to `Sale` movement with unit cost snapshot), `Shipping -> Delivered` (records physical cash collection in `payments.CodCollections`), `Shipping -> DeliveryFailed`.
     - `orders.cancel`: Mid-state and paid cancellations (Manager / Admin only).
     - Customer cancellation strictly guarded: allowed only in `PendingPayment`, `Placed`, `Confirmed`; rejected once warehouse picking starts (`Preparing`).
   - Feature flag: `SEPAY_QR` explicitly returns HTTP 409 Conflict with code `PAYMENT_METHOD_UNAVAILABLE` until Phase 10 (no fake payments).

5. **QA Verification & Scripted Purchasing Flow (`qa-engineer`):**
   - End-to-end scripted purchasing flow passed across all 12 stages, recording full execution trace in `docs/evidence/phase-07/01-scripted-e2e-run.txt`.
   - Concurrency stress tests: 20 parallel threads racing for 1 unit of stock resulted in exactly 1 success and 19 failures (`INSUFFICIENT_STOCK`) with zero oversell; 10 parallel threads racing for coupon with `UsageLimit = 1` resulted in exactly 1 success and 9 failures.
   - Systematic RBAC matrix report completed covering all Phase 07 endpoints across 6 personas: `docs/security/rbac-test-phase-07.md`.

6. **Security Audit & Sign-off (`security-reviewer`):**
   - Complete security audit in `docs/security/review-phase-07.md`: final verdict **GO** (0 Critical, 0 High, 0 Medium, 0 Low).
   - Rate limiter middleware pipeline fixed: `UseRateLimiter()` placed after `UseAuthentication()`, partitioning checkout limits by `ClaimTypes.NameIdentifier`.
   - Dependency scan: 0 vulnerable packages across all 18 projects.
   - Reality check (`scripts/verify-reality.ps1`): 0 violations.

---

## 2. Defects Discovered & Resolved During Integration

| Issue ID | Affected Component | Root Cause | Resolution | Verified By |
|---|---|---|---|---|
| **CR-003** | `StockPersistenceService.cs`, `PurchasingPersistenceService.cs` | Calling unconditional `BeginTransactionAsync()` when an ambient transaction was already opened by `OrderingService.PlaceOrderAsync` caused EF Core `InvalidOperationException: The connection is already in a transaction`. | Updated persistence services to check `_dbContext.Database.CurrentTransaction is null` before opening a new transaction; ambient transactions are reused seamlessly without nested conflicts. | `OrderLifecycleScriptedIntegrationTests`, `CheckoutConcurrencyIntegrationTests` |
| **CR-004** | `CustomersService.cs` | Deleting a default address updated `address.IsDefault = false` and `nextDefault.IsDefault = true` in a single `SaveChangesAsync()`, triggering SQL Server unique constraint violation `UQ_Addresses_UserId_Default` due to statement ordering. | Split operation into two sequential saves: clear old default first, then promote next default. | `CustomerAndCartIntegrationTests` |
| **BUG-003** | `OrderingService.cs` | EF Core `SqlQueryRaw<long>("SELECT NEXT VALUE FOR [ordering].[OrderNumberSeq]")` wraps query in a derived table `SELECT [t].[Value] FROM (...) AS [t]`, triggering SQL Server Error 11719 (`NEXT VALUE FOR function is not allowed in sub-queries or derived tables`). | Replaced with direct ADO.NET scalar command (`cmd.ExecuteScalarAsync`) executing the statement as a top-level SQL query within the ambient transaction. | `OrderLifecycleScriptedIntegrationTests`, `IdempotencyIntegrationTests` |
| **BUG-004** | `Program.cs`, `RateLimitingExtensions.cs` | `app.UseRateLimiter()` was invoked before `app.UseAuthentication()`, causing `httpContext.User` to be unauthenticated during rate limit evaluation. All test requests fell back to shared IP bucket and were throttled to 10 permits/min instead of per user. | Reordered middleware to `UseSerilogRequestLogging() -> UseAuthentication() -> UseAuthorization() -> UseRateLimiter()`, and added `ClaimTypes.NameIdentifier` check in rate limit partition key. | `CheckoutConcurrencyIntegrationTests` (20 parallel requests succeeded in 1 + 19 pattern) |
| **BUG-005** | `OrderingService.cs` | Adding new `OrderStatusHistory` entities to `order.StatusHistory` on an already tracked `Order` caused EF Core `DetectChanges()` to attach them as `EntityState.Modified` instead of `Added`, failing with `DbUpdateConcurrencyException` (0 rows affected). | Explicitly added new history entities via `_dbContext.OrderStatusHistories.Add(history)` across all status transitions, ensuring EF Core marks their state as `Added`. | `OrderLifecycleScriptedIntegrationTests`, `OrderStateMachineIntegrationTests` |

---

## 3. Test Suite & Verification Summary

```text
Solution Build: 0 Warning(s), 0 Error(s) (dotnet build GroceryHub.sln -warnaserror)
Code Format: 100% compliant (dotnet format --verify-no-changes)
Reality Verification: scripts/verify-reality.ps1 -> 0 violations
Vulnerability Scan: dotnet list package --vulnerable -> 0 vulnerable packages

Automated Test Results:
- GroceryHub.UnitTests: 205 passed, 0 failed, 0 skipped
- GroceryHub.ArchitectureTests: 6 passed, 0 failed, 0 skipped
- GroceryHub.IntegrationTests: 188 passed, 0 failed, 0 skipped
Total Test Suite: 399 PASSED / 0 FAILED / 0 SKIPPED (100% PASS RATE)
```

---

## 4. Phase 07 Deliverables Checklist

- [x] Vietnam 2-level administrative dataset (34 provinces, 3,321 wards) + public locations API
- [x] Address CRUD with max 10 limit, single default index, and Vietnamese phone validation (`BR-17`)
- [x] Shipping zones, shipping fee calculation, and free shipping threshold (300k VND)
- [x] Promotion coupons schema, check constraints, validation engine (`BR-04`), and proportional discount allocation (`BR-13`)
- [x] Shopping cart module with guest cookie (`gh_cart`), user cart, login cart merge, and live price/stock revalidation
- [x] Checkout quote endpoint (`POST /checkout/quote`) with server-authoritative calculations
- [x] Idempotent order placement (`POST /checkout/orders`) with `Idempotency-Key` hash verification
- [x] Atomic stock reservation with ambient transaction support and race protection
- [x] Sequential order number (`ORD-YYYYMMDD-NNNNNN`) and payment code generation
- [x] Order lifecycle state machine with granular role/permission segregation (`orders.process`, `orders.ship`, `orders.cancel`)
- [x] COD cash collection recording (`payments.CodCollections`) upon delivery
- [x] `SEPAY_QR` feature flag returning 409 `PAYMENT_METHOD_UNAVAILABLE`
- [x] OpenAPI specification exported to `docs/contracts/openapi.json`
- [x] Full execution trace recorded in `docs/evidence/phase-07/01-scripted-e2e-run.txt`
- [x] Systematic RBAC report: `docs/security/rbac-test-phase-07.md`
- [x] Security review report: `docs/security/review-phase-07.md` (Verdict: GO)
- [x] Architecture Decision Record: `DEC-017` in `docs/progress/DECISIONS.md`
- [x] Master progress updated: `docs/progress/PROGRESS.md` (Phase 07 = Done / GO)

---

## 5. Phase Gate Decision: GO

Phase 07 is complete, verified by 399 real tests running against SQL Server LocalDB, and ready for integration into `main`. Tag: `phase-07-done`.
