using System.Data;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GroceryHub.Persistence.Services;

public sealed class PurchasingPersistenceService(
    GroceryHubDbContext dbContext,
    TimeProvider timeProvider) : IPurchasingPersistenceService
{
    private readonly GroceryHubDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<Result<GoodsReceiptIntakeResult>> ExecuteGoodsReceiptIntakeAsync(
        GoodsReceiptIntakeCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.ReceiptNumber))
        {
            return Result.Failure<GoodsReceiptIntakeResult>(
                DomainError.Validation(ErrorCodes.ValidationFailed, "ReceiptNumber cannot be empty."));
        }

        if (command.ReceivedBy == Guid.Empty)
        {
            return Result.Failure<GoodsReceiptIntakeResult>(
                DomainError.Validation(ErrorCodes.ValidationFailed, "ReceivedBy cannot be empty."));
        }

        if (command.Items is null || command.Items.Count == 0)
        {
            return Result.Failure<GoodsReceiptIntakeResult>(
                DomainError.Validation(ErrorCodes.ValidationFailed, "Goods receipt items list cannot be empty."));
        }

        if (command.Items.Any(static i => i.ReceivedQuantity <= 0 || i.UnitCost < 0))
        {
            return Result.Failure<GoodsReceiptIntakeResult>(
                DomainError.Validation(ErrorCodes.ValidationFailed, "Received quantity must be > 0 and unit cost must be >= 0."));
        }

        bool ownsTransaction = _dbContext.Database.CurrentTransaction is null;
        await using IDbContextTransaction? transaction = ownsTransaction
            ? await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct)
            : null;
        try
        {
            DateTimeOffset utcNow = _timeProvider.GetUtcNow();

            PurchaseOrder? po = null;
            if (command.PurchaseOrderId.HasValue && command.PurchaseOrderId.Value != Guid.Empty)
            {
                po = await _dbContext.PurchaseOrders
                    .Include(static p => p.Items)
                    .FirstOrDefaultAsync(p => p.Id == command.PurchaseOrderId.Value, ct);

                if (po is null)
                {
                    if (transaction is not null)
                    {
                        await transaction.RollbackAsync(ct);
                    }
                    return Result.Failure<GoodsReceiptIntakeResult>(
                        DomainError.NotFound("PurchaseOrderNotFound", $"Purchase order '{command.PurchaseOrderId.Value}' was not found."));
                }
            }

            GoodsReceipt receipt = new()
            {
                Id = Guid.NewGuid(),
                ReceiptNumber = command.ReceiptNumber.Trim(),
                PurchaseOrderId = command.PurchaseOrderId,
                ReceivedBy = command.ReceivedBy,
                ReceivedAt = utcNow,
                Note = command.Note?.Trim(),
                CreatedAt = utcNow
            };

            _dbContext.GoodsReceipts.Add(receipt);

            var sortedItems = command.Items.OrderBy(static i => i.ProductId).ToList();

            foreach (GoodsReceiptLineInput line in sortedItems)
            {
                GoodsReceiptItem grItem = new()
                {
                    Id = Guid.NewGuid(),
                    GoodsReceiptId = receipt.Id,
                    PurchaseOrderItemId = line.PurchaseOrderItemId,
                    ProductId = line.ProductId,
                    ReceivedQuantity = line.ReceivedQuantity,
                    UnitCost = line.UnitCost,
                    CreatedAt = utcNow
                };

                _dbContext.GoodsReceiptItems.Add(grItem);

                if (po is not null && line.PurchaseOrderItemId.HasValue)
                {
                    PurchaseOrderItem? poItem = po.Items.FirstOrDefault(i => i.Id == line.PurchaseOrderItemId.Value);
                    if (poItem is not null)
                    {
                        poItem.ReceivedQuantity += line.ReceivedQuantity;
                        poItem.UpdatedAt = utcNow;
                    }
                }

                StockItem? stockItem = await _dbContext.StockItems
                    .FirstOrDefaultAsync(s => s.ProductId == line.ProductId, ct);

                if (stockItem is null)
                {
                    stockItem = new StockItem
                    {
                        ProductId = line.ProductId,
                        OnHand = 0,
                        Reserved = 0,
                        ReorderLevel = 10,
                        CreatedAt = utcNow,
                        UpdatedAt = utcNow
                    };
                    _dbContext.StockItems.Add(stockItem);
                }

                int currentOnHand = stockItem.OnHand;
                int onHandBefore = currentOnHand;

                Product? product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.Id == line.ProductId, ct);

                long currentCost = product?.CostPrice ?? 0L;
                long newCost;

                if (currentOnHand <= 0)
                {
                    newCost = line.UnitCost;
                }
                else
                {
                    long totalUnits = (long)currentOnHand + line.ReceivedQuantity;
                    long totalValue = ((long)currentOnHand * currentCost) + ((long)line.ReceivedQuantity * line.UnitCost);
                    newCost = (long)Math.Round((double)totalValue / totalUnits, MidpointRounding.AwayFromZero);
                }

                if (product is not null)
                {
                    product.CostPrice = newCost;
                    product.UpdatedAt = utcNow;
                }

                stockItem.OnHand += line.ReceivedQuantity;
                stockItem.UpdatedAt = utcNow;
                int onHandAfter = stockItem.OnHand;

                StockMovement movement = new()
                {
                    ProductId = line.ProductId,
                    Type = StockMovementType.PurchaseReceipt,
                    Quantity = line.ReceivedQuantity,
                    OnHandBefore = onHandBefore,
                    OnHandAfter = onHandAfter,
                    UnitCost = line.UnitCost,
                    ReferenceType = "GoodsReceipt",
                    ReferenceId = receipt.Id,
                    Reason = "Goods Receipt Intake",
                    ActorUserId = command.ReceivedBy,
                    At = utcNow
                };

                _dbContext.StockMovements.Add(movement);
            }

            if (po is not null)
            {
                bool allReceived = po.Items.Count > 0 && po.Items.All(static i => i.ReceivedQuantity >= i.OrderedQuantity);
                po.Status = allReceived
                    ? PurchaseOrderStatus.Received
                    : PurchaseOrderStatus.PartiallyReceived;
                po.UpdatedAt = utcNow;
            }

            await _dbContext.SaveChangesAsync(ct);
            if (transaction is not null)
            {
                await transaction.CommitAsync(ct);
            }

            return Result.Success(new GoodsReceiptIntakeResult(
                receipt.Id,
                receipt.ReceiptNumber,
                po?.Status));
        }
        catch (Exception)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(ct);
            }
            throw;
        }
    }
}
