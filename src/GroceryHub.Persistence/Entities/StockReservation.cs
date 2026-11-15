using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public static class StockReservationStatus
{
    public const string Active = "Active";
    public const string Released = "Released";
    public const string Committed = "Committed";

    public static readonly IReadOnlyList<string> All = [Active, Released, Committed];
}

public class StockReservation : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public string Status { get; set; } = StockReservationStatus.Active;

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public StockItem? StockItem { get; set; }
}
