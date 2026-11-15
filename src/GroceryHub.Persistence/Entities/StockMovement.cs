namespace GroceryHub.Persistence.Entities;

public static class StockMovementType
{
    public const string PurchaseReceipt = "PurchaseReceipt";
    public const string Sale = "Sale";
    public const string Adjustment = "Adjustment";
    public const string Return = "Return";
    public const string Waste = "Waste";

    public static readonly IReadOnlyList<string> All =
    [
        PurchaseReceipt,
        Sale,
        Adjustment,
        Return,
        Waste
    ];
}

public class StockMovement
{
    public long Id { get; set; }

    public Guid ProductId { get; set; }

    public string Type { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int OnHandBefore { get; set; }

    public int OnHandAfter { get; set; }

    public long UnitCost { get; set; }

    public string ReferenceType { get; set; } = string.Empty;

    public Guid? ReferenceId { get; set; }

    public string? Reason { get; set; }

    public Guid? ActorUserId { get; set; }

    public DateTimeOffset At { get; set; }
}
