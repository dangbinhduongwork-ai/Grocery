using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Ordering.Application.DTOs;

#region Shipping & Quotes

public sealed record ShippingQuoteRequest(
    Guid? AddressId,
    string? ProvinceCode,
    long Subtotal);

public sealed record ShippingQuoteResponse(
    string ProvinceCode,
    string ProvinceName,
    bool Serviceable,
    string? ZoneName,
    long Fee,
    long? FreeShippingThreshold,
    long FreeShippingRemaining);

public sealed record CheckoutQuoteRequest(
    Guid AddressId,
    string? CouponCode,
    string? PaymentMethod);

public sealed record CheckoutQuoteItemDto(
    Guid ProductId,
    string Sku,
    string Name,
    string Unit,
    long UnitPrice,
    long ListPrice,
    int Quantity,
    long LineSubtotal,
    long DiscountAllocated,
    long LineTotal);

public sealed record CheckoutQuoteDto(
    long Subtotal,
    string? CouponCode,
    long CouponDiscount,
    long ShippingFee,
    long Total,
    long CodMaxAmount,
    IReadOnlyList<string> AvailablePaymentMethods,
    IReadOnlyList<CheckoutQuoteItemDto> Items,
    IReadOnlyList<string> Warnings);

#endregion

#region Orders Placement & Views

public sealed record PlaceOrderRequest(
    Guid AddressId,
    string? CouponCode,
    string PaymentMethod,
    string? Note);

public sealed record QrPaymentDetailsDto(
    string Url,
    string Bank,
    string AccountNumber,
    string AccountName,
    long Amount,
    string Content);

public sealed record OrderPaymentViewDto(
    Guid PaymentId,
    string Status,
    string Method,
    string PaymentCode,
    int AttemptNumber,
    int MaxAttempts,
    DateTimeOffset? ExpiresAt,
    QrPaymentDetailsDto? Qr);

public sealed record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string PaymentMethod,
    string PaymentStatus,
    long Subtotal,
    string? CouponCode,
    long CouponDiscount,
    long ShippingFee,
    long Total,
    DateTimeOffset PlacedAt,
    DateTimeOffset? ExpiresAt);

public sealed record PlaceOrderResponseDto(
    OrderSummaryDto Order,
    OrderPaymentViewDto Payment);

public sealed record AddressSnapshotDto(
    string RecipientName,
    string Phone,
    string ProvinceName,
    string WardName,
    string Street,
    string? Note);

public sealed record OrderItemViewDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string Name,
    string Unit,
    string? PackSize,
    long UnitPrice,
    int Quantity,
    long LineSubtotal,
    long DiscountAllocated,
    long LineTotal,
    string? ImageUrl);

public sealed record OrderStatusHistoryDto(
    string? FromStatus,
    string ToStatus,
    string? Reason,
    string? ActorRole,
    DateTimeOffset CreatedAt);

public sealed record CustomerOrderListItemDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string PaymentMethod,
    string PaymentStatus,
    long Total,
    int ItemCount,
    string FirstItemName,
    string? FirstItemImageUrl,
    DateTimeOffset PlacedAt,
    DateTimeOffset? ExpiresAt);

public sealed record CustomerOrderDetailDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string PaymentMethod,
    string PaymentStatus,
    long Subtotal,
    string? CouponCode,
    long CouponDiscount,
    long ShippingFee,
    long Total,
    AddressSnapshotDto Address,
    string? CustomerNote,
    IReadOnlyList<OrderItemViewDto> Items,
    IReadOnlyList<OrderStatusHistoryDto> Timeline,
    OrderPaymentViewDto Payment,
    bool CanCancel,
    bool CanRetryPayment,
    DateTimeOffset PlacedAt,
    DateTimeOffset? ExpiresAt);

public sealed record CancelOrderRequest(string Reason);

public sealed record SkippedReorderItemDto(
    Guid ProductId,
    string Name,
    string Reason);

public sealed record ReorderResponseDto(
    CartViewDto Cart,
    int AddedLines,
    IReadOnlyList<SkippedReorderItemDto> SkippedItems);

#endregion

#region Admin Orders

public sealed record AdminOrderListItemDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string PaymentMethod,
    string PaymentStatus,
    long Total,
    string RecipientName,
    string RecipientPhone,
    string ProvinceName,
    DateTimeOffset CreatedAt);

public sealed record AdminOrderDetailDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string PaymentMethod,
    string PaymentStatus,
    long Subtotal,
    string? CouponCode,
    long CouponDiscount,
    long ShippingFee,
    long Total,
    AddressSnapshotDto Address,
    string? CustomerNote,
    string? InternalNote,
    IReadOnlyList<OrderItemViewDto> Items,
    IReadOnlyList<OrderStatusHistoryDto> Timeline,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason);

public sealed record TransitionOrderRequest(
    string To,
    string? Reason,
    long? CodAmount);

public sealed record OrderNoteRequest(string InternalNote);

public sealed record OrderNoteResponseDto(Guid Id, string InternalNote);

public sealed record AssignOrderRequest(Guid? AssignedToUserId);

public sealed record AssignOrderResponseDto(Guid Id, Guid? AssignedToUserId, string AssignedToName);

public sealed record PickListItemDto(
    string Sku,
    string? Barcode,
    string ProductName,
    string Unit,
    string? PackSize,
    int Quantity,
    string? ImageUrl);

public sealed record PickListCategoryGroupDto(
    string CategoryName,
    IReadOnlyList<PickListItemDto> Items);

public sealed record PickListDto(
    Guid OrderId,
    string OrderNumber,
    string Status,
    string? CustomerNote,
    IReadOnlyList<PickListCategoryGroupDto> Groups);

public sealed record DeliveryNoteDto(
    Guid OrderId,
    string OrderNumber,
    DateTimeOffset PlacedAt,
    string RecipientName,
    string Phone,
    string FullAddress,
    string PaymentMethod,
    long CodAmountToCollect,
    IReadOnlyList<OrderItemViewDto> Items,
    long Subtotal,
    long CouponDiscount,
    long ShippingFee,
    long Total);

#endregion

#region Shipping Zones & Rates

public sealed record ShippingRateDto(
    Guid Id,
    long BaseFee,
    long FreeShippingThreshold,
    int EstimatedDaysMin,
    int EstimatedDaysMax,
    bool IsActive);

public sealed record ShippingZoneDto(
    Guid Id,
    string Name,
    bool IsActive,
    IReadOnlyList<string> ProvinceCodes,
    IReadOnlyList<ShippingRateDto> Rates);

public sealed record CreateShippingRateInput(
    long BaseFee,
    long FreeShippingThreshold,
    int EstimatedDaysMin,
    int EstimatedDaysMax,
    bool IsActive);

public sealed record CreateShippingZoneRequest(
    string Name,
    bool IsActive,
    IReadOnlyList<string> ProvinceCodes,
    IReadOnlyList<CreateShippingRateInput> Rates);

public sealed record UpdateShippingZoneRequest(
    string Name,
    bool IsActive,
    IReadOnlyList<string> ProvinceCodes,
    IReadOnlyList<CreateShippingRateInput> Rates);

#endregion

#region Store Settings

public sealed record StoreSettingsDto(
    string StoreName,
    string SupportPhone,
    string SupportEmail,
    bool SePayEnabled,
    string BankShortName,
    string BankAccountNumber,
    string BankAccountName,
    string? TransferContentPrefix,
    int QrExpiryMinutes,
    long FreeShippingThreshold,
    long CodMaxAmount,
    int MaxQuantityPerLine,
    int DefaultLowStockThreshold);

#endregion
