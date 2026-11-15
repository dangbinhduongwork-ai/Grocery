using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public class StockItem : IAuditableEntity
{
    public Guid ProductId { get; set; }

    public int OnHand { get; set; }

    public int Reserved { get; set; }

    public int ReorderLevel { get; set; } = 10;

    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<StockReservation> Reservations { get; set; } = new List<StockReservation>();
}
