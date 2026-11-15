using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Services;

public record GoodsReceiptLineInput(
    Guid ProductId,
    int ReceivedQuantity,
    long UnitCost,
    Guid? PurchaseOrderItemId = null);

public record GoodsReceiptIntakeCommand(
    string ReceiptNumber,
    Guid ReceivedBy,
    IReadOnlyList<GoodsReceiptLineInput> Items,
    Guid? PurchaseOrderId = null,
    string? Note = null);

public record GoodsReceiptIntakeResult(
    Guid GoodsReceiptId,
    string ReceiptNumber,
    string? PurchaseOrderStatus);

public interface IPurchasingPersistenceService
{
    Task<Result<GoodsReceiptIntakeResult>> ExecuteGoodsReceiptIntakeAsync(
        GoodsReceiptIntakeCommand command,
        CancellationToken ct = default);
}
