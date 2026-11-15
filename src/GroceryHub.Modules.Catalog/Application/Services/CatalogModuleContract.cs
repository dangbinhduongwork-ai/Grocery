using GroceryHub.Modules.Catalog.Application.Storage;
using GroceryHub.Modules.Catalog.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Catalog.Application.Services;

public sealed class CatalogModuleContract : ICatalogModuleContract
{
    private readonly GroceryHubDbContext _dbContext;
    private readonly IFileStorage _fileStorage;

    public CatalogModuleContract(GroceryHubDbContext dbContext, IFileStorage fileStorage)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
    }

    public async Task<CatalogProductContractDto?> GetProductByIdAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(static p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null)
        {
            return null;
        }

        string? imgKey = product.Images.FirstOrDefault(static i => i.IsPrimary)?.StorageKey
            ?? product.Images.OrderBy(static i => i.SortOrder).FirstOrDefault()?.StorageKey;

        string? imgUrl = !string.IsNullOrWhiteSpace(imgKey) ? _fileStorage.GetPublicUrl(imgKey) : null;

        return new CatalogProductContractDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Slug,
            product.Unit,
            product.ListPrice,
            product.SellingPrice,
            product.Status,
            imgUrl);
    }

    public async Task<bool> IsProductActiveAsync(Guid productId, CancellationToken ct = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == productId && p.Status == ProductStatus.Active, ct);
    }

    public async Task<IReadOnlyList<CatalogProductContractDto>> GetProductsByIdsAsync(
        IEnumerable<Guid> productIds,
        CancellationToken ct = default)
    {
        var idList = productIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(static p => p.Images)
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(ct);

        return products.Select(p =>
        {
            string? imgKey = p.Images.FirstOrDefault(static i => i.IsPrimary)?.StorageKey
                ?? p.Images.OrderBy(static i => i.SortOrder).FirstOrDefault()?.StorageKey;
            string? imgUrl = !string.IsNullOrWhiteSpace(imgKey) ? _fileStorage.GetPublicUrl(imgKey) : null;

            return new CatalogProductContractDto(
                p.Id,
                p.Sku,
                p.Name,
                p.Slug,
                p.Unit,
                p.ListPrice,
                p.SellingPrice,
                p.Status,
                imgUrl);
        }).ToList();
    }
}
