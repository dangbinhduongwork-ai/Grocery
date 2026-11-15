using GroceryHub.Modules.Catalog.Contracts;
using GroceryHub.Persistence;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Catalog.Application.Services;

public sealed class CatalogReviewSummaryService : ICatalogReviewSummaryPort
{
    private readonly GroceryHubDbContext _dbContext;

    public CatalogReviewSummaryService(GroceryHubDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Result> UpdateReviewAggregatesAsync(
        UpdateProductReviewAggregatesCommand command,
        CancellationToken ct = default)
    {
        if (command == null)
        {
            return Result.Failure(DomainError.Validation("INVALID_COMMAND", "Command cannot be null."));
        }

        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == command.ProductId, ct);
        if (product is null)
        {
            return Result.Failure(DomainError.NotFound($"Product with ID '{command.ProductId}' not found."));
        }

        product.RatingAvg = command.RatingAvg;
        product.RatingCount = command.RatingCount;
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
