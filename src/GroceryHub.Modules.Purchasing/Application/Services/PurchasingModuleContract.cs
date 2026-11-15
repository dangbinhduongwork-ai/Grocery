using GroceryHub.Modules.Purchasing.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Purchasing.Application.Services;

public sealed class PurchasingModuleContract : IPurchasingModuleContract
{
    private readonly GroceryHubDbContext _dbContext;

    public PurchasingModuleContract(GroceryHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasOpenPurchaseOrdersForProductAsync(Guid productId, CancellationToken ct = default)
    {
        return await _dbContext.PurchaseOrderItems
            .AsNoTracking()
            .Where(i => i.ProductId == productId)
            .AnyAsync(i =>
                i.PurchaseOrder != null &&
                (i.PurchaseOrder.Status == PurchaseOrderStatus.Draft ||
                 i.PurchaseOrder.Status == PurchaseOrderStatus.Submitted ||
                 i.PurchaseOrder.Status == PurchaseOrderStatus.PartiallyReceived),
                ct);
    }
}
