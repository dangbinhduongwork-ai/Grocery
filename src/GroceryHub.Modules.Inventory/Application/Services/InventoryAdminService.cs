using GroceryHub.Modules.Inventory.Application.DTOs;
using GroceryHub.Modules.Inventory.Domain;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Inventory.Application.Services;

public sealed class InventoryAdminService
{
    private readonly GroceryHubDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly IAuditLogPort _auditLogPort;

    public InventoryAdminService(
        GroceryHubDbContext dbContext,
        TimeProvider timeProvider,
        IAuditLogPort auditLogPort)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _auditLogPort = auditLogPort;
    }

    public async Task<Result<PagedResponse<AdminStockItemDto>>> GetInventoryAsync(
        string? q,
        Guid? categoryId,
        bool? lowStockOnly,
        bool? outOfStockOnly,
        int page,
        int pageSize,
        bool canReadCost,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = from s in _dbContext.StockItems.AsNoTracking()
                    join p in _dbContext.Products.AsNoTracking() on s.ProductId equals p.Id
                    where p.DeletedAt == null
                    select new
                    {
                        Stock = s,
                        Product = p
                    };

        if (!string.IsNullOrWhiteSpace(q))
        {
            string normalizedQ = VietnameseTextNormalizer.NormalizeForSearch(q.Trim());
            query = query.Where(x =>
                x.Product.SearchText.Contains(normalizedQ) ||
                x.Product.Sku.Contains(q) ||
                (x.Product.Barcode != null && x.Product.Barcode.Contains(q)));
        }

        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
        {
            query = query.Where(x => x.Product.CategoryId == categoryId.Value);
        }

        if (lowStockOnly == true)
        {
            query = query.Where(x => (x.Stock.OnHand - x.Stock.Reserved) <= x.Stock.ReorderLevel);
        }

        if (outOfStockOnly == true)
        {
            query = query.Where(x => (x.Stock.OnHand - x.Stock.Reserved) <= 0);
        }

        int totalCount = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(x => x.Product.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Stock.ProductId,
                x.Product.Sku,
                x.Product.Barcode,
                x.Product.Name,
                x.Product.Unit,
                x.Stock.OnHand,
                x.Stock.Reserved,
                x.Stock.ReorderLevel,
                x.Product.CostPrice,
                x.Stock.RowVersion
            })
            .ToListAsync(ct);

        var dtos = rows.Select(x =>
        {
            int available = x.OnHand - x.Reserved;
            bool isLow = available <= x.ReorderLevel;
            string rowVersionBase64 = x.RowVersion != null && x.RowVersion.Length > 0
                ? Convert.ToBase64String(x.RowVersion)
                : string.Empty;

            return new AdminStockItemDto(
                x.ProductId,
                x.Sku,
                x.Barcode,
                x.Name,
                x.Unit,
                x.OnHand,
                x.Reserved,
                available,
                x.ReorderLevel,
                isLow,
                canReadCost ? x.CostPrice : null,
                rowVersionBase64);
        }).ToList();

        return Result.Success(PagedResponse<AdminStockItemDto>.Create(dtos, page, pageSize, totalCount));
    }

    public Task<Result<PagedResponse<AdminStockItemDto>>> GetLowStockAsync(
        int page,
        int pageSize,
        bool canReadCost,
        CancellationToken ct = default)
    {
        return GetInventoryAsync(
            q: null,
            categoryId: null,
            lowStockOnly: true,
            outOfStockOnly: null,
            page: page,
            pageSize: pageSize,
            canReadCost: canReadCost,
            ct: ct);
    }

    public Task<Result<PagedResponse<ExpiryLotDto>>> GetNearExpiryAsync(
        int withinDays,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        return Task.FromResult(Result.Success(
            PagedResponse<ExpiryLotDto>.Create(Array.Empty<ExpiryLotDto>(), page, pageSize, 0)));
    }

    public async Task<Result<PagedResponse<StockMovementDto>>> GetMovementsAsync(
        Guid? productId,
        string? type,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        bool canReadCost,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = from m in _dbContext.StockMovements.AsNoTracking()
                    join p in _dbContext.Products.AsNoTracking() on m.ProductId equals p.Id
                    select new
                    {
                        Movement = m,
                        Product = p
                    };

        if (productId.HasValue && productId.Value != Guid.Empty)
        {
            query = query.Where(x => x.Movement.ProductId == productId.Value);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(x => x.Movement.Type == type);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.Movement.At >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.Movement.At <= to.Value);
        }

        int totalCount = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(x => x.Movement.At)
            .ThenByDescending(x => x.Movement.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Movement.Id,
                x.Movement.ProductId,
                x.Product.Sku,
                x.Product.Name,
                x.Movement.Type,
                x.Movement.Quantity,
                x.Movement.OnHandBefore,
                x.Movement.OnHandAfter,
                x.Movement.UnitCost,
                x.Movement.ReferenceType,
                x.Movement.ReferenceId,
                x.Movement.Reason,
                x.Movement.ActorUserId,
                x.Movement.At
            })
            .ToListAsync(ct);

        var dtos = rows.Select(x => new StockMovementDto(
            x.Id,
            x.ProductId,
            x.Sku,
            x.Name,
            x.Type,
            x.Quantity,
            x.OnHandBefore,
            x.OnHandAfter,
            canReadCost ? x.UnitCost : null,
            x.ReferenceType,
            x.ReferenceId,
            x.Reason,
            x.ActorUserId,
            x.At)).ToList();

        return Result.Success(PagedResponse<StockMovementDto>.Create(dtos, page, pageSize, totalCount));
    }

    public async Task<Result<AdminStockItemDto>> AdjustStockAsync(
        Guid productId,
        int delta,
        string reason,
        Guid? actorUserId,
        string? actorRole,
        string? ipAddress,
        string? traceId,
        bool canReadCost,
        CancellationToken ct = default)
    {
        if (delta == 0)
        {
            return Result.Failure<AdminStockItemDto>(
                DomainError.Validation(ErrorCodes.ValidationFailed, "Delta must not be zero."));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure<AdminStockItemDto>(
                DomainError.Validation(ErrorCodes.ValidationFailed, "Adjustment reason is required."));
        }

        var stockItem = await _dbContext.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == productId, ct);

        if (stockItem is null)
        {
            return Result.Failure<AdminStockItemDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Stock item for product {productId} was not found."));
        }

        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product is null)
        {
            return Result.Failure<AdminStockItemDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Product {productId} was not found."));
        }

        if (!InventoryInvariants.IsValidAdjustment(stockItem.OnHand, stockItem.Reserved, delta))
        {
            int available = stockItem.OnHand - stockItem.Reserved;
            return Result.Failure<AdminStockItemDto>(
                DomainError.Conflict(
                    ErrorCodes.InsufficientStock,
                    $"Cannot adjust stock by {delta}. OnHand: {stockItem.OnHand}, Reserved: {stockItem.Reserved}, Available: {available}."));
        }

        int oldOnHand = stockItem.OnHand;
        stockItem.OnHand += delta;
        stockItem.UpdatedAt = _timeProvider.GetUtcNow();

        var movement = new StockMovement
        {
            ProductId = productId,
            Type = StockMovementType.Adjustment,
            Quantity = delta,
            OnHandBefore = oldOnHand,
            OnHandAfter = stockItem.OnHand,
            UnitCost = product.CostPrice,
            ReferenceType = "ManualAdjustment",
            ReferenceId = null,
            Reason = reason,
            ActorUserId = actorUserId,
            At = _timeProvider.GetUtcNow()
        };

        _dbContext.StockMovements.Add(movement);

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            actorRole,
            "StockAdjusted",
            "StockItem",
            productId.ToString(),
            new { OnHand = oldOnHand, stockItem.Reserved },
            new { OnHand = stockItem.OnHand, stockItem.Reserved, Delta = delta, Reason = reason },
            ipAddress,
            traceId), ct);

        await _dbContext.SaveChangesAsync(ct);

        int availableAfter = stockItem.OnHand - stockItem.Reserved;
        bool isLowAfter = availableAfter <= stockItem.ReorderLevel;
        string rowVersionBase64 = stockItem.RowVersion != null && stockItem.RowVersion.Length > 0
            ? Convert.ToBase64String(stockItem.RowVersion)
            : string.Empty;

        return Result.Success(new AdminStockItemDto(
            product.Id,
            product.Sku,
            product.Barcode,
            product.Name,
            product.Unit,
            stockItem.OnHand,
            stockItem.Reserved,
            availableAfter,
            stockItem.ReorderLevel,
            isLowAfter,
            canReadCost ? product.CostPrice : null,
            rowVersionBase64));
    }

    public async Task<Result> UpdateReorderLevelAsync(
        Guid productId,
        int reorderLevel,
        CancellationToken ct = default)
    {
        if (reorderLevel < 0)
        {
            return Result.Failure(
                DomainError.Validation(ErrorCodes.ValidationFailed, "ReorderLevel cannot be negative."));
        }

        var stockItem = await _dbContext.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == productId, ct);

        if (stockItem is null)
        {
            return Result.Failure(
                DomainError.NotFound(ErrorCodes.NotFound, $"Stock item for product {productId} was not found."));
        }

        stockItem.ReorderLevel = reorderLevel;
        stockItem.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
