# CR-003: Ambient Transaction Support in StockPersistenceService (Blocking Phase 07 E2E Checkout)

- **CR ID:** `CR-003`
- **Phase:** 07 — Customers, Shipping, Coupons, Cart & Ordering
- **Author:** `qa-engineer`
- **Addressed To:** `database-engineer` / `backend-developer`
- **Status:** Proposed
- **Date:** 2026-11-15
- **Blocking:** Yes (Blocks `T07-03` Checkout Integration Tests: E2E scripted run, concurrency, idempotency, tampering, state machine)
- **Failing Tests:**
  - `GroceryHub.IntegrationTests.Ordering.OrderLifecycleScriptedIntegrationTests.ScriptedLifecycle_FullPurchasingFlow_RecordsAllStepsAndMatchesDomainInvariants`
  - `GroceryHub.IntegrationTests.Ordering.OrderOwnershipIntegrationTests.Idor_CustomerA_CannotViewOrMutate_CustomerB_Orders_Addresses_OrCart`
  - `GroceryHub.IntegrationTests.Ordering.CheckoutConcurrencyIntegrationTests.*`
  - `GroceryHub.IntegrationTests.Ordering.IdempotencyIntegrationTests.*`
  - `GroceryHub.IntegrationTests.Ordering.PriceTamperingIntegrationTests.*`
  - `GroceryHub.IntegrationTests.Ordering.OrderStateMachineIntegrationTests.*`

---

## 1. Issue Description & Reproduction

When an authenticated customer submits `POST /api/v1/checkout/orders` with a valid cart, address, and `Idempotency-Key`, the endpoint returns `500 Internal Server Error`:

```json
{
  "type": "https://groceryhub.vn/errors/INTERNAL_ERROR",
  "title": "Internal server error",
  "status": 500,
  "detail": "An unexpected error occurred while processing your request.",
  "instance": "/api/v1/checkout/orders",
  "code": "INTERNAL_ERROR",
  "traceId": "ce99d309-54f1-4ec4-9ce9-4e6b98ae4fe9"
}
```

The underlying exception thrown inside ASP.NET Core:
```
System.InvalidOperationException: The connection is already in a transaction and cannot participate in another transaction.
   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.EnsureNoTransactions()
   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal.SqlServerExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)
   at GroceryHub.Persistence.Services.StockPersistenceService.ReserveStockAsync(Guid orderId, IReadOnlyList`1 items, DateTimeOffset expiresAt, CancellationToken ct) in src/GroceryHub.Persistence/Services/StockPersistenceService.cs:line 43
   at GroceryHub.Modules.Ordering.Application.Services.OrderingService.PlaceOrderAsync(Guid userId, String idempotencyKey, PlaceOrderRequest request, CancellationToken ct) in src/GroceryHub.Modules.Ordering/Application/Services/OrderingService.cs:line 426
```

---

## 2. Root Cause Analysis

1. **Where:** `src/GroceryHub.Persistence/Services/StockPersistenceService.cs` line 43 (and similarly lines 103, 154, 217).
2. **Why:**
   - In `OrderingService.PlaceOrderAsync` (line 422), checkout requires atomic execution across stock reservation, coupon redemption, sequence number generation, order creation, cart clearance, and idempotency persistence:
     ```csharp
     await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
     ```
   - Line 426 calls `_stockPersistenceService.ReserveStockAsync(...)`.
   - In `StockPersistenceService.ReserveStockAsync` (line 43), it unconditionally attempts to open an inner transaction:
     ```csharp
     await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
     ```
   - EF Core with SQL Server does not support nested transactions on the same DbContext / connection. Calling `BeginTransactionAsync` when `_dbContext.Database.CurrentTransaction != null` throws `InvalidOperationException`.
3. **Since When:** Phase 07 (`OrderingService.PlaceOrderAsync` implementation). Previously `StockPersistenceService` was called only without an ambient transaction.

---

## 3. Proposed Fix

In `src/GroceryHub.Persistence/Services/StockPersistenceService.cs`, support ambient transactions by checking `_dbContext.Database.CurrentTransaction`:

```csharp
public async Task<Result> ReserveStockAsync(
    Guid orderId,
    IReadOnlyList<(Guid ProductId, int Quantity)> items,
    DateTimeOffset expiresAt,
    CancellationToken ct = default)
{
    // ... validation and consolidation ...

    bool ownsTransaction = _dbContext.Database.CurrentTransaction is null;
    IDbContextTransaction? transaction = ownsTransaction
        ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct)
        : null;

    try
    {
        foreach (var item in consolidatedItems)
        {
            int affected = await _dbContext.StockItems
                .Where(s => s.ProductId == item.ProductId && s.OnHand - s.Reserved >= item.Quantity)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(b => b.Reserved, b => b.Reserved + item.Quantity),
                    ct);

            if (affected < 1)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(ct);
                }
                return Result.Failure(DomainError.Conflict(
                    ErrorCodes.InsufficientStock,
                    $"Insufficient stock for product '{item.ProductId}'."));
            }
        }

        // ... add reservations ...
        await _dbContext.StockReservations.AddRangeAsync(reservations, ct);
        await _dbContext.SaveChangesAsync(ct);

        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);
        }

        return Result.Success();
    }
    catch (Exception)
    {
        if (transaction is not null)
        {
            await transaction.RollbackAsync(ct);
        }
        throw;
    }
    finally
    {
        if (transaction is not null)
        {
            await transaction.DisposeAsync();
        }
    }
}
```

Apply the identical ambient transaction check pattern to `ReleaseStockReservationAsync` (line 103), `CommitStockReservationAsync` (line 154), and `SweepExpiredReservationsAsync` (line 217), as well as `PurchasingPersistenceService.ReceivePurchaseOrderAsync` (line 46).
