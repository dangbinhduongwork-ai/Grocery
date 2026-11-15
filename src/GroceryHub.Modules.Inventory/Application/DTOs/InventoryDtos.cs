namespace GroceryHub.Modules.Inventory.Application.DTOs;

public sealed record AdminStockItemDto(
    Guid ProductId,
    string Sku,
    string? Barcode,
    string ProductName,
    string Unit,
    int OnHand,
    int Reserved,
    int Available,
    int ReorderLevel,
    bool IsLowStock,
    long? CostPrice,
    string RowVersion);

public sealed record StockMovementDto(
    long Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    string Type,
    int Quantity,
    int OnHandBefore,
    int OnHandAfter,
    long? UnitCost,
    string ReferenceType,
    Guid? ReferenceId,
    string? Reason,
    Guid? ActorUserId,
    DateTimeOffset At);

public sealed record AdjustStockRequest(
    int Delta,
    string Reason);

public sealed record AdminAdjustStockRequest(
    Guid ProductId,
    int Delta,
    string Reason);

public sealed record UpdateReorderLevelRequest(
    int ReorderLevel);

public sealed record ExpiryLotDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    Guid? ReceiptLineId,
    DateOnly ExpiryDate,
    int DaysRemaining,
    int QuantityRemaining);
