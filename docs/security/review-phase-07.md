# Phase 07 Security Review & Audit — Customers, Shipping, Coupons, Cart & Ordering

- **Phase:** 07 — Customers/Addresses, Shipping, Coupons, Cart & Ordering
- **Task ID:** `T07-04`
- **Reviewer:** `security-reviewer` (Adversarial Security Reviewer)
- **Date:** 2026-11-15
- **Branch:** `integration/phase-07`
- **Scope:** Complete COD purchasing subsystem: 2-level Vietnamese administrative units, customer profile and address management (max 10, single default invariant, VN phone validation), server-side shopping cart with guest cookie merge on login, promotion coupons validation and proportional allocation (`BR-04`, `BR-13`), checkout quote calculation, atomic stock reservation with race condition prevention, order idempotency replay protection, order lifecycle state machine with granular permission gating, COD cash collection audit, and dependency supply-chain scans.
- **Reference Standards & Specs:**
  - OWASP ASVS v4.0.3 Level 2 (V4 Access Control, V5 Input Validation, V10 Malicious Code, V13 API Security)
  - OWASP API Security Top 10 (2023) — API1 BOLA / IDOR, API3 Broken Object Property Level Authorization, API4 Unrestricted Resource Consumption, API5 Broken Function Level Authorization, API6 Unrestricted Access to Sensitive Business Flows
  - `docs/security/threat-model.md` §4.1 (`TM-ORD-01`..`TM-ORD-06`), §4.2 (`TM-PAY-01`), §4.3 (`TM-PRO-01`..`TM-PRO-03`)
  - `docs/security/rbac-test-phase-07.md`
  - `docs/evidence/phase-07/01-scripted-e2e-run.txt`
  - `docs/spec/07-API-SPECIFICATION.md` §4.2, §4.4, §4.5, §5.4, §5.7
  - `docs/spec/state-machines.md` §1 (Order Lifecycle State Machine)
  - `.agents/rules/security.md`

---

## 1. Executive Summary & Gate Verdict

| Metric | Result | Target / Standard | Status |
|---|:---:|:---:|:---:|
| **Critical Severity Findings** | **0** | 0 | **PASS** |
| **High Severity Findings** | **0** | 0 | **PASS** |
| **Medium Severity Findings** | **0** | 0 | **PASS** |
| **Low / Informational Findings** | **0** | 0 | **PASS** |
| **Architecture Default-Deny Test** | **100% Passed (6/6)** | Enforced on every RouteEndpoint | **PASS** |
| **Unit Tests** | **100% Passed (205/205)** | State machine, coupon allocation, shipping fee, phone regex | **PASS** |
| **Phase 07 Integration Tests** | **100% Passed (188/188)** | Real SQL Server, concurrency checkouts, idempotency replay, IDOR | **PASS** |
| **Vulnerable Dependencies** | **0** | `dotnet list package --vulnerable` clean across 18 projects | **PASS** |
| **Approved Dependency Licenses** | **100% Verified** | Zero commercial packages (no MediatR, AutoMapper, FluentAssertions) | **PASS** |

### **Phase 07 Gate Verdict: GO**
The Customers, Promotions, Cart, Ordering, and minimal Payments (COD) modules fully satisfy all security requirements, business invariants, and architectural constraints. All client-supplied totals and line prices are strictly ignored; the checkout engine derives prices authoritatively from catalog records. Concurrency tests with 20 parallel threads racing for a single unit of stock demonstrated zero overselling, and 10 parallel threads racing for a single coupon redemption demonstrated strict single-use enforcement. IDOR tests proved Customer A cannot read or mutate Customer B's addresses, cart, or orders. All 399 unit, architecture, and integration tests pass with zero failures.

---

## 2. Audited Components & Codebase Inventory

| Subsystem / Component | Source File Path | Key Security Aspects Audited |
|---|---|---|
| **Customers API & Service** | `src/GroceryHub.Modules.Customers/` | IDOR ownership checks on addresses (`address.UserId == currentUserId`), Vietnamese phone regex (`BR-17`), max 10 addresses limit, single default address invariant enforced via filtered unique index `UQ_Addresses_UserId_Default` and two-step soft deletion. |
| **Administrative Units (Locations)** | `src/GroceryHub.Modules.Customers/` | Public read endpoints `GET /locations/provinces` and `GET /locations/provinces/{code}/wards` marked `.AllowAnonymous()`. 34 provinces and 3,321 wards seeded idempotently with source dataset metadata (`shared.AdminUnitDataVersion`). |
| **Cart API & Service** | `src/GroceryHub.Modules.Cart/` | Guest cart tracked via signed `gh_cart` cookie. Authenticated cart merged on login in `AuthService` with `min(available, 20)` quantity cap (`BR-16`). Real-time price and stock revalidation on `GET /cart`. Coupon application restricted to authenticated customers (`DEC-011`/`Q-008`). |
| **Promotions API & Service** | `src/GroceryHub.Modules.Promotions/` | Admin coupon CRUD protected by `promotions.manage`. Check constraints on coupon type (`Percent`, `FixedAmount`, `FreeShipping`), `StartsAt < EndsAt`, `MinSubtotal`, `MaxDiscountAmount`. Server-side proportional discount allocation (`BR-13`). |
| **Ordering API & Checkout Service** | `src/GroceryHub.Modules.Ordering/` | Server-authoritative checkout quote. Strict client price tampering rejection. Mandatory `Idempotency-Key` header with request hash verification (`IDEMPOTENCY_CONFLICT` on mismatched body, cached 201 on identical replay). |
| **Stock Persistence Service** | `src/GroceryHub.Persistence/Services/` | Race-safe stock reservation using `ExecuteUpdateAsync` with `OnHand - Reserved >= Quantity` affected row verification. Support for ambient transactions across checkout pipeline. Stock committed to `Sale` movement with unit cost snapshot on `Shipping`. |
| **Order State Machine** | `src/GroceryHub.Modules.Ordering/Domain/OrderStateMachine.cs` | Guarded transitions. Segregation of duties: `orders.process` (early forward), `orders.ship` (dispatch & delivery), `orders.cancel` (mid/late cancel). Customer cancel restricted to `Placed`/`Confirmed`; rejected once picking starts (`Preparing`). |
| **COD Payments** | `src/GroceryHub.Modules.Payments/` | Minimal COD lifecycle: created as `Unpaid`, updated to `Paid` only upon physical cash handover recorded in `payments.CodCollections`. Maximum COD threshold (2,000,000 VND per `BR-05`). Feature flag `SEPAY_QR` returns 409 `PAYMENT_METHOD_UNAVAILABLE` until Phase 10. |
| **Rate Limiter Pipeline** | `src/GroceryHub.Api/Program.cs` | Middleware order corrected: `UseAuthentication()` -> `UseAuthorization()` -> `UseRateLimiter()`. Checkout endpoint rate limited per authenticated user (`ClaimTypes.NameIdentifier`) or fallback to IP. |

---

## 3. Threat Model Verification (`docs/security/threat-model.md`)

### 3.1 TM-ORD-01: Client Price and Total Tampering
- **Threat:** Malicious shopper modifies client HTTP request payload to send `UnitPrice = 1000`, `Subtotal = 1000`, or `Total = 1000` to purchase expensive groceries for nominal cost.
- **Audited Controls:**
  - `PlaceOrderRequest` DTO contains only `AddressId`, optional `CouponCode`, `PaymentMethod`, and `Note`.
  - Endpoint handler ignores any client-supplied totals and invokes `CalculateQuoteInternalAsync`.
  - All product line items are fetched directly from database (`Products.SellingPrice`).
  - Shipping fee is computed by server from verified recipient province code and subtotal.
  - Coupon discount is verified and calculated by server engine.
- **Verification Evidence:** `PriceTamperingIntegrationTests.ClientAttemptsPriceTampering_InjectedPricesAndTotalsAreIgnored_AuthoritativeCatalogPricesUsed` verified that forged client request totals are completely ignored and the order is created with authoritative database totals.

### 3.2 TM-ORD-02: Race Condition / Stock Overselling
- **Threat:** Multiple concurrent checkouts race for the last unit of limited stock, leading to negative inventory or double commitment.
- **Audited Controls:**
  - `StockPersistenceService.ReserveStockAsync` executes an atomic database update:
    ```csharp
    int affected = await _dbContext.StockItems
        .Where(s => s.ProductId == item.ProductId && s.OnHand - s.Reserved >= item.Quantity)
        .ExecuteUpdateAsync(s => s.SetProperty(b => b.Reserved, b => b.Reserved + item.Quantity), ct);
    ```
  - If `affected < 1`, the entire transaction rolls back and returns HTTP 409 `INSUFFICIENT_STOCK`.
  - SQL Server `CK_StockItems_Invariants` constraint ensures `OnHand >= Reserved` and `Reserved >= 0` at the database level.
- **Verification Evidence:** `CheckoutConcurrencyIntegrationTests.LastUnitStock_20ParallelCheckouts_Exactly1Succeeds_19FailWithInsufficientStock_NoOversell` ran 20 concurrent customer checkouts against 1 available unit. Exactly 1 succeeded (201 Created), and 19 failed with 409 Conflict (`INSUFFICIENT_STOCK`). Zero oversell occurred.

### 3.3 TM-ORD-03: Insecure Direct Object Reference (IDOR) on Orders and Addresses
- **Threat:** Customer A accesses or modifies Customer B's saved delivery addresses, cart contents, or order history by guessing or manipulating GUID parameters.
- **Audited Controls:**
  - Address endpoints (`PUT/DELETE /api/v1/me/addresses/{id}`) check `address.UserId == userId`.
  - Order endpoints (`GET /api/v1/me/orders/{id}`, `POST /api/v1/me/orders/{id}/cancel`, `POST /api/v1/me/orders/{id}/reorder`) verify `order.UserId == userId`.
  - Order checkout rejects using an `AddressId` that belongs to another customer.
- **Verification Evidence:** `OrderOwnershipIntegrationTests.Idor_CustomerA_CannotViewOrMutate_CustomerB_Orders_Addresses_OrCart` passed: Customer A received 404/403 when attempting to access Customer B's addresses and orders.

### 3.4 TM-ORD-04: Idempotency Key Replay & Replay Hijacking
- **Threat:** Network retry or malicious replay duplicates orders, charges customer twice, or alters order payload under existing key.
- **Audited Controls:**
  - Checkout requires `Idempotency-Key` header (scope: `checkout:{userId}`).
  - Request body hash (SHA256) is recorded alongside cached HTTP response in `ordering.IdempotencyRecords`.
  - Identical key + identical body -> returns stored 201 Created without re-executing order creation or reserving additional stock.
  - Identical key + different body -> returns HTTP 409 Conflict with code `IDEMPOTENCY_CONFLICT`.
- **Verification Evidence:** `IdempotencyIntegrationTests.ReplaySameIdempotencyKey_IdenticalBody_ReturnsSameOrder201_SingleOrderInDb` and `ReplaySameIdempotencyKey_DifferentBody_Returns409Conflict_IdempotencyConflict` passed.

### 3.5 TM-PRO-01: Coupon Reuse & Limit Exhaustion Race
- **Threat:** Attacker uses automation to redeem single-use coupon (`UsageLimit = 1`) simultaneously across multiple sessions.
- **Audited Controls:**
  - `CouponValidationService` checks `coupon.UsedCount < coupon.UsageLimit`.
  - `CouponRedemptions` record is created inside the checkout atomic transaction.
  - Coupon `UsedCount` is updated with concurrency control (`RowVersion`).
- **Verification Evidence:** `CheckoutConcurrencyIntegrationTests.CouponLimit_10ParallelCheckoutsForCouponWithLimit1_Exactly1Succeeds_9Fail` verified that across 10 concurrent requests, exactly 1 redeemed the coupon; 9 were rejected with `COUPON_USAGE_LIMIT_REACHED`.

### 3.6 TM-PAY-01: Fake Payment Processing & Feature Flag Availability
- **Threat:** Client attempts to select `SEPAY_QR` before official integration in Phase 10, or payment status is marked paid without cash collection.
- **Audited Controls:**
  - Selecting `SEPAY_QR` in Phase 07 returns HTTP 409 Conflict with error code `PAYMENT_METHOD_UNAVAILABLE`.
  - COD payment is marked `Paid` only upon staff transition to `Delivered`, recording physical cash amount in `payments.CodCollections`.
- **Verification Evidence:** `CustomerAndCartIntegrationTests.CheckoutQuote_WithSepayQr_ReportsUnavailable_UntilPhase10` passed. Scripted lifecycle test confirmed `CodCollections` record creation with exact total.

---

## 4. Systematic Security Test Results

```text
Build: 0 Warning(s), 0 Error(s) (with -warnaserror)
Code Format: 100% compliant (dotnet format --verify-no-changes)
Vulnerable Packages: 0 vulnerable dependencies found
Reality Verification: scripts/verify-reality.ps1 -> 0 violations

Test Summary:
- GroceryHub.UnitTests: 205 passed, 0 failed, 0 skipped
- GroceryHub.ArchitectureTests: 6 passed, 0 failed, 0 skipped
- GroceryHub.IntegrationTests: 188 passed, 0 failed, 0 skipped
Total Test Suite: 399 PASSED / 0 FAILED / 0 SKIPPED (100% PASS RATE)
```

---

## 5. Security Sign-Off

The security posture for Phase 07 is verified and robust. All IDOR, price tampering, race condition, state machine, idempotency, and coupon abuse threats are mitigated by defense-in-depth server-side controls and verified by automated integration tests.

**Security Reviewer Verdict:** **GO (Approved for Phase 07 Gate & Merge to main)**
