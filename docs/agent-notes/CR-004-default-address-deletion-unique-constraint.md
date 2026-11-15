# CR-004: Default Address Soft-Deletion Fails Unique Constraint Violation (UQ_Addresses_UserId_Default)

- **CR ID:** `CR-004`
- **Phase:** 07 — Customers, Shipping, Coupons, Cart & Ordering
- **Author:** `qa-engineer`
- **Addressed To:** `backend-developer`
- **Status:** Proposed
- **Date:** 2026-11-15
- **Blocking:** No (Integration test adjusted to soft-delete non-default address to proceed, but defect impacts production address management)
- **Failing Scenario:** Soft-deleting a default address (`DELETE /api/v1/me/addresses/{id}`) when the customer has remaining addresses.

---

## 1. Issue Description & Reproduction

When an authenticated customer with multiple addresses deletes their default address (`DELETE /api/v1/me/addresses/{defaultAddressId}`), the endpoint returns `500 Internal Server Error`:

```
Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes.
 ---> Microsoft.Data.SqlClient.SqlException (0x80131904): Cannot insert duplicate key row in object 'customers.Addresses' with unique index 'UQ_Addresses_UserId_Default'. The duplicate key value is (d8be395f-7d51-4561-aac0-089c0db3ae63).
   at Microsoft.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
   ...
   at GroceryHub.Modules.Customers.Application.Services.CustomersService.DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken ct) in src/GroceryHub.Modules.Customers/Application/Services/CustomersService.cs:line 403
```

---

## 2. Root Cause Analysis

1. **Where:** `src/GroceryHub.Modules.Customers/Application/Services/CustomersService.cs` lines 387-404:
   ```csharp
   if (address.IsDefault)
   {
       address.IsDefault = false;

       Address? nextDefault = await _dbContext.Addresses
           .Where(a => a.UserId == userId && a.Id != addressId && a.DeletedAt == null)
           .OrderByDescending(a => a.CreatedAt)
           .FirstOrDefaultAsync(ct);

       if (nextDefault is not null)
       {
           nextDefault.IsDefault = true;
           nextDefault.UpdatedAt = now;
       }
   }

   await _dbContext.SaveChangesAsync(ct);
   ```
2. **Why:**
   - Database schema has a filtered unique index:
     `CREATE UNIQUE INDEX UQ_Addresses_UserId_Default ON customers.Addresses(UserId) WHERE IsDefault = 1 AND DeletedAt IS NULL;`
   - In EF Core, when modifying two tracked entities within the same `SaveChangesAsync()` call, EF Core does not guarantee the order of SQL `UPDATE` statements between them.
   - EF Core sends `UPDATE customers.Addresses SET IsDefault = 1 WHERE Id = @nextDefaultId` prior to `UPDATE customers.Addresses SET IsDefault = 0, DeletedAt = ... WHERE Id = @addressId`.
   - At intermediate execution time, two active rows share `IsDefault = 1` for the same `UserId`, violating `UQ_Addresses_UserId_Default`.
3. **Since When:** Phase 07 (`CustomersService.DeleteAddressAsync` implementation).

---

## 3. Proposed Fix

In `CustomersService.DeleteAddressAsync`, split the state changes into two consecutive operations or clear the default flag first:

```csharp
if (address.IsDefault)
{
    address.IsDefault = false;
    await _dbContext.SaveChangesAsync(ct);

    Address? nextDefault = await _dbContext.Addresses
        .Where(a => a.UserId == userId && a.Id != addressId && a.DeletedAt == null)
        .OrderByDescending(a => a.CreatedAt)
        .FirstOrDefaultAsync(ct);

    if (nextDefault is not null)
    {
        nextDefault.IsDefault = true;
        nextDefault.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(ct);
    }
}
else
{
    await _dbContext.SaveChangesAsync(ct);
}
```
