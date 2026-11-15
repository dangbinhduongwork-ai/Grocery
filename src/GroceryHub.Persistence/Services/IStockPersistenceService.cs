using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Services;

public interface IStockPersistenceService
{
    Task<Result> ReserveStockAsync(
        Guid orderId,
        IReadOnlyList<(Guid ProductId, int Quantity)> items,
        DateTimeOffset expiresAt,
        CancellationToken ct = default);

    Task<Result> ReleaseStockReservationAsync(
        Guid orderId,
        CancellationToken ct = default);

    Task<Result> CommitStockReservationAsync(
        Guid orderId,
        CancellationToken ct = default);
}
