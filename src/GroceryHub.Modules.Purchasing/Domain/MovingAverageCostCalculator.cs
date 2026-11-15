namespace GroceryHub.Modules.Purchasing.Domain;

public static class MovingAverageCostCalculator
{
    public static long Calculate(
        int currentOnHand,
        long currentCostPrice,
        int receivedQuantity,
        long receiptUnitCost)
    {
        if (receivedQuantity <= 0)
        {
            return Math.Max(0L, currentCostPrice);
        }

        if (receiptUnitCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(receiptUnitCost), "Receipt unit cost cannot be negative.");
        }

        if (currentOnHand <= 0)
        {
            return receiptUnitCost;
        }

        long totalUnits = (long)currentOnHand + receivedQuantity;
        if (totalUnits <= 0)
        {
            return receiptUnitCost;
        }

        long currentCost = Math.Max(0L, currentCostPrice);
        long totalValue = ((long)currentOnHand * currentCost) + ((long)receivedQuantity * receiptUnitCost);

        double rawAverage = (double)totalValue / totalUnits;
        return (long)Math.Round(rawAverage, MidpointRounding.AwayFromZero);
    }
}
