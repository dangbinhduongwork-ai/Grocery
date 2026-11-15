using GroceryHub.Modules.Purchasing.Domain;
using Xunit;

namespace GroceryHub.UnitTests.Purchasing;

public sealed class MovingAverageCostCalculatorTests
{
    [Fact]
    public void Calculate_WhenCurrentOnHandIsZero_ReturnsReceiptUnitCost()
    {
        long cost = MovingAverageCostCalculator.Calculate(
            currentOnHand: 0,
            currentCostPrice: 20_000,
            receivedQuantity: 50,
            receiptUnitCost: 25_000);

        Assert.Equal(25_000, cost);
    }

    [Fact]
    public void Calculate_WhenCurrentOnHandIsNegative_ReturnsReceiptUnitCost()
    {
        long cost = MovingAverageCostCalculator.Calculate(
            currentOnHand: -5,
            currentCostPrice: 20_000,
            receivedQuantity: 50,
            receiptUnitCost: 25_000);

        Assert.Equal(25_000, cost);
    }

    [Fact]
    public void Calculate_WhenReceivedQuantityIsZero_ReturnsCurrentCost()
    {
        long cost = MovingAverageCostCalculator.Calculate(
            currentOnHand: 10,
            currentCostPrice: 30_000,
            receivedQuantity: 0,
            receiptUnitCost: 40_000);

        Assert.Equal(30_000, cost);
    }

    [Fact]
    public void Calculate_StandardMovingAverage_ComputesCorrectly()
    {
        // 10 units @ 20,000đ = 200,000đ
        // 10 units @ 30,000đ = 300,000đ
        // Total 20 units, Total value 500,000đ -> 25,000đ
        long cost = MovingAverageCostCalculator.Calculate(
            currentOnHand: 10,
            currentCostPrice: 20_000,
            receivedQuantity: 10,
            receiptUnitCost: 30_000);

        Assert.Equal(25_000, cost);
    }

    [Fact]
    public void Calculate_WithFractionalVnd_RoundsAwayFromZero()
    {
        // 100 units @ 10,000đ = 1,000,000đ
        // 3 units @ 15,000đ = 45,000đ
        // Total units = 103, Total value = 1,045,000đ
        // 1,045,000 / 103 = 10145.631067... -> rounds to 10146
        long cost = MovingAverageCostCalculator.Calculate(
            currentOnHand: 100,
            currentCostPrice: 10_000,
            receivedQuantity: 3,
            receiptUnitCost: 15_000);

        Assert.Equal(10_146, cost);
    }

    [Fact]
    public void Calculate_WithNegativeReceiptCost_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MovingAverageCostCalculator.Calculate(10, 20_000, 5, -100));
    }
}
