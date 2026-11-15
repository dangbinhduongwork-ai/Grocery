namespace GroceryHub.Persistence;

public static class DatabaseSequences
{
    public const string OrderNumberSeq = "OrderNumberSeq";
    public const string OrderNumber = OrderNumberSeq;

    public const string PaymentCodeSeq = "PaymentCodeSeq";
    public const string PaymentCode = PaymentCodeSeq;

    public const string PurchaseOrderSeq = "PurchaseOrderSeq";
    public const string PurchaseOrder = PurchaseOrderSeq;

    public const string GoodsReceiptSeq = "GoodsReceiptSeq";
    public const string GoodsReceipt = GoodsReceiptSeq;

    public const long PaymentCodeStartValue = 1_000_000_001L;
}
