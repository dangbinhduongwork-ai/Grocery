namespace GroceryHub.Modules.Inventory.Contracts;

public interface IInventoryModuleContract
{
    Task<int> GetAvailableStockAsync(Guid productId, CancellationToken ct = default);

    Task<bool> IsInStockAsync(Guid productId, int quantity, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, int>> GetAvailableStocksAsync(IEnumerable<Guid> productIds, CancellationToken ct = default);
}
