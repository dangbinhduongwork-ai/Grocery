using GroceryHub.Modules.Ordering.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Ordering.Application.Services;

public sealed class OrderVerificationService : IOrderVerificationContract
{
    private readonly GroceryHubDbContext _dbContext;

    public OrderVerificationService(GroceryHubDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Result<VerifiedOrderItemPurchaseDto>> VerifyPurchasedOrderItemAsync(
        Guid userId,
        Guid productId,
        Guid orderItemId,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty || productId == Guid.Empty || orderItemId == Guid.Empty)
        {
            return Result.Failure<VerifiedOrderItemPurchaseDto>(
                DomainError.Validation("INVALID_IDENTIFIERS", "UserId, ProductId, and OrderItemId must not be empty."));
        }

        var orderItem = await _dbContext.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
                .ThenInclude(o => o!.StatusHistory)
            .FirstOrDefaultAsync(oi => oi.Id == orderItemId, ct);

        if (orderItem is null || orderItem.Order is null)
        {
            return Result.Failure<VerifiedOrderItemPurchaseDto>(
                DomainError.Forbidden("REVIEW_NOT_ELIGIBLE", "Order line item does not exist."));
        }

        if (orderItem.Order.UserId != userId)
        {
            return Result.Failure<VerifiedOrderItemPurchaseDto>(
                DomainError.Forbidden("REVIEW_NOT_ELIGIBLE", "Order does not belong to the caller."));
        }

        if (orderItem.ProductId != productId)
        {
            return Result.Failure<VerifiedOrderItemPurchaseDto>(
                DomainError.Forbidden("REVIEW_NOT_ELIGIBLE", "Order item does not match product."));
        }

        if (orderItem.Order.Status != OrderStatusConstants.Delivered)
        {
            return Result.Failure<VerifiedOrderItemPurchaseDto>(
                DomainError.Forbidden("REVIEW_NOT_ELIGIBLE", "Order is not in Delivered status. Only delivered purchases can be reviewed."));
        }

        DateTimeOffset? deliveredAt = orderItem.Order.StatusHistory
            .OrderByDescending(h => h.CreatedAt)
            .FirstOrDefault(h => h.ToStatus == OrderStatusConstants.Delivered)?.CreatedAt
            ?? orderItem.Order.UpdatedAt;

        return Result.Success(new VerifiedOrderItemPurchaseDto(
            orderItem.OrderId,
            orderItem.Order.OrderNumber,
            orderItem.Id,
            orderItem.ProductId,
            deliveredAt));
    }
}
