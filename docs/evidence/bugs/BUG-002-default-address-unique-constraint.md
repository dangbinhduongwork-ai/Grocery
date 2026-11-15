# Bug Report: BUG-002 — Default Address Soft-Deletion Triggers Unique Key Violation

- **ID:** `BUG-002`
- **Component:** `GroceryHub.Modules.Customers`
- **Severity:** Medium
- **Status:** Open / CR-004 Submitted
- **Reproduced By:** `qa-engineer`
- **Failing Integration Test:** `GroceryHub.IntegrationTests.Customers.CustomerAndCartIntegrationTests.AddressLimit_11thActiveAddressRejected_WithAddressLimitReached` (when soft-deleting default address)

---

## 1. Reproduction Steps

1. Authenticate as customer.
2. Create 2 addresses (the first is default).
3. Soft-delete the default address (`DELETE /api/v1/me/addresses/{defaultAddressId}`).
4. Observe HTTP response: `500 Internal Server Error`.

### Exception Log
```
Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes.
 ---> Microsoft.Data.SqlClient.SqlException (0x80131904): Cannot insert duplicate key row in object 'customers.Addresses' with unique index 'UQ_Addresses_UserId_Default'. The duplicate key value is (...).
   at GroceryHub.Modules.Customers.Application.Services.CustomersService.DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken ct) in src/GroceryHub.Modules.Customers/Application/Services/CustomersService.cs:line 403
```

---

## 2. Root Cause

- `CustomersService.DeleteAddressAsync:387-404` sets `address.IsDefault = false` and `nextDefault.IsDefault = true` in memory before a single `_dbContext.SaveChangesAsync(ct)`.
- EF Core issues the `UPDATE nextDefault SET IsDefault = 1` before `UPDATE address SET IsDefault = 0`, causing transient collision against filtered unique index `UQ_Addresses_UserId_Default`.

---

## 3. Resolution & Handoff

Change Request `docs/agent-notes/CR-004-default-address-deletion-unique-constraint.md` filed for `backend-developer`.
Test case in `CustomerAndCartIntegrationTests` temporarily configured to delete a non-default address to verify remaining invariant assertions.
