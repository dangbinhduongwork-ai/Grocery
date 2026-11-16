namespace GroceryHub.Modules.Catalog.Application.DTOs;

public record CategoryTreeDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    int SortOrder,
    string? ImageUrl,
    int ActiveProductCount,
    IReadOnlyList<CategoryTreeDto> Children);

public record BrandDto(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl);

public record StorefrontProductCardDto(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    string Unit,
    string? PackSize,
    long ListPrice,
    long SellingPrice,
    int DiscountPercent,
    string? PrimaryImageUrl,
    decimal RatingAvg,
    int RatingCount,
    int SoldCount,
    bool InStock,
    bool LowStock);

public record CategorySummaryDto(
    Guid Id,
    string Name,
    string Slug);

public record BrandSummaryDto(
    Guid Id,
    string Name,
    string Slug);

public record ProductImageDto(
    Guid Id,
    string Url,
    string Alt,
    int SortOrder,
    bool IsPrimary,
    int Width,
    int Height);

public record StorefrontProductDetailDto(
    Guid Id,
    string Sku,
    string? Barcode,
    string Name,
    string Slug,
    CategorySummaryDto Category,
    BrandSummaryDto? Brand,
    string Unit,
    string? PackSize,
    long ListPrice,
    long SellingPrice,
    int DiscountPercent,
    string? ShortDescription,
    string? Description,
    object? Specs,
    IReadOnlyList<ProductImageDto> Images,
    decimal RatingAvg,
    int RatingCount,
    int SoldCount,
    bool InStock,
    bool LowStock,
    int AvailableQuantityCap,
    int MaxQuantityPerLine);

public record BannerDto(
    Guid Id,
    string Title,
    string? Subtitle,
    string ImageUrl,
    string? LinkUrl,
    string Placement,
    int SortOrder);

public record AdminBannerDto(
    Guid Id,
    string Title,
    string? Subtitle,
    string ImageKey,
    string ImageUrl,
    string? LinkUrl,
    string Placement,
    int SortOrder,
    bool IsActive,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record CreateBannerRequest(
    string Title,
    string? Subtitle,
    string ImageKey,
    string? LinkUrl,
    string? Placement,
    int SortOrder,
    bool IsActive,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt);

public record UpdateBannerRequest(
    string Title,
    string? Subtitle,
    string ImageKey,
    string? LinkUrl,
    string? Placement,
    int SortOrder,
    bool IsActive,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt);

public record BannerImageUploadResponse(
    string ImageKey,
    string ImageUrl);

public record UiDesignSettingsDto(
    IReadOnlyList<string> Announcements,
    string? HeroBadge,
    string? HeroTitle,
    string? HeroSubtitle);

public record HomeStorefrontDto(
    IReadOnlyList<BannerDto> Banners,
    IReadOnlyList<CategoryTreeDto> Categories,
    IReadOnlyList<StorefrontProductCardDto> FeaturedProducts,
    IReadOnlyList<StorefrontProductCardDto> NewArrivals,
    IReadOnlyList<StorefrontProductCardDto> BestSellers,
    IReadOnlyList<StorefrontProductCardDto> OnSaleProducts);

public record AdminProductListItemDto(
    Guid Id,
    string Sku,
    string? Barcode,
    string Name,
    string Slug,
    string CategoryName,
    string? BrandName,
    string Unit,
    string? PackSize,
    long ListPrice,
    long SellingPrice,
    long? CostPrice,
    string Status,
    bool IsFeatured,
    bool LowStock,
    int OnHand,
    int Reserved,
    int Available,
    int SoldCount,
    DateTimeOffset CreatedAt,
    string RowVersion,
    string? PrimaryImageUrl = null);

public record ProductPriceHistoryDto(
    Guid Id,
    long OldSellingPrice,
    long NewSellingPrice,
    long OldListPrice,
    long NewListPrice,
    Guid? ChangedBy,
    string? Reason,
    DateTimeOffset ChangedAt);

public record AdminProductDetailDto(
    Guid Id,
    string Sku,
    string? Barcode,
    string Name,
    string Slug,
    Guid CategoryId,
    string CategoryName,
    Guid? BrandId,
    string? BrandName,
    string Unit,
    string? PackSize,
    long ListPrice,
    long SellingPrice,
    long? CostPrice,
    string Status,
    string? ShortDescription,
    string? Description,
    string? SpecsJson,
    bool IsFeatured,
    bool ExpiryTracked,
    decimal RatingAvg,
    int RatingCount,
    int SoldCount,
    int OnHand,
    int Reserved,
    int Available,
    int ReorderLevel,
    IReadOnlyList<ProductImageDto> Images,
    IReadOnlyList<ProductPriceHistoryDto> PriceHistory,
    string RowVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record CreateProductRequest(
    string Sku,
    string? Barcode,
    string Name,
    string? Slug,
    Guid CategoryId,
    Guid? BrandId,
    string Unit,
    string? PackSize,
    long ListPrice,
    long SellingPrice,
    long? InitialCostPrice,
    string? Status,
    string? ShortDescription,
    string? Description,
    string? SpecsJson,
    bool? IsFeatured,
    bool? ExpiryTracked,
    int? InitialReorderLevel);

public record UpdateProductRequest(
    string Sku,
    string? Barcode,
    string Name,
    string Slug,
    Guid CategoryId,
    Guid? BrandId,
    string Unit,
    string? PackSize,
    string? ShortDescription,
    string? Description,
    string? SpecsJson,
    bool IsFeatured,
    bool ExpiryTracked);

public record UpdateProductStatusRequest(
    string Status);

public record UpdateProductPriceRequest(
    long ListPrice,
    long SellingPrice,
    string Reason);

public record ReorderProductImagesRequest(
    IReadOnlyList<Guid> ImageIds,
    Guid? PrimaryImageId);

public record AdminCategoryDto(
    Guid Id,
    Guid? ParentId,
    string? ParentName,
    string Name,
    string Slug,
    string? Description,
    string? ImageKey,
    string? ImageUrl,
    int SortOrder,
    bool IsActive,
    int ProductCount,
    DateTimeOffset CreatedAt);

public record CreateCategoryRequest(
    Guid? ParentId,
    string Name,
    string? Slug,
    string? Description,
    string? ImageKey,
    int SortOrder,
    bool IsActive);

public record UpdateCategoryRequest(
    Guid? ParentId,
    string Name,
    string Slug,
    string? Description,
    string? ImageKey,
    int SortOrder,
    bool IsActive);

public record AdminBrandDto(
    Guid Id,
    string Name,
    string Slug,
    string? LogoKey,
    string? LogoUrl,
    bool IsActive,
    int ProductCount,
    DateTimeOffset CreatedAt);

public record CreateBrandRequest(
    string Name,
    string? Slug,
    string? LogoKey,
    bool IsActive);

public record UpdateBrandRequest(
    string Name,
    string Slug,
    string? LogoKey,
    bool IsActive);

public record ProductStatusResponse(
    Guid Id,
    string Status,
    string RowVersion);

public record ProductPriceResponse(
    Guid Id,
    long ListPrice,
    long SellingPrice,
    string RowVersion);
