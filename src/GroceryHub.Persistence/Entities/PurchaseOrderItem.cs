using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public class PurchaseOrderItem : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid PurchaseOrderId { get; set; }

    public Guid ProductId { get; set; }

    public int OrderedQuantity { get; set; }

    public int ReceivedQuantity { get; set; }

    public long UnitCost { get; set; }

    public long Subtotal { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }
}
