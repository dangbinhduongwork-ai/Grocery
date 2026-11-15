namespace GroceryHub.Modules.Purchasing.Contracts;

public interface IPurchasingModuleContract
{
    Task<bool> HasOpenPurchaseOrdersForProductAsync(Guid productId, CancellationToken ct = default);
}
