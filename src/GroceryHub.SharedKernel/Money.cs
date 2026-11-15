using System.Globalization;

namespace GroceryHub.SharedKernel;

/// <summary>
/// Represents a monetary amount in whole Vietnamese Đồng (VND).
/// Uses <see cref="long"/> internally; floating-point arithmetic is strictly forbidden.
/// </summary>
public readonly record struct Money(long Amount) : IComparable<Money>
{
    private static readonly NumberFormatInfo VndNumberFormat = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalDigits = 0,
        NumberGroupSizes = [3]
    };

    public static Money Zero => new(0L);

    public static Money FromVnd(long amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Monetary amount cannot be negative.");
        }

        return new Money(amount);
    }

    public static Money FromSignedVnd(long amount) => new(amount);

    public Money MultiplyQuantity(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity cannot be negative.");
        }

        return new Money(checked(Amount * quantity));
    }

    public Money CalculateDiscount(int percent, Money? maxDiscount = null)
    {
        if (percent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Discount percent must be between 0 and 100.");
        }

        if (Amount <= 0 || percent == 0)
        {
            return Zero;
        }

        long rawDiscount = checked(Amount * percent) / 100L;
        if (maxDiscount.HasValue && rawDiscount > maxDiscount.Value.Amount)
        {
            rawDiscount = maxDiscount.Value.Amount;
        }

        if (rawDiscount > Amount)
        {
            rawDiscount = Amount;
        }

        return new Money(rawDiscount);
    }

    public Money[] Allocate(IReadOnlyList<int> ratios)
    {
        ArgumentNullException.ThrowIfNull(ratios);
        if (ratios.Count == 0)
        {
            throw new ArgumentException("At least one allocation ratio is required.", nameof(ratios));
        }

        long[] longRatios = new long[ratios.Count];
        for (int i = 0; i < ratios.Count; i++)
        {
            if (ratios[i] < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ratios), "Allocation ratio cannot be negative.");
            }

            longRatios[i] = ratios[i];
        }

        return AllocateByWeights(longRatios);
    }

    public Money[] AllocateByWeights(IReadOnlyList<long> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Count == 0)
        {
            throw new ArgumentException("At least one weight is required.", nameof(weights));
        }

        long totalWeight = 0;
        int lastPositiveIndex = -1;
        for (int i = 0; i < weights.Count; i++)
        {
            if (weights[i] < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weights), "Weight cannot be negative.");
            }

            totalWeight = checked(totalWeight + weights[i]);
            if (weights[i] > 0)
            {
                lastPositiveIndex = i;
            }
        }

        Money[] results = new Money[weights.Count];
        if (Amount == 0 || totalWeight == 0)
        {
            return results;
        }

        long allocatedSum = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            long share = (long)((Int128)Amount * weights[i] / totalWeight);
            results[i] = new Money(share);
            allocatedSum = checked(allocatedSum + share);
        }

        long remainder = Amount - allocatedSum;
        if (remainder != 0 && lastPositiveIndex >= 0)
        {
            results[lastPositiveIndex] = new Money(checked(results[lastPositiveIndex].Amount + remainder));
        }

        return results;
    }

    public static Money ComputeMovingAverageCost(int oldOnHand, Money oldCostPrice, int receivedQuantity, Money receiptUnitCost)
    {
        if (receivedQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(receivedQuantity), receivedQuantity, "Received quantity must be positive.");
        }

        long effectiveOldOnHand = Math.Max(0, oldOnHand);
        Int128 totalCost = ((Int128)effectiveOldOnHand * oldCostPrice.Amount) + ((Int128)receivedQuantity * receiptUnitCost.Amount);
        Int128 totalQty = effectiveOldOnHand + receivedQuantity;
        Int128 rounded = (totalCost + (totalQty / 2)) / totalQty;
        return FromVnd((long)rounded);
    }

    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

    public string FormatVnd() => $"{Amount.ToString("N0", VndNumberFormat)} \u20ab";

    public override string ToString() => FormatVnd();

    public static Money operator +(Money left, Money right) => new(checked(left.Amount + right.Amount));

    public static Money operator -(Money left, Money right) => new(checked(left.Amount - right.Amount));

    public static Money operator *(Money money, int multiplier) => new(checked(money.Amount * multiplier));

    public static Money operator *(int multiplier, Money money) => new(checked(money.Amount * multiplier));

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;
}
