using GroceryHub.SharedKernel;
using Xunit;

namespace GroceryHub.UnitTests.Promotions;

public sealed class CouponCalculationTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 11, 15, 12, 0, 0, TimeSpan.FromHours(7));

    private static CouponRuleInput CreateCoupon(
        string type = CouponValidationEngine.TypePercent,
        long value = 10,
        long? maxDiscount = null,
        long minSubtotal = 100_000,
        DateTimeOffset? startsAt = null,
        DateTimeOffset? endsAt = null,
        int? usageLimit = null,
        int? perUserLimit = null,
        int usedCount = 0,
        bool isActive = true,
        IReadOnlyList<CouponScopeRuleDto>? scopes = null)
    {
        return new CouponRuleInput(
            Id: Guid.NewGuid(),
            Code: "TESTCOUPON",
            Type: type,
            Value: value,
            MaxDiscount: maxDiscount,
            MinSubtotal: minSubtotal,
            StartsAt: startsAt ?? BaseTime.AddDays(-1),
            EndsAt: endsAt ?? BaseTime.AddDays(7),
            UsageLimit: usageLimit,
            PerUserLimit: perUserLimit,
            UsedCount: usedCount,
            IsActive: isActive,
            Scopes: scopes ?? []);
    }

    [Fact]
    public void PercentDiscount_WithoutCap_CalculatesCorrectDiscount()
    {
        var coupon = CreateCoupon(type: CouponValidationEngine.TypePercent, value: 10, minSubtotal: 100_000);
        var p1 = Guid.NewGuid();
        var cat = Guid.NewGuid();
        var items = new[]
        {
            new CartItemEvaluationDto(p1, cat, 200_000, 1, 200_000)
        };

        var result = CouponValidationEngine.Evaluate(coupon, Guid.NewGuid(), 200_000, 25_000, items, BaseTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(20_000, result.Value.DiscountAmount);
        Assert.Equal(20_000, result.Value.LineDiscountAllocations[p1]);
    }

    [Fact]
    public void PercentDiscount_WithCap_ClampsToMaxDiscount()
    {
        // 20% on 300,000 = 60,000, but MaxDiscount is 50,000
        var coupon = CreateCoupon(type: CouponValidationEngine.TypePercent, value: 20, maxDiscount: 50_000, minSubtotal: 100_000);
        var p1 = Guid.NewGuid();
        var cat = Guid.NewGuid();
        var items = new[]
        {
            new CartItemEvaluationDto(p1, cat, 300_000, 1, 300_000)
        };

        var result = CouponValidationEngine.Evaluate(coupon, Guid.NewGuid(), 300_000, 25_000, items, BaseTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(50_000, result.Value.DiscountAmount);
        Assert.Equal(50_000, result.Value.LineDiscountAllocations[p1]);
    }

    [Fact]
    public void FixedAmountDiscount_DeductsExactAmount()
    {
        var coupon = CreateCoupon(type: CouponValidationEngine.TypeFixedAmount, value: 40_000, minSubtotal: 100_000);
        var p1 = Guid.NewGuid();
        var cat = Guid.NewGuid();
        var items = new[]
        {
            new CartItemEvaluationDto(p1, cat, 150_000, 1, 150_000)
        };

        var result = CouponValidationEngine.Evaluate(coupon, Guid.NewGuid(), 150_000, 25_000, items, BaseTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(40_000, result.Value.DiscountAmount);
        Assert.Equal(40_000, result.Value.LineDiscountAllocations[p1]);
    }

    [Fact]
    public void FreeShippingDiscount_SetsDiscountToShippingFee_AndZeroLineDiscounts()
    {
        var coupon = CreateCoupon(type: CouponValidationEngine.TypeFreeShipping, value: 0, minSubtotal: 100_000);
        var p1 = Guid.NewGuid();
        var cat = Guid.NewGuid();
        var items = new[]
        {
            new CartItemEvaluationDto(p1, cat, 200_000, 1, 200_000)
        };

        var result = CouponValidationEngine.Evaluate(coupon, Guid.NewGuid(), 200_000, shippingFee: 35_000, items, BaseTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(35_000, result.Value.DiscountAmount);
        Assert.Equal(0, result.Value.LineDiscountAllocations[p1]);
    }

    [Fact]
    public void MinSubtotal_GuardBlocksUnderThreshold()
    {
        var coupon = CreateCoupon(minSubtotal: 250_000);
        var p1 = Guid.NewGuid();
        var cat = Guid.NewGuid();
        var items = new[]
        {
            new CartItemEvaluationDto(p1, cat, 200_000, 1, 200_000)
        };

        var result = CouponValidationEngine.Evaluate(coupon, Guid.NewGuid(), 200_000, 25_000, items, BaseTime);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.CouponMinSubtotal, result.Error.Code);
    }

    [Fact]
    public void DateRange_GuardBlocksBeforeStartOrAfterEnd()
    {
        var expiredCoupon = CreateCoupon(
            startsAt: BaseTime.AddDays(-10),
            endsAt: BaseTime.AddDays(-1));
        var p1 = Guid.NewGuid();
        var cat = Guid.NewGuid();
        var items = new[] { new CartItemEvaluationDto(p1, cat, 200_000, 1, 200_000) };

        var expiredResult = CouponValidationEngine.Evaluate(expiredCoupon, Guid.NewGuid(), 200_000, 25_000, items, BaseTime);
        Assert.False(expiredResult.IsSuccess);
        Assert.Equal(ErrorCodes.CouponExpired, expiredResult.Error.Code);

        var futureCoupon = CreateCoupon(
            startsAt: BaseTime.AddDays(1),
            endsAt: BaseTime.AddDays(5));

        var futureResult = CouponValidationEngine.Evaluate(futureCoupon, Guid.NewGuid(), 200_000, 25_000, items, BaseTime);
        Assert.False(futureResult.IsSuccess);
        Assert.Equal(ErrorCodes.CouponExpired, futureResult.Error.Code);
    }

    [Fact]
    public void UsageLimits_BlocksWhenLimitExceeded()
    {
        var globalMaxCoupon = CreateCoupon(usageLimit: 100, usedCount: 100);
        var p1 = Guid.NewGuid();
        var cat = Guid.NewGuid();
        var items = new[] { new CartItemEvaluationDto(p1, cat, 200_000, 1, 200_000) };

        var globalResult = CouponValidationEngine.Evaluate(globalMaxCoupon, Guid.NewGuid(), 200_000, 25_000, items, BaseTime);
        Assert.False(globalResult.IsSuccess);
        Assert.Equal(ErrorCodes.CouponLimitReached, globalResult.Error.Code);

        var perUserCoupon = CreateCoupon(perUserLimit: 2, usedCount: 5);
        var userResult = CouponValidationEngine.Evaluate(perUserCoupon, Guid.NewGuid(), 200_000, 25_000, items, BaseTime, userExistingRedemptionCount: 2);
        Assert.False(userResult.IsSuccess);
        Assert.Equal(ErrorCodes.CouponLimitReached, userResult.Error.Code);
    }

    [Fact]
    public void ScopedCoupon_OnlyDiscountsEligibleItems()
    {
        var targetCat = Guid.NewGuid();
        var otherCat = Guid.NewGuid();

        var coupon = CreateCoupon(
            type: CouponValidationEngine.TypePercent,
            value: 10,
            minSubtotal: 50_000,
            scopes: [new CouponScopeRuleDto("Category", targetCat)]);

        var pEligible = Guid.NewGuid();
        var pIneligible = Guid.NewGuid();

        var items = new[]
        {
            new CartItemEvaluationDto(pEligible, targetCat, 100_000, 1, 100_000),
            new CartItemEvaluationDto(pIneligible, otherCat, 150_000, 1, 150_000)
        };

        var result = CouponValidationEngine.Evaluate(coupon, Guid.NewGuid(), 250_000, 25_000, items, BaseTime);

        Assert.True(result.IsSuccess);
        // 10% on 100,000 eligible subtotal = 10,000
        Assert.Equal(10_000, result.Value.DiscountAmount);
        Assert.Equal(10_000, result.Value.LineDiscountAllocations[pEligible]);
        Assert.Equal(0, result.Value.LineDiscountAllocations[pIneligible]);
    }

    [Fact]
    public void ProportionalAllocation_BR13_AllocatesRemainderToHighestSubtotalItem()
    {
        // Subtotal = 300,000 (Item 1: 100,000; Item 2: 200,000)
        // Fixed discount: 50,000
        // Line 1: 50,000 * 100,000 / 300,000 = 16,666
        // Line 2: 50,000 * 200,000 / 300,000 = 33,333
        // Sum = 49,999. Remainder 1 must go to Item 2 (highest subtotal) -> 33,334
        var coupon = CreateCoupon(
            type: CouponValidationEngine.TypeFixedAmount,
            value: 50_000,
            minSubtotal: 100_000);

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var cat = Guid.NewGuid();

        var items = new[]
        {
            new CartItemEvaluationDto(p1, cat, 100_000, 1, 100_000),
            new CartItemEvaluationDto(p2, cat, 200_000, 1, 200_000)
        };

        var result = CouponValidationEngine.Evaluate(coupon, Guid.NewGuid(), 300_000, 25_000, items, BaseTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(50_000, result.Value.DiscountAmount);
        Assert.Equal(16_666, result.Value.LineDiscountAllocations[p1]);
        Assert.Equal(33_334, result.Value.LineDiscountAllocations[p2]);
        Assert.Equal(50_000, result.Value.LineDiscountAllocations[p1] + result.Value.LineDiscountAllocations[p2]);
    }
}
