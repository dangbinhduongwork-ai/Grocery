using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroceryHub.Persistence.Seeding;

public static class PromotionsSeeder
{
    public const string CodeChaoHe30 = "CHAOHE30";
    public const string CodeFreeShip = "FREESHIP";
    public const string CodeGiam50K = "GIAM50K";

    public static async Task SeedPromotionsAsync(
        GroceryHubDbContext db,
        TimeProvider timeProvider,
        ILogger logger,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset startDate = now.AddDays(-30);
        DateTimeOffset endDate = now.AddYears(1);

        List<Coupon> couponsToSeed =
        [
            // 1. CHAOHE30: Percent 30%, MaxDiscount 50.000đ, MinSubtotal 200.000đ
            new Coupon
            {
                Id = Guid.NewGuid(),
                Code = CodeChaoHe30,
                Name = "Chào hè giảm 30%",
                Description = "Giảm 30% tối đa 50.000đ cho đơn hàng từ 200.000đ",
                Type = CouponTypeConstants.Percent,
                Value = 30L,
                MaxDiscount = 50_000L,
                MinSubtotal = 200_000L,
                StartsAt = startDate,
                EndsAt = endDate,
                UsageLimit = 1000,
                PerUserLimit = 1,
                UsedCount = 0,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },

            // 2. FREESHIP: FreeShipping, MinSubtotal 200.000đ
            new Coupon
            {
                Id = Guid.NewGuid(),
                Code = CodeFreeShip,
                Name = "Miễn phí vận chuyển",
                Description = "Miễn phí giao hàng cho đơn hàng từ 200.000đ",
                Type = CouponTypeConstants.FreeShipping,
                Value = 0L,
                MaxDiscount = null,
                MinSubtotal = 200_000L,
                StartsAt = startDate,
                EndsAt = endDate,
                UsageLimit = 5000,
                PerUserLimit = 2,
                UsedCount = 0,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },

            // 3. GIAM50K: FixedAmount 50.000đ, MinSubtotal 300.000đ
            new Coupon
            {
                Id = Guid.NewGuid(),
                Code = CodeGiam50K,
                Name = "Giảm ngay 50.000đ",
                Description = "Giảm 50.000đ cho đơn hàng từ 300.000đ",
                Type = CouponTypeConstants.FixedAmount,
                Value = 50_000L,
                MaxDiscount = null,
                MinSubtotal = 300_000L,
                StartsAt = startDate,
                EndsAt = endDate,
                UsageLimit = 500,
                PerUserLimit = 1,
                UsedCount = 0,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        ];

        HashSet<string> existingCodes = (await db.Coupons
            .Select(c => c.Code)
            .ToListAsync(ct))
            .ToHashSet();

        List<Coupon> newCoupons = [];
        foreach (Coupon coupon in couponsToSeed)
        {
            if (!existingCodes.Contains(coupon.Code))
            {
                newCoupons.Add(coupon);
                logger.LogInformation("Seeding reference coupon {CouponCode} ({CouponType})", coupon.Code, coupon.Type);
            }
        }

        if (newCoupons.Count > 0)
        {
            db.Coupons.AddRange(newCoupons);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Promotions seeding completed: added {Count} coupons.", newCoupons.Count);
        }
    }
}
