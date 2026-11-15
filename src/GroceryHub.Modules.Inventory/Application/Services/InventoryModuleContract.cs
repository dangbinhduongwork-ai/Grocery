using GroceryHub.Modules.Inventory.Contracts;
using GroceryHub.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Inventory.Application.Services;

public sealed class InventoryModuleContract : IInventoryModuleContract
{
    private readonly GroceryHubDbContext _dbContext;

    public InventoryModuleContract(GroceryHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> GetAvailableStockAsync(Guid productId, CancellationToken ct = default)
    {
        var item = await _dbContext.StockItems
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .Select(s => new { s.OnHand, s.Reserved })
            .FirstOrDefaultAsync(ct);

        if (item is null)
        {
            return 0;
        }

        return Math.Max(0, item.OnHand - item.Reserved);
    }

    public async Task<bool> IsInStockAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            return true;
        }

        int available = await GetAvailableStockAsync(productId, ct);
        return available >= quantity;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetAvailableStocksAsync(
        IEnumerable<Guid> productIds,
        CancellationToken ct = default)
    {
        var idList = productIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var rows = await _dbContext.StockItems
            .AsNoTracking()
            .Where(s => idList.Contains(s.ProductId))
            .Select(s => new { s.ProductId, Available = Math.Max(0, s.OnHand - s.Reserved) })
            .ToListAsync(ct);

        var dict = rows.ToDictionary(x => x.ProductId, x => x.Available);

        foreach (var id in idList)
        {
            dict.TryAdd(id, 0);
        }

        return dict;
    }
}
