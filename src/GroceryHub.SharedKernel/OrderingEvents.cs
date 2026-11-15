namespace GroceryHub.SharedKernel;

public sealed record OrderPlacedLineItem(
    Guid ProductId,
    string Sku,
    int Quantity,
    long UnitPrice,
    long DiscountAllocated,
    long LineTotal);

public sealed record OrderPlacedEvent(
    Guid EventId,
    Guid OrderId,
    string OrderNumber,
    Guid UserId,
    string PaymentMethod,
    string InitialStatus,
    long Subtotal,
    string? CouponCode,
    long CouponDiscount,
    long ShippingFee,
    long Total,
    IReadOnlyList<OrderPlacedLineItem> Items,
    DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public string EventType => "ordering.order_placed.v1";
}

public sealed record OrderConfirmedEvent(
    Guid EventId,
    Guid OrderId,
    string OrderNumber,
    Guid? ActorUserId,
    string ConfirmationSource,
    DateTimeOffset ConfirmedAt,
    DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public string EventType => "ordering.order_confirmed.v1";
}

public sealed record OrderPackedEvent(
    Guid EventId,
    Guid OrderId,
    string OrderNumber,
    Guid PackedByUserId,
    DateTimeOffset PackedAt,
    DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public string EventType => "ordering.order_packed.v1";
}

public sealed record OrderShippedCostSnapshotItem(
    Guid OrderItemId,
    Guid ProductId,
    int Quantity,
    long UnitCostSnapshot);

public sealed record OrderShippedEvent(
    Guid EventId,
    Guid OrderId,
    string OrderNumber,
    Guid ShippedByUserId,
    IReadOnlyList<OrderShippedCostSnapshotItem> Items,
    DateTimeOffset ShippedAt,
    DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public string EventType => "ordering.order_shipped.v1";
}

public sealed record ProductSoldItem(Guid ProductId, int QuantitySold);

public sealed record OrderDeliveredEvent(
    Guid EventId,
    Guid OrderId,
    string OrderNumber,
    Guid UserId,
    Guid DeliveredByUserId,
    string PaymentMethod,
    long Total,
    IReadOnlyList<ProductSoldItem> DeliveredItems,
    DateTimeOffset DeliveredAt,
    DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public string EventType => "ordering.order_delivered.v1";
}

public sealed record OrderCancelledEvent(
    Guid EventId,
    Guid OrderId,
    string OrderNumber,
    Guid UserId,
    string PreviousStatus,
    string PreviousPaymentStatus,
    Guid? CancelledByUserId,
    string CancelledByRole,
    string CancelReason,
    bool StockReturnedFromShipping,
    Guid? AutoCreatedRefundId,
    DateTimeOffset CancelledAt,
    DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public string EventType => "ordering.order_cancelled.v1";
}
