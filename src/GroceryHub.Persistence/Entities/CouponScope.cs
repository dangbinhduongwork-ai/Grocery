namespace GroceryHub.Persistence.Entities;

public static class CouponScopeTypes
{
    public const string Category = "Category";
    public const string Product = "Product";

    public static readonly IReadOnlyList<string> All =
    [
        Category,
        Product
    ];
}

public class CouponScope
{
    public Guid CouponId { get; set; }

    public string ScopeType { get; set; } = string.Empty;

    public Guid ScopeId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Coupon? Coupon { get; set; }
}
