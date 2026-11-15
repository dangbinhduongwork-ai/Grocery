using System.Data;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GroceryHub.Persistence.Services;

public sealed class StockPersistenceService(
    GroceryHubDbContext dbContext,
    TimeProvider timeProvider) : IStockPersistenceService
{
    private readonly GroceryHubDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<Result> ReserveStockAsync(
        Guid orderId,
        IReadOnlyList<(Guid ProductId, int Quantity)> items,
        DateTimeOffset expiresAt,
        CancellationToken ct = default)
    {
        if (orderId == Guid.Empty)
        {
            return Result.Failure(DomainError.Validation(ErrorCodes.ValidationFailed, "OrderId cannot be empty."));
        }

        if (items is null || items.Count == 0)
        {
            return Result.Failure(DomainError.Validation(ErrorCodes.ValidationFailed, "Items list cannot be empty."));
        }

        if (items.Any(static i => i.Quantity <= 0))
        {
            return Result.Failure(DomainError.Validation(ErrorCodes.ValidationFailed, "Item quantity must be greater than zero."));
        }

        var consolidatedItems = items
            .GroupBy(static i => i.ProductId)
            .Select(static g => (ProductId: g.Key, Quantity: g.Sum(static x => x.Quantity)))
            .OrderBy(static x => x.ProductId)
            .ToList();

        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            foreach (var item in consolidatedItems)
            {
                int affected = await _dbContext.StockItems
                    .Where(s => s.ProductId == item.ProductId && s.OnHand - s.Reserved >= item.Quantity)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(b => b.Reserved, b => b.Reserved + item.Quantity),
                        ct);

                if (affected < 1)
                {
                    await transaction.RollbackAsync(ct);
                    return Result.Failure(DomainError.Conflict(
                        ErrorCodes.InsufficientStock,
                        $"Insufficient stock for product '{item.ProductId}'."));
                }
            }

            DateTimeOffset utcNow = _timeProvider.GetUtcNow();
            List<StockReservation> reservations = new(consolidatedItems.Count);

            foreach (var item in consolidatedItems)
            {
                reservations.Add(new StockReservation
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Status = StockReservationStatus.Active,
                    ExpiresAt = expiresAt,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow
                });
            }

            await _dbContext.StockReservations.AddRangeAsync(reservations, ct);
            await _dbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
            return Result.Success();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<Result> ReleaseStockReservationAsync(
        Guid orderId,
        CancellationToken ct = default)
    {
        if (orderId == Guid.Empty)
        {
            return Result.Failure(DomainError.Validation(ErrorCodes.ValidationFailed, "OrderId cannot be empty."));
        }

        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var activeReservations = await _dbContext.StockReservations
                .Where(r => r.OrderId == orderId && r.Status == StockReservationStatus.Active)
                .OrderBy(static r => r.ProductId)
                .ToListAsync(ct);

            if (activeReservations.Count == 0)
            {
                await transaction.RollbackAsync(ct);
                return Result.Success();
            }

            foreach (StockReservation res in activeReservations)
            {
                await _dbContext.StockItems
                    .Where(s => s.ProductId == res.ProductId && s.Reserved >= res.Quantity)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(b => b.Reserved, b => b.Reserved - res.Quantity),
                        ct);
            }

            DateTimeOffset utcNow = _timeProvider.GetUtcNow();
            await _dbContext.StockReservations
                .Where(r => r.OrderId == orderId && r.Status == StockReservationStatus.Active)
                .ExecuteUpdateAsync(
                    r => r
                        .SetProperty(b => b.Status, StockReservationStatus.Released)
                        .SetProperty(b => b.UpdatedAt, utcNow),
                    ct);

            await transaction.CommitAsync(ct);
            return Result.Success();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<Result> CommitStockReservationAsync(
        Guid orderId,
        CancellationToken ct = default)
    {
        if (orderId == Guid.Empty)
        {
            return Result.Failure(DomainError.Validation(ErrorCodes.ValidationFailed, "OrderId cannot be empty."));
        }

        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var activeReservations = await _dbContext.StockReservations
                .Where(r => r.OrderId == orderId && r.Status == StockReservationStatus.Active)
                .OrderBy(static r => r.ProductId)
                .ToListAsync(ct);

            if (activeReservations.Count == 0)
            {
                await transaction.RollbackAsync(ct);
                return Result.Failure(DomainError.NotFound(
                    "ReservationsNotFound",
                    $"No active stock reservations found for order '{orderId}'."));
            }

            DateTimeOffset utcNow = _timeProvider.GetUtcNow();

            foreach (StockReservation res in activeReservations)
            {
                StockItem? stockItem = await _dbContext.StockItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ProductId == res.ProductId, ct);

                if (stockItem is null)
                {
                    await transaction.RollbackAsync(ct);
                    return Result.Failure(DomainError.NotFound(
                        "StockItemNotFound",
                        $"Stock item for product '{res.ProductId}' was not found."));
                }

                int onHandBefore = stockItem.OnHand;

                int affected = await _dbContext.StockItems
                    .Where(s => s.ProductId == res.ProductId && s.OnHand >= res.Quantity && s.Reserved >= res.Quantity)
                    .ExecuteUpdateAsync(
                        s => s
                            .SetProperty(b => b.OnHand, b => b.OnHand - res.Quantity)
                            .SetProperty(b => b.Reserved, b => b.Reserved - res.Quantity),
                        ct);

                if (affected < 1)
                {
                    await transaction.RollbackAsync(ct);
                    return Result.Failure(DomainError.Conflict(
                        "CommitFailed",
                        $"Failed to deduct stock for product '{res.ProductId}'."));
                }

                int onHandAfter = onHandBefore - res.Quantity;

                Product? product = await _dbContext.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == res.ProductId, ct);
                long unitCost = product?.CostPrice ?? 0L;

                StockMovement movement = new()
                {
                    ProductId = res.ProductId,
                    Type = StockMovementType.Sale,
                    Quantity = -res.Quantity,
                    OnHandBefore = onHandBefore,
                    OnHandAfter = onHandAfter,
                    UnitCost = unitCost,
                    ReferenceType = "Order",
                    ReferenceId = orderId,
                    Reason = "Sale",
                    ActorUserId = null,
                    At = utcNow
                };

                _dbContext.StockMovements.Add(movement);
            }

            await _dbContext.StockReservations
                .Where(r => r.OrderId == orderId && r.Status == StockReservationStatus.Active)
                .ExecuteUpdateAsync(
                    r => r
                        .SetProperty(b => b.Status, StockReservationStatus.Committed)
                        .SetProperty(b => b.UpdatedAt, utcNow),
                    ct);

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Result.Success();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
