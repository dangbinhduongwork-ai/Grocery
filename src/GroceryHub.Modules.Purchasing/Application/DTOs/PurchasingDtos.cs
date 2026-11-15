namespace GroceryHub.Modules.Purchasing.Application.DTOs;

public sealed record SupplierDto(
    Guid Id,
    string Code,
    string Name,
    string? ContactPerson,
    string Phone,
    string? Email,
    string? Address,
    string? TaxCode,
    string? Note,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record CreateSupplierRequest(
    string Code,
    string Name,
    string? ContactPerson,
    string Phone,
    string? Email,
    string? Address,
    string? TaxCode,
    string? Note);

public sealed record UpdateSupplierRequest(
    string Name,
    string? ContactPerson,
    string Phone,
    string? Email,
    string? Address,
    string? TaxCode,
    string? Note,
    bool IsActive);

public sealed record PurchaseOrderLineDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    string Unit,
    int OrderedQuantity,
    int ReceivedQuantity,
    long UnitCost,
    long Subtotal);

public sealed record PurchaseOrderSummaryDto(
    Guid Id,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    string Status,
    long TotalAmount,
    int TotalOrderedQuantity,
    int TotalReceivedQuantity,
    Guid CreatedBy,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt);

public sealed record PurchaseOrderDetailDto(
    Guid Id,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    string Status,
    long TotalAmount,
    string? Note,
    Guid CreatedBy,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt,
    string RowVersion,
    IReadOnlyList<PurchaseOrderLineDto> Items);

public sealed record PurchaseOrderItemRequest(
    Guid ProductId,
    int OrderedQuantity,
    long UnitCost);

public sealed record CreatePurchaseOrderRequest(
    Guid SupplierId,
    string? Note,
    IReadOnlyList<PurchaseOrderItemRequest> Items);

public sealed record UpdatePurchaseOrderRequest(
    string? Note,
    IReadOnlyList<PurchaseOrderItemRequest> Items);

public sealed record CancelPurchaseOrderRequest(
    string? Reason);

public sealed record ReceivePurchaseOrderItemRequest(
    Guid? PurchaseOrderItemId,
    Guid ProductId,
    int ReceivedQuantity,
    long UnitCost);

public sealed record ReceivePurchaseOrderRequest(
    string? ReceiptNumber,
    string? Note,
    IReadOnlyList<ReceivePurchaseOrderItemRequest> Items);

public sealed record GoodsReceiptSummaryDto(
    Guid Id,
    string ReceiptNumber,
    Guid? PurchaseOrderId,
    string? PoNumber,
    Guid ReceivedBy,
    DateTimeOffset ReceivedAt,
    int TotalQuantity,
    string? Note,
    DateTimeOffset CreatedAt);

public sealed record GoodsReceiptLineDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    string Unit,
    int ReceivedQuantity,
    long UnitCost,
    Guid? PurchaseOrderItemId);

public sealed record GoodsReceiptDetailDto(
    Guid Id,
    string ReceiptNumber,
    Guid? PurchaseOrderId,
    string? PoNumber,
    Guid ReceivedBy,
    DateTimeOffset ReceivedAt,
    string? Note,
    DateTimeOffset CreatedAt,
    IReadOnlyList<GoodsReceiptLineDto> Items);

public sealed record CreateGoodsReceiptLineRequest(
    Guid ProductId,
    int Quantity,
    long UnitCost,
    DateOnly? ExpiryDate = null,
    string? BatchNo = null,
    Guid? PurchaseOrderItemId = null);

public sealed record CreateGoodsReceiptRequest(
    Guid? PurchaseOrderId,
    Guid SupplierId,
    string? InvoiceNo,
    string? Note,
    IReadOnlyList<CreateGoodsReceiptLineRequest> Lines);
