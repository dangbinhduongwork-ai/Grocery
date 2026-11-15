using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public static class CouponTypeConstants
{
    public const string Percent = "Percent";
    public const string FixedAmount = "FixedAmount";
    public const string FreeShipping = "FreeShipping";

    public static readonly IReadOnlyList<string> All =
    [
        Percent,
        FixedAmount,
        FreeShipping
    ];
}

public class Coupon : IAuditableEntity
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Type { get; set; } = CouponTypeConstants.Percent;

    public long Value { get; set; }

    public long? MaxDiscount { get; set; }

    public long MinSubtotal { get; set; } = 0;

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public int? UsageLimit { get; set; }

    public int? PerUserLimit { get; set; } = 1;

    public int UsedCount { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<CouponScope> Scopes { get; set; } = new List<CouponScope>();

    public ICollection<CouponRedemption> Redemptions { get; set; } = new List<CouponRedemption>();
}
