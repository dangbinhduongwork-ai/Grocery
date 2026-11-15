namespace GroceryHub.Persistence.Entities;

public class CouponRedemption
{
    public Guid Id { get; set; }

    public Guid CouponId { get; set; }

    public Guid UserId { get; set; }

    public Guid OrderId { get; set; }

    public long Amount { get; set; }

    public DateTimeOffset RedeemedAt { get; set; }

    public DateTimeOffset? ReleasedAt { get; set; }

    public Coupon? Coupon { get; set; }

    public User? User { get; set; }
}
