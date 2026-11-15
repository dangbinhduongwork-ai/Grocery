using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Catalog.Contracts;

public sealed record UpdateProductReviewAggregatesCommand(
    Guid ProductId,
    decimal RatingAvg,
    int RatingCount);

public interface ICatalogReviewSummaryPort
{
    Task<Result> UpdateReviewAggregatesAsync(
        UpdateProductReviewAggregatesCommand command,
        CancellationToken ct = default);
}
