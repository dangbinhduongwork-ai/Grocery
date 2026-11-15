namespace GroceryHub.Persistence.Entities;

public class GoodsReceipt
{
    public Guid Id { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;

    public Guid? PurchaseOrderId { get; set; }

    public Guid ReceivedBy { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }

    public ICollection<GoodsReceiptItem> Items { get; set; } = new List<GoodsReceiptItem>();
}
