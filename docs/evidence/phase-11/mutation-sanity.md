# GroceryHub — Phase 11 Mutation Sanity Report

**Date:** 2026-11-15  
**Environment:** .NET 10 LTS, SQL Server LocalDB  
**Status:** 8 / 8 Critical Rules Tested and Verified Caught

---

## 1. Mutation Test Summary

In accordance with Phase 11 quality standards (`docs/phase-prompts/phase-11-testing-security.md` and `docs/spec/12-TESTING-STRATEGY.md` §6), 8 critical business and security invariants were broken one at a time in the codebase to prove that automated test suites catch regressions. Every mutated rule resulted in an immediate test failure, demonstrating high fault-detection coverage.

| # | Invariant / Critical Rule | Mutation Applied | Test Triggered | Failure Observed | Reverted Cleanly |
|---|---|---|---|---|:---:|
| 1 | **Stock Concurrency & Invariants** | Removed `OnHand - Reserved >= quantity` check in `StockPersistenceService.ReserveStockAsync` | `StockConcurrencyIntegrationTests.ReserveStock_50ConcurrentRequestsForStockOf10_Exactly10Succeed_40Fail_NoOverselling` | `Microsoft.Data.SqlClient.SqlException: The UPDATE statement conflicted with the CHECK constraint "CK_StockItems_Invariants"` | Yes |
| 2 | **Coupon Usage Limit** | Bypassed `UsageLimit.HasValue && UsedCount >= UsageLimit.Value` check in `CouponValidationEngine.Evaluate` | `CouponCalculationTests.UsageLimits_BlocksWhenLimitExceeded` | `Assert.False() Failure: Expected False, Actual True` | Yes |
| 3 | **Order State Machine Transition Guard** | Returned `Result.Success()` for arbitrary order jumps in `OrderStateMachine.ValidateStaffTransition` | `OrderStateMachineTests.StaffTransition_InvalidJumps_FailsWithConflict` | `Assert.False() Failure: Expected False, Actual True` | Yes |
| 4 | **Object-Level Authorization (IDOR)** | Removed `order.UserId != userId` ownership check in `OrderingService.GetCustomerOrderDetailAsync` | `OrderOwnershipIntegrationTests.Idor_CustomerA_CannotViewOrMutate_CustomerB_Orders_Addresses_OrCart` | `Expected 403 or 404, but received OK` | Yes |
| 5 | **Authoritative Server Pricing** | Substituted line price calculation with static fake price in `OrderingService.GetCheckoutQuoteAsync` | `PriceTamperingIntegrationTests.ClientAttemptsPriceTampering_InjectedPricesAndTotalsAreIgnored_AuthoritativeCatalogPricesUsed` | `Assert.Equal() Failure: Values differ. Expected: 500000, Actual: 2000` | Yes |
| 6 | **SePay Webhook HMAC Signature** | Forced `ValidateWebhook` to always return `true` regardless of signature header in `SePaySignatureValidator` | `SePaySignatureAndUrlTests.T_PAY_04_TamperedRawJsonBody_SignatureValidationFails` | `Assert.False() Failure: Expected False, Actual True` | Yes |
| 7 | **SePay Webhook Amount Matching** | Bypassed `TransferAmount < Payment.Amount` check in `PaymentService.ProcessSePayWebhookAsync` | `SePayPaymentIntegrationTests.T_PAY_08_AmountMismatch_UnderpaidAndOverpaidRecordedForReview` | `Assert.Equal() Failure: Strings differ. Expected: "PendingPayment", Actual: "Confirmed"` | Yes |
| 8 | **Refund Two-Person Rule (BR-15)** | Disabled `refund.RequestedBy == decidedByUserId` check in `PaymentService.ApproveRefundAsync` | `SePayPaymentIntegrationTests.T_PAY_18_TwoPersonRule_RefundSelfApprovalBlockedAndSecondPersonSucceeds` | `Assert.Equal() Failure: Expected Conflict` | Yes |

---

## 2. Detailed Mutation Logs

### Mutation 1: Stock Check & Database Invariants
- **Target File:** `src/GroceryHub.Persistence/Services/StockPersistenceService.cs`
- **Mutation:**
  ```csharp
  // Mutated from:
  .Where(s => s.ProductId == item.ProductId && s.OnHand - s.Reserved >= item.Quantity)
  // To:
  .Where(s => s.ProductId == item.ProductId)
  ```
- **Observed Test Failure:**
  ```text
  [FAIL] GroceryHub.IntegrationTests.Inventory.StockConcurrencyIntegrationTests.ReserveStock_50ConcurrentRequestsForStockOf10_Exactly10Succeed_40Fail_NoOverselling
  Microsoft.Data.SqlClient.SqlException : The UPDATE statement conflicted with the CHECK constraint "CK_StockItems_Invariants". The conflict occurred in database "GroceryHub_IntTests_...", table "inventory.StockItems".
  ```

### Mutation 2: Coupon Usage Limits
- **Target File:** `src/GroceryHub.SharedKernel/Promotions.cs`
- **Mutation:**
  ```csharp
  // Mutated from:
  if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
  // To:
  if (false && coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
  ```
- **Observed Test Failure:**
  ```text
  [FAIL] GroceryHub.UnitTests.Promotions.CouponCalculationTests.UsageLimits_BlocksWhenLimitExceeded
  Assert.False() Failure
  Expected: False
  Actual:   True
  at GroceryHub.UnitTests.Promotions.CouponCalculationTests.UsageLimits_BlocksWhenLimitExceeded() line 161
  ```

### Mutation 3: Order State Machine Transition Guards
- **Target File:** `src/GroceryHub.Modules.Ordering/Domain/OrderStateMachine.cs`
- **Mutation:**
  ```csharp
  // Mutated from:
  return Result.Failure(DomainError.Conflict(ErrorCodes.OrderInvalidTransition, ...));
  // To:
  if (currentStatus.Length > 0) return Result.Success();
  ```
- **Observed Test Failure:**
  ```text
  [FAIL] GroceryHub.UnitTests.Ordering.OrderStateMachineTests.StaffTransition_InvalidJumps_FailsWithConflict(current: "Preparing", target: "Shipping")
  [FAIL] GroceryHub.UnitTests.Ordering.OrderStateMachineTests.StaffTransition_InvalidJumps_FailsWithConflict(current: "Placed", target: "Shipping")
  [FAIL] GroceryHub.UnitTests.Ordering.OrderStateMachineTests.StaffTransition_InvalidJumps_FailsWithConflict(current: "Cancelled", target: "Placed")
  [FAIL] GroceryHub.UnitTests.Ordering.OrderStateMachineTests.StaffTransition_InvalidJumps_FailsWithConflict(current: "Placed", target: "Delivered")
  ```

### Mutation 4: Object-Level Authorization (IDOR)
- **Target File:** `src/GroceryHub.Modules.Ordering/Application/Services/OrderingService.cs`
- **Mutation:**
  ```csharp
  // Mutated from:
  if (order.UserId != userId)
  // To:
  if (false && order.UserId != userId)
  ```
- **Observed Test Failure:**
  ```text
  [FAIL] GroceryHub.IntegrationTests.Ordering.OrderOwnershipIntegrationTests.Idor_CustomerA_CannotViewOrMutate_CustomerB_Orders_Addresses_OrCart
  Expected 403 or 404, but received OK
  at GroceryHub.IntegrationTests.Ordering.OrderOwnershipIntegrationTests.Idor_CustomerA_CannotViewOrMutate_CustomerB_Orders_Addresses_OrCart() line 110
  ```

### Mutation 5: Authoritative Server Pricing & Anti-Tampering
- **Target File:** `src/GroceryHub.Modules.Ordering/Application/Services/OrderingService.cs`
- **Mutation:**
  ```csharp
  // Mutated from:
  long lineSubtotal = product.SellingPrice * item.Quantity;
  // To:
  long lineSubtotal = 1000 * item.Quantity;
  ```
- **Observed Test Failure:**
  ```text
  [FAIL] GroceryHub.IntegrationTests.Ordering.PriceTamperingIntegrationTests.ClientAttemptsPriceTampering_InjectedPricesAndTotalsAreIgnored_AuthoritativeCatalogPricesUsed
  Assert.Equal() Failure: Values differ
  Expected: 500000
  Actual:   2000
  ```

### Mutation 6: SePay Webhook HMAC Signature Validation
- **Target File:** `src/GroceryHub.Modules.Payments/Application/SePaySignatureValidator.cs`
- **Mutation:**
  ```csharp
  // Mutated from:
  string mode = _options.Webhook.AuthMode ?? "Hmac";
  // To:
  if (rawBody.Length >= 0) return true;
  ```
- **Observed Test Failure:**
  ```text
  [FAIL] GroceryHub.UnitTests.Payments.SePaySignatureAndUrlTests.T_PAY_04_TamperedRawJsonBody_SignatureValidationFails
  Assert.False() Failure
  Expected: False
  Actual:   True
  ```

### Mutation 7: SePay Webhook Amount Matching & Anomaly Routing
- **Target File:** `src/GroceryHub.Modules.Payments/Application/PaymentService.cs`
- **Mutation:**
  ```csharp
  // Mutated from:
  if (payload.TransferAmount < attempt.Payment.Amount)
  // To:
  if (false && payload.TransferAmount < attempt.Payment.Amount)
  ```
- **Observed Test Failure:**
  ```text
  [FAIL] GroceryHub.IntegrationTests.Payments.SePayPaymentIntegrationTests.T_PAY_08_AmountMismatch_UnderpaidAndOverpaidRecordedForReview
  Assert.Equal() Failure: Strings differ
  Expected: "PendingPayment"
  Actual:   "Confirmed"
  ```

### Mutation 8: Refund Two-Person Rule (BR-15)
- **Target File:** `src/GroceryHub.Modules.Payments/Application/PaymentService.cs`
- **Mutation:**
  ```csharp
  // Mutated from:
  if (refund.RequestedBy == decidedByUserId)
  // To:
  if (false && refund.RequestedBy == decidedByUserId)
  ```
- **Observed Test Failure:**
  ```text
  [FAIL] GroceryHub.IntegrationTests.Payments.SePayPaymentIntegrationTests.T_PAY_18_TwoPersonRule_RefundSelfApprovalBlockedAndSecondPersonSucceeds
  Assert.Equal() Failure: Values differ
  Expected: Conflict
  ```

---

## 3. Conclusion
All 8 mutations were caught immediately by our automated test suite. Zero defects or broken invariants passed undetected. Code has been fully reverted to clean, authoritative state.
