# Bug Report: BUG-001 — Nested Transactions on PlaceOrderAsync in Ordering & StockPersistenceService

- **ID:** `BUG-001`
- **Component:** `GroceryHub.Modules.Ordering` & `GroceryHub.Persistence`
- **Severity:** High (Blocker for checkout)
- **Status:** Open / CR-003 Submitted
- **Reproduced By:** `qa-engineer`
- **Failing Integration Test:** `GroceryHub.IntegrationTests.Ordering.OrderLifecycleScriptedIntegrationTests.ScriptedLifecycle_FullPurchasingFlow_RecordsAllStepsAndMatchesDomainInvariants`

---

## 1. Reproduction Steps

1. Start API and LocalDB / SQL Server container.
2. Authenticate as customer (`POST /api/v1/auth/login`).
3. Add item to cart (`POST /api/v1/cart/items`).
4. Create customer delivery address (`POST /api/v1/me/addresses`).
5. Execute checkout order placement (`POST /api/v1/checkout/orders` with `Idempotency-Key` header).
6. Observe HTTP response: `500 Internal Server Error`.

### Raw Error Response
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

### Exception Log
```
System.InvalidOperationException: The connection is already in a transaction and cannot participate in another transaction.
   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.EnsureNoTransactions()
   at Microsoft.EntityFrameworkCore.Storage.RelationalConnection.BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal.SqlServerExecutionStrategy.ExecuteAsync[TState,TResult](TState state, Func`4 operation, Func`4 verifySucceeded, CancellationToken cancellationToken)
   at GroceryHub.Persistence.Services.StockPersistenceService.ReserveStockAsync(Guid orderId, IReadOnlyList`1 items, DateTimeOffset expiresAt, CancellationToken ct) in src/GroceryHub.Persistence/Services/StockPersistenceService.cs:line 43
   at GroceryHub.Modules.Ordering.Application.Services.OrderingService.PlaceOrderAsync(Guid userId, String idempotencyKey, PlaceOrderRequest request, CancellationToken ct) in src/GroceryHub.Modules.Ordering/Application/Services/OrderingService.cs:line 426
```

---

## 2. Root Cause

- `OrderingService.PlaceOrderAsync:422` begins an outer transaction via `_dbContext.Database.BeginTransactionAsync(ct)`.
- `StockPersistenceService.ReserveStockAsync:43` unconditionally calls `_dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct)`.
- SQL Server EF Core provider does not allow nested transactions on the same connection.

---

## 3. Resolution & Handoff

Change Request `docs/agent-notes/CR-003-nested-transaction-stock-reservation.md` filed for `database-engineer` / `backend-developer`.
