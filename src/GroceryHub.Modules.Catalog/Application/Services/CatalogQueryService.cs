using GroceryHub.Modules.Catalog.Application.DTOs;
using GroceryHub.Modules.Catalog.Application.Storage;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Catalog.Application.Services;

public sealed class CatalogQueryService
{
    private readonly GroceryHubDbContext _dbContext;
    private readonly IFileStorage _fileStorage;

    public CatalogQueryService(GroceryHubDbContext dbContext, IFileStorage fileStorage)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
    }

    public async Task<IReadOnlyList<CategoryTreeDto>> GetCategoryTreeAsync(CancellationToken ct = default)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .Where(static c => c.IsActive)
            .OrderBy(static c => c.SortOrder)
            .ThenBy(static c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.ParentId,
                c.Name,
                c.Slug,
                c.SortOrder,
                c.ImageKey,
                ActiveProductCount = c.Products.Count(static p => p.Status == ProductStatus.Active)
            })
            .ToListAsync(ct);

        var lookup = categories.ToLookup(static c => c.ParentId);

        List<CategoryTreeDto> BuildTree(Guid? parentId)
        {
            List<CategoryTreeDto> result = [];
            foreach (var item in lookup[parentId])
            {
                string? imgUrl = !string.IsNullOrWhiteSpace(item.ImageKey)
                    ? _fileStorage.GetPublicUrl(item.ImageKey)
                    : null;

                var children = BuildTree(item.Id);
                int totalProducts = item.ActiveProductCount + children.Sum(static ch => ch.ActiveProductCount);

                result.Add(new CategoryTreeDto(
                    item.Id,
                    item.ParentId,
                    item.Name,
                    item.Slug,
                    item.SortOrder,
                    imgUrl,
                    totalProducts,
                    children));
            }
            return result;
        }

        return BuildTree(null);
    }

    public async Task<IReadOnlyList<BrandDto>> GetBrandsAsync(CancellationToken ct = default)
    {
        var brands = await _dbContext.Brands
            .AsNoTracking()
            .Where(static b => b.IsActive)
            .OrderBy(static b => b.Name)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Slug,
                b.LogoKey
            })
            .ToListAsync(ct);

        return brands.Select(b => new BrandDto(
            b.Id,
            b.Name,
            b.Slug,
            !string.IsNullOrWhiteSpace(b.LogoKey) ? _fileStorage.GetPublicUrl(b.LogoKey) : null
        )).ToList();
    }

    public async Task<Result<PagedResponse<StorefrontProductCardDto>>> SearchProductsAsync(
        string? q,
        string? categorySlug,
        string? brandSlug,
        long? minPrice,
        long? maxPrice,
        bool inStockOnly,
        string? sort,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 24 : pageSize, 1, 100);

        var query = _dbContext.Products
            .AsNoTracking()
            .Where(static p => p.Status == ProductStatus.Active);

        if (!string.IsNullOrWhiteSpace(q))
        {
            string normalizedQ = VietnameseTextNormalizer.NormalizeForSearch(q);
            query = query.Where(p => p.SearchText.Contains(normalizedQ) || p.Name.Contains(q) || p.Sku.Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            var category = await _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == categorySlug, ct);

            if (category != null)
            {
                var descendantCategoryIds = await GetDescendantCategoryIdsAsync(category.Id, ct);
                descendantCategoryIds.Add(category.Id);
                query = query.Where(p => descendantCategoryIds.Contains(p.CategoryId));
            }
            else
            {
                return Result.Success(PagedResponse<StorefrontProductCardDto>.Empty(page, pageSize));
            }
        }

        if (!string.IsNullOrWhiteSpace(brandSlug))
        {
            query = query.Where(p => p.Brand != null && p.Brand.Slug == brandSlug);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.SellingPrice >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.SellingPrice <= maxPrice.Value);
        }

        var joined = query.GroupJoin(
            _dbContext.StockItems.AsNoTracking(),
            static p => p.Id,
            static s => s.ProductId,
            static (p, stocks) => new { Product = p, Stock = stocks.FirstOrDefault() });

        if (inStockOnly)
        {
            joined = joined.Where(static x => x.Stock != null && (x.Stock.OnHand - x.Stock.Reserved) > 0);
        }

        // Sorting
        var sorted = (sort?.ToLowerInvariant()) switch
        {
            "price_asc" => joined.OrderBy(static x => x.Product.SellingPrice),
            "price_desc" => joined.OrderByDescending(static x => x.Product.SellingPrice),
            "newest" => joined.OrderByDescending(static x => x.Product.CreatedAt),
            "best_selling" => joined.OrderByDescending(static x => x.Product.SoldCount),
            _ => joined.OrderByDescending(static x => x.Product.IsFeatured).ThenByDescending(static x => x.Product.CreatedAt)
        };

        int totalCount = await sorted.CountAsync(ct);

        var items = await sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Product.Id,
                x.Product.Sku,
                x.Product.Name,
                x.Product.Slug,
                x.Product.Unit,
                x.Product.PackSize,
                x.Product.ListPrice,
                x.Product.SellingPrice,
                PrimaryImageKey = x.Product.Images
                    .Where(static img => img.IsPrimary)
                    .Select(static img => img.StorageKey)
                    .FirstOrDefault() ?? x.Product.Images.OrderBy(static img => img.SortOrder).Select(static img => img.StorageKey).FirstOrDefault(),
                x.Product.RatingAvg,
                x.Product.RatingCount,
                x.Product.SoldCount,
                OnHand = x.Stock != null ? x.Stock.OnHand : 0,
                Reserved = x.Stock != null ? x.Stock.Reserved : 0,
                ReorderLevel = x.Stock != null ? x.Stock.ReorderLevel : 10
            })
            .ToListAsync(ct);

        var dtos = items.Select(x =>
        {
            int available = Math.Max(0, x.OnHand - x.Reserved);
            bool inStock = available > 0;
            bool lowStock = inStock && available <= x.ReorderLevel;
            int discountPercent = x.ListPrice > x.SellingPrice && x.ListPrice > 0
                ? (int)Math.Round((double)(x.ListPrice - x.SellingPrice) * 100 / x.ListPrice)
                : 0;

            string? primaryImageUrl = !string.IsNullOrWhiteSpace(x.PrimaryImageKey)
                ? _fileStorage.GetPublicUrl(x.PrimaryImageKey)
                : null;

            return new StorefrontProductCardDto(
                x.Id,
                x.Sku,
                x.Name,
                x.Slug,
                x.Unit,
                x.PackSize,
                x.ListPrice,
                x.SellingPrice,
                discountPercent,
                primaryImageUrl,
                x.RatingAvg,
                x.RatingCount,
                x.SoldCount,
                inStock,
                lowStock);
        }).ToList();

        return Result.Success(PagedResponse<StorefrontProductCardDto>.Create(
            dtos,
            page,
            pageSize,
            totalCount));
    }

    public async Task<Result<StorefrontProductDetailDto>> GetProductBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(static p => p.Category)
            .Include(static p => p.Brand)
            .Include(static p => p.Images)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == ProductStatus.Active, ct);

        if (product == null)
        {
            return Result.Failure<StorefrontProductDetailDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Product with slug '{slug}' not found."));
        }

        var stock = await _dbContext.StockItems
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ProductId == product.Id, ct);

        int onHand = stock?.OnHand ?? 0;
        int reserved = stock?.Reserved ?? 0;
        int reorderLevel = stock?.ReorderLevel ?? 10;
        int available = Math.Max(0, onHand - reserved);
        bool inStock = available > 0;
        bool lowStock = inStock && available <= reorderLevel;

        int discountPercent = product.ListPrice > product.SellingPrice && product.ListPrice > 0
            ? (int)Math.Round((double)(product.ListPrice - product.SellingPrice) * 100 / product.ListPrice)
            : 0;

        var images = product.Images
            .OrderBy(static img => img.SortOrder)
            .Select(img => new ProductImageDto(
                img.Id,
                _fileStorage.GetPublicUrl(img.StorageKey),
                img.AltText,
                img.SortOrder,
                img.IsPrimary,
                img.Width,
                img.Height))
            .ToList();

        int availableQuantityCap = Math.Min(20, available);

        return Result.Success(new StorefrontProductDetailDto(
            product.Id,
            product.Sku,
            product.Barcode,
            product.Name,
            product.Slug,
            new CategorySummaryDto(product.Category!.Id, product.Category.Name, product.Category.Slug),
            product.Brand != null ? new BrandSummaryDto(product.Brand.Id, product.Brand.Name, product.Brand.Slug) : null,
            product.Unit,
            product.PackSize,
            product.ListPrice,
            product.SellingPrice,
            discountPercent,
            product.ShortDescription,
            product.Description,
            product.SpecsJson,
            images,
            product.RatingAvg,
            product.RatingCount,
            product.SoldCount,
            inStock,
            lowStock,
            availableQuantityCap,
            MaxQuantityPerLine: 20));
    }

    public async Task<IReadOnlyList<StorefrontProductCardDto>> GetRelatedProductsAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null)
        {
            return [];
        }

        var related = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == product.CategoryId && p.Id != productId && p.Status == ProductStatus.Active)
            .OrderByDescending(static p => p.SoldCount)
            .ThenByDescending(static p => p.CreatedAt)
            .Take(8)
            .GroupJoin(
                _dbContext.StockItems.AsNoTracking(),
                static p => p.Id,
                static s => s.ProductId,
                static (p, stocks) => new { Product = p, Stock = stocks.FirstOrDefault() })
            .Select(x => new
            {
                x.Product.Id,
                x.Product.Sku,
                x.Product.Name,
                x.Product.Slug,
                x.Product.Unit,
                x.Product.PackSize,
                x.Product.ListPrice,
                x.Product.SellingPrice,
                PrimaryImageKey = x.Product.Images
                    .Where(static img => img.IsPrimary)
                    .Select(static img => img.StorageKey)
                    .FirstOrDefault() ?? x.Product.Images.OrderBy(static img => img.SortOrder).Select(static img => img.StorageKey).FirstOrDefault(),
                x.Product.RatingAvg,
                x.Product.RatingCount,
                x.Product.SoldCount,
                OnHand = x.Stock != null ? x.Stock.OnHand : 0,
                Reserved = x.Stock != null ? x.Stock.Reserved : 0,
                ReorderLevel = x.Stock != null ? x.Stock.ReorderLevel : 10
            })
            .ToListAsync(ct);

        return related.Select(x =>
        {
            int available = Math.Max(0, x.OnHand - x.Reserved);
            bool inStock = available > 0;
            bool lowStock = inStock && available <= x.ReorderLevel;
            int discountPercent = x.ListPrice > x.SellingPrice && x.ListPrice > 0
                ? (int)Math.Round((double)(x.ListPrice - x.SellingPrice) * 100 / x.ListPrice)
                : 0;

            string? primaryImageUrl = !string.IsNullOrWhiteSpace(x.PrimaryImageKey)
                ? _fileStorage.GetPublicUrl(x.PrimaryImageKey)
                : null;

            return new StorefrontProductCardDto(
                x.Id,
                x.Sku,
                x.Name,
                x.Slug,
                x.Unit,
                x.PackSize,
                x.ListPrice,
                x.SellingPrice,
                discountPercent,
                primaryImageUrl,
                x.RatingAvg,
                x.RatingCount,
                x.SoldCount,
                inStock,
                lowStock);
        }).ToList();
    }

    public async Task<IReadOnlyList<BannerDto>> GetBannersAsync(string? placement = null, CancellationToken ct = default)
    {
        var query = _dbContext.Banners
            .AsNoTracking()
            .Where(static b => b.IsActive);

        if (!string.IsNullOrWhiteSpace(placement))
        {
            query = query.Where(b => b.Placement == placement);
        }

        var banners = await query
            .OrderBy(static b => b.SortOrder)
            .ToListAsync(ct);

        return banners.Select(b => new BannerDto(
            b.Id,
            b.Title,
            b.Subtitle,
            _fileStorage.GetPublicUrl(b.ImageKey),
            b.LinkUrl,
            b.Placement,
            b.SortOrder)).ToList();
    }

    public async Task<HomeStorefrontDto> GetHomeStorefrontAsync(CancellationToken ct = default)
    {
        var banners = await GetBannersAsync(null, ct);
        var categories = await GetCategoryTreeAsync(ct);

        // Featured products
        var featured = await GetProductsForHomeAsync(query => query.Where(static p => p.IsFeatured).OrderByDescending(static p => p.CreatedAt).Take(12), ct);
        var newArrivals = await GetProductsForHomeAsync(query => query.OrderByDescending(static p => p.CreatedAt).Take(12), ct);
        var bestSellers = await GetProductsForHomeAsync(query => query.OrderByDescending(static p => p.SoldCount).Take(12), ct);
        var onSale = await GetProductsForHomeAsync(query => query.Where(static p => p.ListPrice > p.SellingPrice).OrderByDescending(static p => (p.ListPrice - p.SellingPrice)).Take(12), ct);

        return new HomeStorefrontDto(
            banners,
            categories,
            featured,
            newArrivals,
            bestSellers,
            onSale);
    }

    private async Task<List<StorefrontProductCardDto>> GetProductsForHomeAsync(
        Func<IQueryable<Product>, IQueryable<Product>> configure,
        CancellationToken ct)
    {
        var baseQuery = _dbContext.Products
            .AsNoTracking()
            .Where(static p => p.Status == ProductStatus.Active);

        var filtered = configure(baseQuery);

        var items = await filtered.GroupJoin(
            _dbContext.StockItems.AsNoTracking(),
            static p => p.Id,
            static s => s.ProductId,
            static (p, stocks) => new { Product = p, Stock = stocks.FirstOrDefault() })
            .Select(x => new
            {
                x.Product.Id,
                x.Product.Sku,
                x.Product.Name,
                x.Product.Slug,
                x.Product.Unit,
                x.Product.PackSize,
                x.Product.ListPrice,
                x.Product.SellingPrice,
                PrimaryImageKey = x.Product.Images
                    .Where(static img => img.IsPrimary)
                    .Select(static img => img.StorageKey)
                    .FirstOrDefault() ?? x.Product.Images.OrderBy(static img => img.SortOrder).Select(static img => img.StorageKey).FirstOrDefault(),
                x.Product.RatingAvg,
                x.Product.RatingCount,
                x.Product.SoldCount,
                OnHand = x.Stock != null ? x.Stock.OnHand : 0,
                Reserved = x.Stock != null ? x.Stock.Reserved : 0,
                ReorderLevel = x.Stock != null ? x.Stock.ReorderLevel : 10
            })
            .ToListAsync(ct);

        return items.Select(x =>
        {
            int available = Math.Max(0, x.OnHand - x.Reserved);
            bool inStock = available > 0;
            bool lowStock = inStock && available <= x.ReorderLevel;
            int discountPercent = x.ListPrice > x.SellingPrice && x.ListPrice > 0
                ? (int)Math.Round((double)(x.ListPrice - x.SellingPrice) * 100 / x.ListPrice)
                : 0;

            string? primaryImageUrl = !string.IsNullOrWhiteSpace(x.PrimaryImageKey)
                ? _fileStorage.GetPublicUrl(x.PrimaryImageKey)
                : null;

            return new StorefrontProductCardDto(
                x.Id,
                x.Sku,
                x.Name,
                x.Slug,
                x.Unit,
                x.PackSize,
                x.ListPrice,
                x.SellingPrice,
                discountPercent,
                primaryImageUrl,
                x.RatingAvg,
                x.RatingCount,
                x.SoldCount,
                inStock,
                lowStock);
        }).ToList();
    }

    private async Task<List<Guid>> GetDescendantCategoryIdsAsync(Guid categoryId, CancellationToken ct)
    {
        var allCategories = await _dbContext.Categories
            .AsNoTracking()
            .Select(static c => new { c.Id, c.ParentId })
            .ToListAsync(ct);

        List<Guid> results = [];
        Queue<Guid> queue = new();
        queue.Enqueue(categoryId);

        while (queue.Count > 0)
        {
            Guid current = queue.Dequeue();
            foreach (var child in allCategories.Where(c => c.ParentId == current))
            {
                results.Add(child.Id);
                queue.Enqueue(child.Id);
            }
        }

        return results;
    }
}
