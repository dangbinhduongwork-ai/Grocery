using GroceryHub.SharedKernel;
using Xunit;

namespace GroceryHub.UnitTests;

public sealed class MoneyTests
{
    [Fact]
    public void Zero_HasZeroAmountAndFormatsAsVnd()
    {
        Money zero = Money.Zero;

        Assert.Equal(0L, zero.Amount);
        Assert.Equal("0 \u20ab", zero.FormatVnd());
        Assert.Equal("0 \u20ab", zero.ToString());
    }

    [Theory]
    [InlineData(0L, "0 \u20ab")]
    [InlineData(500L, "500 \u20ab")]
    [InlineData(185_000L, "185.000 \u20ab")]
    [InlineData(1_250_500L, "1.250.500 \u20ab")]
    public void FromVnd_WithNonNegativeAmount_CreatesMoneyAndFormatsCorrectly(long amount, string expectedFormatted)
    {
        Money money = Money.FromVnd(amount);

        Assert.Equal(amount, money.Amount);
        Assert.Equal(expectedFormatted, money.FormatVnd());
        Assert.Equal(expectedFormatted, money.ToString());
    }

    [Fact]
    public void FromVnd_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => Money.FromVnd(-1L));

        Assert.Equal("amount", ex.ParamName);
    }

    [Theory]
    [InlineData(-185_000L, "-185.000 \u20ab")]
    [InlineData(0L, "0 \u20ab")]
    [InlineData(185_000L, "185.000 \u20ab")]
    public void FromSignedVnd_AllowsNegativeZeroAndPositiveAmounts(long amount, string expectedFormatted)
    {
        Money money = Money.FromSignedVnd(amount);

        Assert.Equal(amount, money.Amount);
        Assert.Equal(expectedFormatted, money.FormatVnd());
    }

    [Fact]
    public void ArithmeticOperators_AddSubtractMultiply_ComputeExactVnd()
    {
        Money unitPrice = Money.FromVnd(45_000L);
        Money shipping = Money.FromVnd(25_000L);
        Money discount = Money.FromVnd(10_000L);

        Money lineTotal = unitPrice * 3;
        Money lineTotalCommutative = 3 * unitPrice;
        Money lineTotalViaMethod = unitPrice.MultiplyQuantity(3);
        Money grandTotal = lineTotal + shipping - discount;

        Assert.Equal(135_000L, lineTotal.Amount);
        Assert.Equal(135_000L, lineTotalCommutative.Amount);
        Assert.Equal(135_000L, lineTotalViaMethod.Amount);
        Assert.Equal(150_000L, grandTotal.Amount);
    }

    [Fact]
    public void MultiplyQuantity_WithNegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        Money price = Money.FromVnd(10_000L);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => price.MultiplyQuantity(-1));

        Assert.Equal("quantity", ex.ParamName);
    }

    [Fact]
    public void ComparisonOperators_CompareByVndAmount()
    {
        Money smaller = Money.FromVnd(50_000L);
        Money equal = Money.FromVnd(50_000L);
        Money larger = Money.FromVnd(120_000L);

        Assert.True(smaller < larger);
        Assert.True(smaller <= larger);
        Assert.True(smaller <= equal);
        Assert.True(larger > smaller);
        Assert.True(larger >= smaller);
        Assert.True(equal >= smaller);
        Assert.False(smaller > larger);
        Assert.False(larger < smaller);
        Assert.Equal(0, smaller.CompareTo(equal));
        Assert.True(smaller.CompareTo(larger) < 0);
        Assert.True(larger.CompareTo(smaller) > 0);
    }

    [Fact]
    public void CalculateDiscount_WithoutMaxDiscount_ComputesIntegerFloorPercentage()
    {
        Money subtotal = Money.FromVnd(185_000L);

        Money discount = subtotal.CalculateDiscount(15);

        Assert.Equal(27_750L, discount.Amount);
        Assert.Equal(Money.Zero, subtotal.CalculateDiscount(0));
        Assert.Equal(Money.Zero, Money.Zero.CalculateDiscount(50));
    }

    [Fact]
    public void CalculateDiscount_WithMaxDiscount_CapsAtMaxDiscountAndAmount()
    {
        Money subtotal = Money.FromVnd(500_000L);
        Money maxDiscount = Money.FromVnd(40_000L);

        Money cappedByMax = subtotal.CalculateDiscount(20, maxDiscount);
        Money uncappedWhenBelowMax = subtotal.CalculateDiscount(5, maxDiscount);
        Money cappedBySubtotal = subtotal.CalculateDiscount(100, Money.FromVnd(900_000L));

        Assert.Equal(40_000L, cappedByMax.Amount);
        Assert.Equal(25_000L, uncappedWhenBelowMax.Amount);
        Assert.Equal(500_000L, cappedBySubtotal.Amount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void CalculateDiscount_WithOutOfRangePercent_ThrowsArgumentOutOfRangeException(int invalidPercent)
    {
        Money subtotal = Money.FromVnd(100_000L);

        Assert.Throws<ArgumentOutOfRangeException>(() => subtotal.CalculateDiscount(invalidPercent));
    }

    [Fact]
    public void Allocate_DistributesRemainderDeterministicAndPreservesExactTotal()
    {
        Money total = Money.FromVnd(100_000L);

        Money[] shares = total.Allocate([1, 1, 1]);

        Assert.Equal(3, shares.Length);
        Assert.Equal(total.Amount, shares.Sum(s => s.Amount));
        Assert.Equal(33_333L, shares[0].Amount);
        Assert.Equal(33_333L, shares[1].Amount);
        Assert.Equal(33_334L, shares[2].Amount);
    }

    [Fact]
    public void AllocateByWeights_DistributesProRataAndPreservesExactSumOnLastPositiveItem()
    {
        Money discountPool = Money.FromVnd(50_000L);
        long[] lineWeights = [185_000L, 185_000L, 185_000L, 0L];

        Money[] allocated = discountPool.AllocateByWeights(lineWeights);

        Assert.Equal(4, allocated.Length);
        Assert.Equal(discountPool.Amount, allocated.Sum(m => m.Amount));
        Assert.Equal(16_666L, allocated[0].Amount);
        Assert.Equal(16_666L, allocated[1].Amount);
        Assert.Equal(16_668L, allocated[2].Amount);
        Assert.Equal(0L, allocated[3].Amount);
    }

    [Fact]
    public void AllocateAndAllocateByWeights_GuardAgainstInvalidArguments()
    {
        Money amount = Money.FromVnd(10_000L);

        Assert.Throws<ArgumentNullException>(() => amount.Allocate(null!));
        Assert.Throws<ArgumentException>(() => amount.Allocate([]));
        Assert.Throws<ArgumentOutOfRangeException>(() => amount.Allocate([1, -1]));

        Assert.Throws<ArgumentNullException>(() => amount.AllocateByWeights(null!));
        Assert.Throws<ArgumentException>(() => amount.AllocateByWeights([]));
        Assert.Throws<ArgumentOutOfRangeException>(() => amount.AllocateByWeights([100L, -5L]));

        Money[] zeroWeights = amount.AllocateByWeights([0L, 0L]);
        Assert.All(zeroWeights, item => Assert.Equal(Money.Zero, item));
    }

    [Fact]
    public void ComputeMovingAverageCost_Br01_CalculatesWeightedAverageAndRoundsToNearestVnd()
    {
        // Standard weighted average: (10 * 20_000 + 30 * 24_000) / 40 = 920_000 / 40 = 23_000
        Money exactAverage = Money.ComputeMovingAverageCost(
            oldOnHand: 10,
            oldCostPrice: Money.FromVnd(20_000L),
            receivedQuantity: 30,
            receiptUnitCost: Money.FromVnd(24_000L));
        Assert.Equal(23_000L, exactAverage.Amount);

        // Rounding check: (2 * 10_000 + 1 * 11_000) / 3 = 31_000 / 3 = 10_333.33 -> 10_333
        Money roundedDown = Money.ComputeMovingAverageCost(
            oldOnHand: 2,
            oldCostPrice: Money.FromVnd(10_000L),
            receivedQuantity: 1,
            receiptUnitCost: Money.FromVnd(11_000L));
        Assert.Equal(10_333L, roundedDown.Amount);

        // Rounding half-up check: (1 * 10_000 + 2 * 11_000) / 3 = 32_000 / 3 = 10_666.66 -> 10_667
        Money roundedUp = Money.ComputeMovingAverageCost(
            oldOnHand: 1,
            oldCostPrice: Money.FromVnd(10_000L),
            receivedQuantity: 2,
            receiptUnitCost: Money.FromVnd(11_000L));
        Assert.Equal(10_667L, roundedUp.Amount);

        // Negative or zero oldOnHand is clamped to 0, so new cost equals receiptUnitCost
        Money fromNegativeOnHand = Money.ComputeMovingAverageCost(
            oldOnHand: -5,
            oldCostPrice: Money.FromVnd(15_000L),
            receivedQuantity: 10,
            receiptUnitCost: Money.FromVnd(19_500L));
        Assert.Equal(19_500L, fromNegativeOnHand.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ComputeMovingAverageCost_WithNonPositiveReceivedQuantity_ThrowsArgumentOutOfRangeException(int invalidQty)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Money.ComputeMovingAverageCost(10, Money.FromVnd(10_000L), invalidQty, Money.FromVnd(12_000L)));
    }
}
