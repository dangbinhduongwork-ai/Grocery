using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public static class PurchaseOrderStatus
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string PartiallyReceived = "PartiallyReceived";
    public const string Received = "Received";
    public const string Closed = "Closed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Draft,
        Submitted,
        PartiallyReceived,
        Received,
        Closed,
        Cancelled
    ];
}

public class PurchaseOrder : IAuditableEntity
{
    public Guid Id { get; set; }

    public string PoNumber { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }

    public string Status { get; set; } = PurchaseOrderStatus.Draft;

    public long TotalAmount { get; set; }

    public string? Note { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Supplier? Supplier { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();

    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
}
