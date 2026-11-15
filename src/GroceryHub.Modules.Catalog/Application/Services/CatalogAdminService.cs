using GroceryHub.Modules.Catalog.Application.DTOs;
using GroceryHub.Modules.Catalog.Application.Storage;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroceryHub.Modules.Catalog.Application.Services;

public sealed class CatalogAdminService
{
    private readonly GroceryHubDbContext _dbContext;
    private readonly IImageUploadService _imageUploadService;
    private readonly IFileStorage _fileStorage;
    private readonly IAuditLogPort _auditLogPort;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CatalogAdminService> _logger;

    public CatalogAdminService(
        GroceryHubDbContext dbContext,
        IImageUploadService imageUploadService,
        IFileStorage fileStorage,
        IAuditLogPort auditLogPort,
        TimeProvider timeProvider,
        ILogger<CatalogAdminService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _imageUploadService = imageUploadService ?? throw new ArgumentNullException(nameof(imageUploadService));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _auditLogPort = auditLogPort ?? throw new ArgumentNullException(nameof(auditLogPort));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PagedResponse<AdminProductListItemDto>>> GetAdminProductsAsync(
        string? q,
        Guid? categoryId,
        Guid? brandId,
        string? status,
        bool? lowStock,
        int page,
        int pageSize,
        bool canReadCost,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 24 : pageSize, 1, 100);

        var query = _dbContext.Products
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            string normalizedQ = VietnameseTextNormalizer.NormalizeForSearch(q);
            query = query.Where(p => p.SearchText.Contains(normalizedQ) || p.Name.Contains(q) || p.Sku.Contains(q) || (p.Barcode != null && p.Barcode.Contains(q)));
        }

        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (brandId.HasValue && brandId.Value != Guid.Empty)
        {
            query = query.Where(p => p.BrandId == brandId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        var joined = query.GroupJoin(
            _dbContext.StockItems.AsNoTracking(),
            static p => p.Id,
            static s => s.ProductId,
            static (p, stocks) => new ProductWithStock { Product = p, Stock = stocks.FirstOrDefault() });

        if (lowStock == true)
        {
            joined = joined.Where(static x => x.Stock != null && (x.Stock.OnHand - x.Stock.Reserved) <= x.Stock.ReorderLevel);
        }

        int totalCount = await joined.CountAsync(ct);

        var items = await joined
            .OrderByDescending(static x => x.Product.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Product.Id,
                x.Product.Sku,
                x.Product.Barcode,
                x.Product.Name,
                x.Product.Slug,
                CategoryName = x.Product.Category != null ? x.Product.Category.Name : string.Empty,
                BrandName = x.Product.Brand != null ? x.Product.Brand.Name : null,
                x.Product.Unit,
                x.Product.PackSize,
                x.Product.ListPrice,
                x.Product.SellingPrice,
                x.Product.CostPrice,
                x.Product.Status,
                x.Product.IsFeatured,
                x.Product.SoldCount,
                x.Product.CreatedAt,
                RowVersion = Convert.ToBase64String(x.Product.RowVersion),
                OnHand = x.Stock != null ? x.Stock.OnHand : 0,
                Reserved = x.Stock != null ? x.Stock.Reserved : 0,
                ReorderLevel = x.Stock != null ? x.Stock.ReorderLevel : 10
            })
            .ToListAsync(ct);

        var dtos = items.Select(x =>
        {
            int available = Math.Max(0, x.OnHand - x.Reserved);
            bool isLow = available <= x.ReorderLevel;

            return new AdminProductListItemDto(
                x.Id,
                x.Sku,
                x.Barcode,
                x.Name,
                x.Slug,
                x.CategoryName,
                x.BrandName,
                x.Unit,
                x.PackSize,
                x.ListPrice,
                x.SellingPrice,
                canReadCost ? x.CostPrice : null, // BR-01: CostPrice omitted without permission
                x.Status,
                x.IsFeatured,
                isLow,
                x.OnHand,
                x.Reserved,
                available,
                x.SoldCount,
                x.CreatedAt,
                x.RowVersion);
        }).ToList();

        return Result.Success(PagedResponse<AdminProductListItemDto>.Create(
            dtos,
            page,
            pageSize,
            totalCount));
    }

    public async Task<Result<AdminProductDetailDto>> GetAdminProductByIdAsync(
        Guid id,
        bool canReadCost,
        CancellationToken ct = default)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(static p => p.Category)
            .Include(static p => p.Brand)
            .Include(static p => p.Images)
            .Include(static p => p.PriceHistories)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product == null)
        {
            return Result.Failure<AdminProductDetailDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Product with ID '{id}' was not found."));
        }

        var stock = await _dbContext.StockItems
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ProductId == id, ct);

        int onHand = stock?.OnHand ?? 0;
        int reserved = stock?.Reserved ?? 0;
        int reorderLevel = stock?.ReorderLevel ?? 10;
        int available = Math.Max(0, onHand - reserved);

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

        var priceHistories = product.PriceHistories
            .OrderByDescending(static ph => ph.ChangedAt)
            .Select(static ph => new ProductPriceHistoryDto(
                ph.Id,
                ph.OldSellingPrice,
                ph.NewSellingPrice,
                ph.OldListPrice,
                ph.NewListPrice,
                ph.ChangedBy,
                ph.Reason,
                ph.ChangedAt))
            .ToList();

        return Result.Success(new AdminProductDetailDto(
            product.Id,
            product.Sku,
            product.Barcode,
            product.Name,
            product.Slug,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.BrandId,
            product.Brand?.Name,
            product.Unit,
            product.PackSize,
            product.ListPrice,
            product.SellingPrice,
            canReadCost ? product.CostPrice : null, // BR-01
            product.Status,
            product.ShortDescription,
            product.Description,
            product.SpecsJson,
            product.IsFeatured,
            product.ExpiryTracked,
            product.RatingAvg,
            product.RatingCount,
            product.SoldCount,
            onHand,
            reserved,
            available,
            reorderLevel,
            images,
            priceHistories,
            Convert.ToBase64String(product.RowVersion),
            product.CreatedAt,
            product.UpdatedAt));
    }

    public async Task<Result<AdminProductDetailDto>> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Check SKU uniqueness
        bool skuExists = await _dbContext.Products.AnyAsync(p => p.Sku == request.Sku.Trim(), ct);
        if (skuExists)
        {
            return Result.Failure<AdminProductDetailDto>(
                DomainError.Conflict("DUPLICATE_SKU", $"SKU '{request.Sku.Trim()}' already exists."));
        }

        // Slug generation & uniqueness
        string slug = !string.IsNullOrWhiteSpace(request.Slug)
            ? VietnameseTextNormalizer.ToSlug(request.Slug)
            : VietnameseTextNormalizer.ToSlug(request.Name);

        bool slugExists = await _dbContext.Products.AnyAsync(p => p.Slug == slug, ct);
        if (slugExists)
        {
            slug = $"{slug}-{Guid.NewGuid():N}"[..Math.Min(280, slug.Length + 9)];
        }

        // Validate Category
        var category = await _dbContext.Categories.FindAsync([request.CategoryId], ct);
        if (category == null)
        {
            return Result.Failure<AdminProductDetailDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Category '{request.CategoryId}' does not exist."));
        }

        // Validate Brand if provided
        Brand? brand = null;
        if (request.BrandId.HasValue && request.BrandId.Value != Guid.Empty)
        {
            brand = await _dbContext.Brands.FindAsync([request.BrandId.Value], ct);
            if (brand == null)
            {
                return Result.Failure<AdminProductDetailDto>(
                    DomainError.NotFound(ErrorCodes.NotFound, $"Brand '{request.BrandId.Value}' does not exist."));
            }
        }

        // Price validation BR-01
        if (request.SellingPrice > request.ListPrice)
        {
            return Result.Failure<AdminProductDetailDto>(
                DomainError.Validation(ErrorCodes.ValidationFailed, "SellingPrice cannot exceed ListPrice (BR-01)."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        string searchText = VietnameseTextNormalizer.NormalizeForSearch(
            request.Name,
            request.Sku,
            request.Barcode,
            category.Name,
            brand?.Name);

        Product product = new()
        {
            Id = Guid.NewGuid(),
            Sku = request.Sku.Trim(),
            Barcode = request.Barcode?.Trim(),
            Name = request.Name.Trim(),
            Slug = slug,
            SearchText = searchText,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            Unit = request.Unit.Trim(),
            PackSize = request.PackSize?.Trim(),
            ListPrice = request.ListPrice,
            SellingPrice = request.SellingPrice,
            CostPrice = request.InitialCostPrice ?? 0L,
            Status = !string.IsNullOrWhiteSpace(request.Status) ? request.Status : ProductStatus.Draft,
            ShortDescription = request.ShortDescription?.Trim(),
            Description = request.Description?.Trim(),
            SpecsJson = request.SpecsJson?.Trim(),
            IsFeatured = request.IsFeatured ?? false,
            ExpiryTracked = request.ExpiryTracked ?? false,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Products.Add(product);

        // Initialize StockItems row
        StockItem stockItem = new()
        {
            ProductId = product.Id,
            OnHand = 0,
            Reserved = 0,
            ReorderLevel = request.InitialReorderLevel ?? 10,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.StockItems.Add(stockItem);

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Created product {ProductId} ({Sku}: {Name})", product.Id, product.Sku, product.Name);

        return await GetAdminProductByIdAsync(product.Id, canReadCost: true, ct);
    }

    public async Task<Result<AdminProductDetailDto>> UpdateProductAsync(
        Guid id,
        UpdateProductRequest request,
        string? ifMatchRowVersion,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await _dbContext.Products
            .Include(static p => p.Category)
            .Include(static p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product == null)
        {
            return Result.Failure<AdminProductDetailDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Product '{id}' was not found."));
        }

        // Optimistic concurrency check
        if (!string.IsNullOrWhiteSpace(ifMatchRowVersion))
        {
            string currentRowVersion = Convert.ToBase64String(product.RowVersion);
            string expectedRowVersion = ifMatchRowVersion.Trim('"');
            if (!string.Equals(currentRowVersion, expectedRowVersion, StringComparison.Ordinal))
            {
                return Result.Failure<AdminProductDetailDto>(
                    DomainError.Concurrency("Product was modified by another operation."));
            }
        }

        // Check SKU uniqueness if changed
        if (!string.Equals(product.Sku, request.Sku.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            bool skuExists = await _dbContext.Products.AnyAsync(p => p.Sku == request.Sku.Trim() && p.Id != id, ct);
            if (skuExists)
            {
                return Result.Failure<AdminProductDetailDto>(
                    DomainError.Conflict("DUPLICATE_SKU", $"SKU '{request.Sku.Trim()}' already exists."));
            }
            product.Sku = request.Sku.Trim();
        }

        // Check Slug uniqueness if changed
        string normalizedSlug = VietnameseTextNormalizer.ToSlug(request.Slug);
        if (!string.Equals(product.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase))
        {
            bool slugExists = await _dbContext.Products.AnyAsync(p => p.Slug == normalizedSlug && p.Id != id, ct);
            if (slugExists)
            {
                return Result.Failure<AdminProductDetailDto>(
                    DomainError.Conflict("DUPLICATE_SLUG", $"Slug '{normalizedSlug}' already exists."));
            }
            product.Slug = normalizedSlug;
        }

        // Category validation
        var category = await _dbContext.Categories.FindAsync([request.CategoryId], ct);
        if (category == null)
        {
            return Result.Failure<AdminProductDetailDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Category '{request.CategoryId}' does not exist."));
        }

        // Brand validation
        Brand? brand = null;
        if (request.BrandId.HasValue && request.BrandId.Value != Guid.Empty)
        {
            brand = await _dbContext.Brands.FindAsync([request.BrandId.Value], ct);
            if (brand == null)
            {
                return Result.Failure<AdminProductDetailDto>(
                    DomainError.NotFound(ErrorCodes.NotFound, $"Brand '{request.BrandId.Value}' does not exist."));
            }
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        product.Name = request.Name.Trim();
        product.Barcode = request.Barcode?.Trim();
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.Unit = request.Unit.Trim();
        product.PackSize = request.PackSize?.Trim();
        product.ShortDescription = request.ShortDescription?.Trim();
        product.Description = request.Description?.Trim();
        product.SpecsJson = request.SpecsJson?.Trim();
        product.IsFeatured = request.IsFeatured;
        product.ExpiryTracked = request.ExpiryTracked;
        product.SearchText = VietnameseTextNormalizer.NormalizeForSearch(
            product.Name,
            product.Sku,
            product.Barcode,
            category.Name,
            brand?.Name);
        product.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(ct);

        return await GetAdminProductByIdAsync(id, canReadCost: true, ct);
    }

    public async Task<Result<ProductStatusResponse>> UpdateProductStatusAsync(
        Guid id,
        string status,
        CancellationToken ct = default)
    {
        string normalizedStatus = (status ?? string.Empty).Trim();
        if (!ProductStatus.All.Contains(normalizedStatus))
        {
            return Result.Failure<ProductStatusResponse>(
                DomainError.Validation(ErrorCodes.ValidationFailed, $"Invalid product status '{status}'."));
        }

        var product = await _dbContext.Products.FindAsync([id], ct);
        if (product == null)
        {
            return Result.Failure<ProductStatusResponse>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Product '{id}' was not found."));
        }

        product.Status = normalizedStatus;
        product.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new ProductStatusResponse(
            product.Id,
            product.Status,
            Convert.ToBase64String(product.RowVersion)));
    }

    public async Task<Result<ProductPriceResponse>> UpdateProductPriceAsync(
        Guid id,
        UpdateProductPriceRequest request,
        Guid? actorUserId,
        string? actorRole,
        string? ifMatchRowVersion,
        string? ipAddress = null,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Price validation BR-01
        if (request.SellingPrice > request.ListPrice)
        {
            return Result.Failure<ProductPriceResponse>(
                DomainError.Validation(ErrorCodes.ValidationFailed, "SellingPrice cannot exceed ListPrice (BR-01)."));
        }

        if (request.SellingPrice < 0 || request.ListPrice < 0)
        {
            return Result.Failure<ProductPriceResponse>(
                DomainError.Validation(ErrorCodes.ValidationFailed, "Prices cannot be negative."));
        }

        var product = await _dbContext.Products.FindAsync([id], ct);
        if (product == null)
        {
            return Result.Failure<ProductPriceResponse>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Product '{id}' was not found."));
        }

        // Optimistic concurrency check
        if (!string.IsNullOrWhiteSpace(ifMatchRowVersion))
        {
            string currentRowVersion = Convert.ToBase64String(product.RowVersion);
            string expectedRowVersion = ifMatchRowVersion.Trim('"');
            if (!string.Equals(currentRowVersion, expectedRowVersion, StringComparison.Ordinal))
            {
                return Result.Failure<ProductPriceResponse>(
                    DomainError.Concurrency("Product was modified by another operation."));
            }
        }

        long oldSelling = product.SellingPrice;
        long oldList = product.ListPrice;
        DateTimeOffset now = _timeProvider.GetUtcNow();

        product.SellingPrice = request.SellingPrice;
        product.ListPrice = request.ListPrice;
        product.UpdatedAt = now;

        ProductPriceHistory history = new()
        {
            Id = Guid.NewGuid(),
            ProductId = id,
            OldSellingPrice = oldSelling,
            NewSellingPrice = request.SellingPrice,
            OldListPrice = oldList,
            NewListPrice = request.ListPrice,
            ChangedBy = actorUserId,
            Reason = request.Reason.Trim(),
            ChangedAt = now
        };

        _dbContext.ProductPriceHistories.Add(history);
        await _dbContext.SaveChangesAsync(ct);

        // Write AuditLog
        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            ActorUserId: actorUserId,
            ActorRole: actorRole,
            Action: "UpdateProductPrice",
            EntityType: "Product",
            EntityId: id.ToString(),
            BeforeState: new { SellingPrice = oldSelling, ListPrice = oldList },
            AfterState: new { SellingPrice = request.SellingPrice, ListPrice = request.ListPrice, Reason = request.Reason },
            IpAddress: ipAddress,
            CorrelationId: correlationId), ct);

        return Result.Success(new ProductPriceResponse(
            product.Id,
            product.ListPrice,
            product.SellingPrice,
            Convert.ToBase64String(product.RowVersion)));
    }

    public async Task<Result<ProductImageDto>> AddProductImageAsync(
        Guid productId,
        Stream inputStream,
        string contentType,
        long length,
        string? altText,
        bool isPrimary,
        CancellationToken ct = default)
    {
        var product = await _dbContext.Products
            .Include(static p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null)
        {
            return Result.Failure<ProductImageDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Product '{productId}' was not found."));
        }

        Result<SanitizedImageResult> uploadResult = await _imageUploadService.ProcessAndStoreImageAsync(
            inputStream,
            contentType,
            length,
            ct);

        if (!uploadResult.IsSuccess)
        {
            return Result.Failure<ProductImageDto>(uploadResult.Error);
        }

        SanitizedImageResult img = uploadResult.Value;
        DateTimeOffset now = _timeProvider.GetUtcNow();

        if (isPrimary || product.Images.Count == 0)
        {
            foreach (var existing in product.Images)
            {
                existing.IsPrimary = false;
            }
            isPrimary = true;
        }

        int nextSortOrder = product.Images.Count > 0 ? product.Images.Max(static i => i.SortOrder) + 1 : 0;

        ProductImage productImage = new()
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            StorageKey = img.StorageKey,
            AltText = !string.IsNullOrWhiteSpace(altText) ? altText.Trim() : product.Name,
            SortOrder = nextSortOrder,
            IsPrimary = isPrimary,
            Width = img.Width,
            Height = img.Height,
            CreatedAt = now
        };

        _dbContext.ProductImages.Add(productImage);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new ProductImageDto(
            productImage.Id,
            img.Url,
            productImage.AltText,
            productImage.SortOrder,
            productImage.IsPrimary,
            productImage.Width,
            productImage.Height));
    }

    public async Task<Result<IReadOnlyList<ProductImageDto>>> ReorderProductImagesAsync(
        Guid productId,
        ReorderProductImagesRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await _dbContext.Products
            .Include(static p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null)
        {
            return Result.Failure<IReadOnlyList<ProductImageDto>>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Product '{productId}' was not found."));
        }

        for (int i = 0; i < request.ImageIds.Count; i++)
        {
            Guid imgId = request.ImageIds[i];
            var img = product.Images.FirstOrDefault(x => x.Id == imgId);
            if (img != null)
            {
                img.SortOrder = i;
                img.IsPrimary = request.PrimaryImageId.HasValue && request.PrimaryImageId.Value == imgId;
            }
        }

        // If no primary explicitly set, ensure image 0 is primary
        if (product.Images.Count > 0 && !product.Images.Any(static i => i.IsPrimary))
        {
            var first = product.Images.OrderBy(static i => i.SortOrder).First();
            first.IsPrimary = true;
        }

        await _dbContext.SaveChangesAsync(ct);

        var dtos = product.Images
            .OrderBy(static i => i.SortOrder)
            .Select(i => new ProductImageDto(
                i.Id,
                _fileStorage.GetPublicUrl(i.StorageKey),
                i.AltText,
                i.SortOrder,
                i.IsPrimary,
                i.Width,
                i.Height))
            .ToList();

        return Result.Success<IReadOnlyList<ProductImageDto>>(dtos);
    }

    public async Task<Result> DeleteProductImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken ct = default)
    {
        var product = await _dbContext.Products
            .Include(static p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null)
        {
            return Result.Failure(DomainError.NotFound(ErrorCodes.NotFound, $"Product '{productId}' was not found."));
        }

        var image = product.Images.FirstOrDefault(i => i.Id == imageId);
        if (image == null)
        {
            return Result.Failure(DomainError.NotFound(ErrorCodes.NotFound, $"Image '{imageId}' was not found on product '{productId}'."));
        }

        bool wasPrimary = image.IsPrimary;
        _dbContext.ProductImages.Remove(image);

        // Delete from file storage
        await _fileStorage.DeleteAsync(image.StorageKey, ct);

        if (wasPrimary && product.Images.Count > 1)
        {
            var next = product.Images.Where(i => i.Id != imageId).OrderBy(static i => i.SortOrder).FirstOrDefault();
            if (next != null)
            {
                next.IsPrimary = true;
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    // Categories Management
    public async Task<IReadOnlyList<AdminCategoryDto>> GetAdminCategoriesAsync(CancellationToken ct = default)
    {
        var items = await _dbContext.Categories
            .AsNoTracking()
            .Include(static c => c.Parent)
            .OrderBy(static c => c.SortOrder)
            .ThenBy(static c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.ParentId,
                ParentName = c.Parent != null ? c.Parent.Name : null,
                c.Name,
                c.Slug,
                c.Description,
                c.ImageKey,
                c.SortOrder,
                c.IsActive,
                ProductCount = c.Products.Count(),
                c.CreatedAt
            })
            .ToListAsync(ct);

        return items.Select(c => new AdminCategoryDto(
            c.Id,
            c.ParentId,
            c.ParentName,
            c.Name,
            c.Slug,
            c.Description,
            c.ImageKey,
            !string.IsNullOrWhiteSpace(c.ImageKey) ? _fileStorage.GetPublicUrl(c.ImageKey) : null,
            c.SortOrder,
            c.IsActive,
            c.ProductCount,
            c.CreatedAt)).ToList();
    }

    public async Task<Result<AdminCategoryDto>> CreateCategoryAsync(
        CreateCategoryRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string slug = !string.IsNullOrWhiteSpace(request.Slug)
            ? VietnameseTextNormalizer.ToSlug(request.Slug)
            : VietnameseTextNormalizer.ToSlug(request.Name);

        bool slugExists = await _dbContext.Categories.AnyAsync(c => c.Slug == slug, ct);
        if (slugExists)
        {
            return Result.Failure<AdminCategoryDto>(
                DomainError.Conflict("DUPLICATE_CATEGORY_SLUG", $"Category with slug '{slug}' already exists."));
        }

        string? parentName = null;
        if (request.ParentId.HasValue && request.ParentId.Value != Guid.Empty)
        {
            var parent = await _dbContext.Categories.FindAsync([request.ParentId.Value], ct);
            if (parent == null)
            {
                return Result.Failure<AdminCategoryDto>(
                    DomainError.NotFound(ErrorCodes.NotFound, $"Parent category '{request.ParentId.Value}' was not found."));
            }
            parentName = parent.Name;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        Category category = new()
        {
            Id = Guid.NewGuid(),
            ParentId = request.ParentId,
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description?.Trim(),
            ImageKey = request.ImageKey?.Trim(),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new AdminCategoryDto(
            category.Id,
            category.ParentId,
            parentName,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageKey,
            !string.IsNullOrWhiteSpace(category.ImageKey) ? _fileStorage.GetPublicUrl(category.ImageKey) : null,
            category.SortOrder,
            category.IsActive,
            0,
            category.CreatedAt));
    }

    public async Task<Result<AdminCategoryDto>> UpdateCategoryAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ParentId == id)
        {
            return Result.Failure<AdminCategoryDto>(
                DomainError.Validation(ErrorCodes.ValidationFailed, "Category cannot be its own parent."));
        }

        var category = await _dbContext.Categories
            .Include(static c => c.Parent)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category == null)
        {
            return Result.Failure<AdminCategoryDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Category '{id}' was not found."));
        }

        string slug = VietnameseTextNormalizer.ToSlug(request.Slug);
        if (!string.Equals(category.Slug, slug, StringComparison.OrdinalIgnoreCase))
        {
            bool slugExists = await _dbContext.Categories.AnyAsync(c => c.Slug == slug && c.Id != id, ct);
            if (slugExists)
            {
                return Result.Failure<AdminCategoryDto>(
                    DomainError.Conflict("DUPLICATE_CATEGORY_SLUG", $"Category with slug '{slug}' already exists."));
            }
            category.Slug = slug;
        }

        string? parentName = null;
        if (request.ParentId.HasValue && request.ParentId.Value != Guid.Empty)
        {
            var parent = await _dbContext.Categories.FindAsync([request.ParentId.Value], ct);
            if (parent == null)
            {
                return Result.Failure<AdminCategoryDto>(
                    DomainError.NotFound(ErrorCodes.NotFound, $"Parent category '{request.ParentId.Value}' was not found."));
            }
            parentName = parent.Name;
        }

        category.ParentId = request.ParentId;
        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.ImageKey = request.ImageKey?.Trim();
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        category.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(ct);

        int productCount = await _dbContext.Products.CountAsync(p => p.CategoryId == id, ct);

        return Result.Success(new AdminCategoryDto(
            category.Id,
            category.ParentId,
            parentName,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageKey,
            !string.IsNullOrWhiteSpace(category.ImageKey) ? _fileStorage.GetPublicUrl(category.ImageKey) : null,
            category.SortOrder,
            category.IsActive,
            productCount,
            category.CreatedAt));
    }

    public async Task<Result> DeleteCategoryAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _dbContext.Categories.FindAsync([id], ct);
        if (category == null)
        {
            return Result.Failure(DomainError.NotFound(ErrorCodes.NotFound, $"Category '{id}' was not found."));
        }

        bool hasChildren = await _dbContext.Categories.AnyAsync(c => c.ParentId == id, ct);
        if (hasChildren)
        {
            return Result.Failure(DomainError.Conflict(
                ErrorCodes.Conflict,
                "Cannot delete category that contains subcategories."));
        }

        bool hasProducts = await _dbContext.Products.AnyAsync(p => p.CategoryId == id, ct);
        if (hasProducts)
        {
            return Result.Failure(DomainError.Conflict(
                ErrorCodes.Conflict,
                "Cannot delete category that contains products."));
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    // Brands Management
    public async Task<IReadOnlyList<AdminBrandDto>> GetAdminBrandsAsync(CancellationToken ct = default)
    {
        var items = await _dbContext.Brands
            .AsNoTracking()
            .OrderBy(static b => b.Name)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Slug,
                b.LogoKey,
                b.IsActive,
                ProductCount = b.Products.Count(),
                b.CreatedAt
            })
            .ToListAsync(ct);

        return items.Select(b => new AdminBrandDto(
            b.Id,
            b.Name,
            b.Slug,
            b.LogoKey,
            !string.IsNullOrWhiteSpace(b.LogoKey) ? _fileStorage.GetPublicUrl(b.LogoKey) : null,
            b.IsActive,
            b.ProductCount,
            b.CreatedAt)).ToList();
    }

    public async Task<Result<AdminBrandDto>> CreateBrandAsync(
        CreateBrandRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string slug = !string.IsNullOrWhiteSpace(request.Slug)
            ? VietnameseTextNormalizer.ToSlug(request.Slug)
            : VietnameseTextNormalizer.ToSlug(request.Name);

        bool slugExists = await _dbContext.Brands.AnyAsync(b => b.Slug == slug, ct);
        if (slugExists)
        {
            return Result.Failure<AdminBrandDto>(
                DomainError.Conflict("DUPLICATE_BRAND_SLUG", $"Brand with slug '{slug}' already exists."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        Brand brand = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = slug,
            LogoKey = request.LogoKey?.Trim(),
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Brands.Add(brand);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new AdminBrandDto(
            brand.Id,
            brand.Name,
            brand.Slug,
            brand.LogoKey,
            !string.IsNullOrWhiteSpace(brand.LogoKey) ? _fileStorage.GetPublicUrl(brand.LogoKey) : null,
            brand.IsActive,
            0,
            brand.CreatedAt));
    }

    public async Task<Result<AdminBrandDto>> UpdateBrandAsync(
        Guid id,
        UpdateBrandRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var brand = await _dbContext.Brands.FindAsync([id], ct);
        if (brand == null)
        {
            return Result.Failure<AdminBrandDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Brand '{id}' was not found."));
        }

        string slug = VietnameseTextNormalizer.ToSlug(request.Slug);
        if (!string.Equals(brand.Slug, slug, StringComparison.OrdinalIgnoreCase))
        {
            bool slugExists = await _dbContext.Brands.AnyAsync(b => b.Slug == slug && b.Id != id, ct);
            if (slugExists)
            {
                return Result.Failure<AdminBrandDto>(
                    DomainError.Conflict("DUPLICATE_BRAND_SLUG", $"Brand with slug '{slug}' already exists."));
            }
            brand.Slug = slug;
        }

        brand.Name = request.Name.Trim();
        brand.LogoKey = request.LogoKey?.Trim();
        brand.IsActive = request.IsActive;
        brand.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(ct);

        int productCount = await _dbContext.Products.CountAsync(p => p.BrandId == id, ct);

        return Result.Success(new AdminBrandDto(
            brand.Id,
            brand.Name,
            brand.Slug,
            brand.LogoKey,
            !string.IsNullOrWhiteSpace(brand.LogoKey) ? _fileStorage.GetPublicUrl(brand.LogoKey) : null,
            brand.IsActive,
            productCount,
            brand.CreatedAt));
    }

    public async Task<Result> DeleteBrandAsync(Guid id, CancellationToken ct = default)
    {
        var brand = await _dbContext.Brands.FindAsync([id], ct);
        if (brand == null)
        {
            return Result.Failure(DomainError.NotFound(ErrorCodes.NotFound, $"Brand '{id}' was not found."));
        }

        bool hasProducts = await _dbContext.Products.AnyAsync(p => p.BrandId == id, ct);
        if (hasProducts)
        {
            return Result.Failure(DomainError.Conflict(
                ErrorCodes.Conflict,
                "Cannot delete brand that has products associated with it."));
        }

        _dbContext.Brands.Remove(brand);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
