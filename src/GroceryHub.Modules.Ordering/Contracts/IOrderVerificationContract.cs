using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Ordering.Contracts;

public sealed record VerifiedOrderItemPurchaseDto(
    Guid OrderId,
    string OrderNumber,
    Guid OrderItemId,
    Guid ProductId,
    DateTimeOffset? DeliveredAt);

/// <summary>
/// Port implemented by Ordering and consumed by Reviews to verify BR-09 (Delivered order line owned by customer).
/// </summary>
public interface IOrderVerificationContract
{
    Task<Result<VerifiedOrderItemPurchaseDto>> VerifyPurchasedOrderItemAsync(
        Guid userId,
        Guid productId,
        Guid orderItemId,
        CancellationToken ct = default);
}
