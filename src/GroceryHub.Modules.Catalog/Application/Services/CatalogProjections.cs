using GroceryHub.Persistence.Entities;

namespace GroceryHub.Modules.Catalog.Application.Services;

internal sealed class ProductWithStock
{
    public Product Product { get; set; } = null!;
    public StockItem? Stock { get; set; }
}

internal sealed class AdminProductProjection
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? PackSize { get; set; }
    public long ListPrice { get; set; }
    public long SellingPrice { get; set; }
    public long CostPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public int SoldCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public int ReorderLevel { get; set; }
}

internal sealed class StorefrontProductProjection
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? PackSize { get; set; }
    public long ListPrice { get; set; }
    public long SellingPrice { get; set; }
    public string? PrimaryImageKey { get; set; }
    public decimal RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public int SoldCount { get; set; }
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public int ReorderLevel { get; set; }
}

internal sealed class CategoryParentProjection
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
}
