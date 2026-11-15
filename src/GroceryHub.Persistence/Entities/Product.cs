using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public static class ProductStatus
{
    public const string Draft = "Draft";
    public const string Active = "Active";
    public const string Archived = "Archived";

    public static readonly IReadOnlyList<string> All = [Draft, Active, Archived];
}

public class Product : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public Guid? BrandId { get; set; }

    public string Unit { get; set; } = string.Empty;

    public string? PackSize { get; set; }

    public long ListPrice { get; set; }

    public long SellingPrice { get; set; }

    public long CostPrice { get; set; }

    public string Status { get; set; } = ProductStatus.Draft;

    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public string? SpecsJson { get; set; }

    public bool IsFeatured { get; set; }

    public decimal RatingAvg { get; set; }

    public int RatingCount { get; set; }

    public int SoldCount { get; set; }

    public bool ExpiryTracked { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Category? Category { get; set; }

    public Brand? Brand { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public ICollection<ProductPriceHistory> PriceHistories { get; set; } = new List<ProductPriceHistory>();
}
