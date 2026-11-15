namespace GroceryHub.SharedKernel;

public sealed record CartItemEvaluationDto(
    Guid ProductId,
    Guid CategoryId,
    long UnitPrice,
    int Quantity,
    long LineSubtotal);

public sealed record CouponScopeRuleDto(
    string ScopeType,
    Guid ScopeId);

public sealed record CouponRuleInput(
    Guid Id,
    string Code,
    string Type,
    long Value,
    long? MaxDiscount,
    long MinSubtotal,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? UsageLimit,
    int? PerUserLimit,
    int UsedCount,
    bool IsActive,
    IReadOnlyList<CouponScopeRuleDto> Scopes);

public sealed record CouponEvaluationResult(
    Guid CouponId,
    string Code,
    string Type,
    long DiscountAmount,
    IReadOnlyDictionary<Guid, long> LineDiscountAllocations);

public static class CouponValidationEngine
{
    public const string TypePercent = "Percent";
    public const string TypeFixedAmount = "FixedAmount";
    public const string TypeFreeShipping = "FreeShipping";

    public static Result<CouponEvaluationResult> Evaluate(
        CouponRuleInput coupon,
        Guid? userId,
        long subtotal,
        long shippingFee,
        IReadOnlyList<CartItemEvaluationDto> items,
        DateTimeOffset now,
        int userExistingRedemptionCount = 0)
    {
        ArgumentNullException.ThrowIfNull(coupon);
        ArgumentNullException.ThrowIfNull(items);

        if (!coupon.IsActive)
        {
            return Result.Failure<CouponEvaluationResult>(DomainError.Validation(
                ErrorCodes.CouponInvalid,
                $"Coupon '{coupon.Code}' is not active."));
        }

        if (now < coupon.StartsAt || now > coupon.EndsAt)
        {
            return Result.Failure<CouponEvaluationResult>(DomainError.Validation(
                ErrorCodes.CouponExpired,
                $"Coupon '{coupon.Code}' is expired or not yet active."));
        }

        if (subtotal < coupon.MinSubtotal)
        {
            return Result.Failure<CouponEvaluationResult>(DomainError.Validation(
                ErrorCodes.CouponMinSubtotal,
                $"Order subtotal ({subtotal:N0} ₫) does not meet minimum subtotal requirement ({coupon.MinSubtotal:N0} ₫)."));
        }

        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
        {
            return Result.Failure<CouponEvaluationResult>(DomainError.Conflict(
                ErrorCodes.CouponLimitReached,
                $"Coupon '{coupon.Code}' usage limit has been reached."));
        }

        if (coupon.PerUserLimit.HasValue && userId.HasValue && userExistingRedemptionCount >= coupon.PerUserLimit.Value)
        {
            return Result.Failure<CouponEvaluationResult>(DomainError.Conflict(
                ErrorCodes.CouponLimitReached,
                $"You have already used coupon '{coupon.Code}' the maximum number of times ({coupon.PerUserLimit.Value})."));
        }

        // Determine eligible items based on scopes
        IReadOnlyList<CartItemEvaluationDto> eligibleItems;
        if (coupon.Scopes != null && coupon.Scopes.Count > 0)
        {
            HashSet<Guid> productScopes = coupon.Scopes
                .Where(s => string.Equals(s.ScopeType, "Product", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.ScopeId)
                .ToHashSet();

            HashSet<Guid> categoryScopes = coupon.Scopes
                .Where(s => string.Equals(s.ScopeType, "Category", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.ScopeId)
                .ToHashSet();

            eligibleItems = items.Where(i =>
                productScopes.Contains(i.ProductId) || categoryScopes.Contains(i.CategoryId)).ToList();

            if (eligibleItems.Count == 0)
            {
                return Result.Failure<CouponEvaluationResult>(DomainError.Validation(
                    ErrorCodes.CouponInvalid,
                    $"No items in your cart qualify for coupon '{coupon.Code}'."));
            }
        }
        else
        {
            eligibleItems = items;
        }

        long eligibleSubtotal = eligibleItems.Sum(static i => i.LineSubtotal);
        if (eligibleSubtotal <= 0)
        {
            return Result.Failure<CouponEvaluationResult>(DomainError.Validation(
                ErrorCodes.CouponInvalid,
                "Eligible items subtotal must be greater than zero."));
        }

        long totalDiscount = 0;
        Dictionary<Guid, long> allocations = new();

        foreach (var item in items)
        {
            allocations[item.ProductId] = 0;
        }

        if (string.Equals(coupon.Type, TypeFreeShipping, StringComparison.OrdinalIgnoreCase))
        {
            // Per DEC-011 / Q-009: FreeShipping sets CouponDiscount = ShippingFee and item DiscountAllocated = 0
            totalDiscount = shippingFee;
        }
        else if (string.Equals(coupon.Type, TypePercent, StringComparison.OrdinalIgnoreCase))
        {
            long percentDiscount = (eligibleSubtotal * coupon.Value) / 100;
            if (coupon.MaxDiscount.HasValue && percentDiscount > coupon.MaxDiscount.Value)
            {
                percentDiscount = coupon.MaxDiscount.Value;
            }

            totalDiscount = Math.Min(percentDiscount, eligibleSubtotal);
            AllocateProportionally(eligibleItems, eligibleSubtotal, totalDiscount, allocations);
        }
        else if (string.Equals(coupon.Type, TypeFixedAmount, StringComparison.OrdinalIgnoreCase))
        {
            totalDiscount = Math.Min(coupon.Value, eligibleSubtotal);
            AllocateProportionally(eligibleItems, eligibleSubtotal, totalDiscount, allocations);
        }
        else
        {
            return Result.Failure<CouponEvaluationResult>(DomainError.Validation(
                ErrorCodes.CouponInvalid,
                $"Unknown coupon type '{coupon.Type}'."));
        }

        return Result.Success(new CouponEvaluationResult(
            coupon.Id,
            coupon.Code,
            coupon.Type,
            totalDiscount,
            allocations));
    }

    private static void AllocateProportionally(
        IReadOnlyList<CartItemEvaluationDto> eligibleItems,
        long eligibleSubtotal,
        long totalDiscount,
        Dictionary<Guid, long> allocations)
    {
        if (eligibleItems.Count == 0 || eligibleSubtotal <= 0 || totalDiscount <= 0)
        {
            return;
        }

        long allocatedSum = 0;
        CartItemEvaluationDto? maxSubtotalItem = null;
        long maxSubtotal = -1;

        foreach (var item in eligibleItems)
        {
            long lineDiscount = (totalDiscount * item.LineSubtotal) / eligibleSubtotal;
            allocations[item.ProductId] = lineDiscount;
            allocatedSum += lineDiscount;

            if (item.LineSubtotal > maxSubtotal)
            {
                maxSubtotal = item.LineSubtotal;
                maxSubtotalItem = item;
            }
        }

        // Remainder adjustment to item with max subtotal (BR-13)
        long remainder = totalDiscount - allocatedSum;
        if (remainder != 0 && maxSubtotalItem != null)
        {
            allocations[maxSubtotalItem.ProductId] += remainder;
        }
    }
}
