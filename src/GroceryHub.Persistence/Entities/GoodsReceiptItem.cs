namespace GroceryHub.Persistence.Entities;

public class GoodsReceiptItem
{
    public Guid Id { get; set; }

    public Guid GoodsReceiptId { get; set; }

    public Guid? PurchaseOrderItemId { get; set; }

    public Guid ProductId { get; set; }

    public int ReceivedQuantity { get; set; }

    public long UnitCost { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public GoodsReceipt? GoodsReceipt { get; set; }
}
