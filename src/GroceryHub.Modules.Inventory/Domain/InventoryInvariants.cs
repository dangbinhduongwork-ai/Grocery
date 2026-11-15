namespace GroceryHub.Modules.Inventory.Domain;

public static class InventoryInvariants
{
    public static readonly string[] AllowedAdjustmentReasons =
    [
        "Damage",
        "CountCorrection",
        "Expiry",
        "Theft"
    ];

    public static bool IsValidAdjustment(int onHand, int reserved, int delta)
    {
        return (onHand + delta) >= reserved && (onHand + delta) >= 0;
    }
}
