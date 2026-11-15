using GroceryHub.Modules.Purchasing.Application.DTOs;
using GroceryHub.Modules.Purchasing.Domain;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.Persistence.Services;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Purchasing.Application.Services;

public sealed class PurchasingAdminService
{
    private readonly GroceryHubDbContext _dbContext;
    private readonly IPurchasingPersistenceService _purchasingPersistenceService;
    private readonly TimeProvider _timeProvider;
    private readonly IAuditLogPort _auditLogPort;

    public PurchasingAdminService(
        GroceryHubDbContext dbContext,
        IPurchasingPersistenceService purchasingPersistenceService,
        TimeProvider timeProvider,
        IAuditLogPort auditLogPort)
    {
        _dbContext = dbContext;
        _purchasingPersistenceService = purchasingPersistenceService;
        _timeProvider = timeProvider;
        _auditLogPort = auditLogPort;
    }

    #region Suppliers

    public async Task<Result<PagedResponse<SupplierDto>>> GetSuppliersAsync(
        string? q,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Suppliers
            .AsNoTracking()
            .Where(s => s.DeletedAt == null);

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            string trimmed = q.Trim();
            query = query.Where(s =>
                s.Name.Contains(trimmed) ||
                s.Code.Contains(trimmed) ||
                s.Phone.Contains(trimmed));
        }

        int totalCount = await query.CountAsync(ct);

        var dtos = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SupplierDto(
                s.Id,
                s.Code,
                s.Name,
                s.ContactPerson,
                s.Phone,
                s.Email,
                s.Address,
                s.TaxCode,
                s.Note,
                s.IsActive,
                s.CreatedAt))
            .ToListAsync(ct);

        return Result.Success(PagedResponse<SupplierDto>.Create(dtos, page, pageSize, totalCount));
    }

    public async Task<Result<SupplierDto>> GetSupplierByIdAsync(Guid id, CancellationToken ct = default)
    {
        var supplier = await _dbContext.Suppliers
            .AsNoTracking()
            .Where(s => s.DeletedAt == null && s.Id == id)
            .Select(s => new SupplierDto(
                s.Id,
                s.Code,
                s.Name,
                s.ContactPerson,
                s.Phone,
                s.Email,
                s.Address,
                s.TaxCode,
                s.Note,
                s.IsActive,
                s.CreatedAt))
            .FirstOrDefaultAsync(ct);

        if (supplier is null)
        {
            return Result.Failure<SupplierDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Supplier {id} was not found."));
        }

        return Result.Success(supplier);
    }

    public async Task<Result<SupplierDto>> CreateSupplierAsync(
        CreateSupplierRequest request,
        Guid? actorUserId,
        string? actorRole,
        string? ipAddress,
        string? traceId,
        CancellationToken ct = default)
    {
        string normalizedCode = request.Code.Trim();
        bool exists = await _dbContext.Suppliers
            .AnyAsync(s => s.DeletedAt == null && s.Code == normalizedCode, ct);

        if (exists)
        {
            return Result.Failure<SupplierDto>(
                DomainError.Conflict(ErrorCodes.Conflict, $"Supplier code '{normalizedCode}' already exists."));
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            Name = request.Name.Trim(),
            ContactPerson = request.ContactPerson?.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            TaxCode = request.TaxCode?.Trim(),
            Note = request.Note?.Trim(),
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        _dbContext.Suppliers.Add(supplier);

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            actorRole,
            "SupplierCreated",
            "Supplier",
            supplier.Id.ToString(),
            null,
            supplier,
            ipAddress,
            traceId), ct);

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new SupplierDto(
            supplier.Id,
            supplier.Code,
            supplier.Name,
            supplier.ContactPerson,
            supplier.Phone,
            supplier.Email,
            supplier.Address,
            supplier.TaxCode,
            supplier.Note,
            supplier.IsActive,
            supplier.CreatedAt));
    }

    public async Task<Result<SupplierDto>> UpdateSupplierAsync(
        Guid id,
        UpdateSupplierRequest request,
        Guid? actorUserId,
        string? actorRole,
        string? ipAddress,
        string? traceId,
        CancellationToken ct = default)
    {
        var supplier = await _dbContext.Suppliers
            .FirstOrDefaultAsync(s => s.DeletedAt == null && s.Id == id, ct);

        if (supplier is null)
        {
            return Result.Failure<SupplierDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Supplier {id} was not found."));
        }

        var beforeState = new
        {
            supplier.Name,
            supplier.ContactPerson,
            supplier.Phone,
            supplier.Email,
            supplier.Address,
            supplier.TaxCode,
            supplier.Note,
            supplier.IsActive
        };

        supplier.Name = request.Name.Trim();
        supplier.ContactPerson = request.ContactPerson?.Trim();
        supplier.Phone = request.Phone.Trim();
        supplier.Email = request.Email?.Trim();
        supplier.Address = request.Address?.Trim();
        supplier.TaxCode = request.TaxCode?.Trim();
        supplier.Note = request.Note?.Trim();
        supplier.IsActive = request.IsActive;
        supplier.UpdatedAt = _timeProvider.GetUtcNow();

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            actorRole,
            "SupplierUpdated",
            "Supplier",
            supplier.Id.ToString(),
            beforeState,
            supplier,
            ipAddress,
            traceId), ct);

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new SupplierDto(
            supplier.Id,
            supplier.Code,
            supplier.Name,
            supplier.ContactPerson,
            supplier.Phone,
            supplier.Email,
            supplier.Address,
            supplier.TaxCode,
            supplier.Note,
            supplier.IsActive,
            supplier.CreatedAt));
    }

    public async Task<Result> DeleteSupplierAsync(
        Guid id,
        Guid? actorUserId,
        string? actorRole,
        string? ipAddress,
        string? traceId,
        CancellationToken ct = default)
    {
        var supplier = await _dbContext.Suppliers
            .FirstOrDefaultAsync(s => s.DeletedAt == null && s.Id == id, ct);

        if (supplier is null)
        {
            return Result.Failure(
                DomainError.NotFound(ErrorCodes.NotFound, $"Supplier {id} was not found."));
        }

        supplier.DeletedAt = _timeProvider.GetUtcNow();
        supplier.IsActive = false;
        supplier.UpdatedAt = _timeProvider.GetUtcNow();

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            actorRole,
            "SupplierDeleted",
            "Supplier",
            supplier.Id.ToString(),
            new { supplier.Id, supplier.Code, supplier.Name },
            new { DeletedAt = supplier.DeletedAt },
            ipAddress,
            traceId), ct);

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    #endregion

    #region Purchase Orders

    public async Task<Result<PagedResponse<PurchaseOrderSummaryDto>>> GetPurchaseOrdersAsync(
        string? status,
        Guid? supplierId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.PurchaseOrders
            .AsNoTracking()
            .Include(static p => p.Supplier)
            .Include(static p => p.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (supplierId.HasValue && supplierId.Value != Guid.Empty)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= to.Value);
        }

        int totalCount = await query.CountAsync(ct);

        var orders = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = orders.Select(p => new PurchaseOrderSummaryDto(
            p.Id,
            p.PoNumber,
            p.SupplierId,
            p.Supplier?.Name ?? string.Empty,
            p.Status,
            p.TotalAmount,
            p.Items.Sum(static i => i.OrderedQuantity),
            p.Items.Sum(static i => i.ReceivedQuantity),
            p.CreatedBy,
            p.SubmittedAt,
            p.ClosedAt,
            p.CreatedAt)).ToList();

        return Result.Success(PagedResponse<PurchaseOrderSummaryDto>.Create(dtos, page, pageSize, totalCount));
    }

    public async Task<Result<PurchaseOrderDetailDto>> GetPurchaseOrderByIdAsync(Guid id, CancellationToken ct = default)
    {
        var po = await _dbContext.PurchaseOrders
            .AsNoTracking()
            .Include(static p => p.Supplier)
            .Include(static p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null)
        {
            return Result.Failure<PurchaseOrderDetailDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Purchase order {id} was not found."));
        }

        var productIds = po.Items.Select(static i => i.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var lineDtos = po.Items.Select(i =>
        {
            products.TryGetValue(i.ProductId, out var prod);
            return new PurchaseOrderLineDto(
                i.Id,
                i.ProductId,
                prod?.Sku ?? string.Empty,
                prod?.Name ?? string.Empty,
                prod?.Unit ?? string.Empty,
                i.OrderedQuantity,
                i.ReceivedQuantity,
                i.UnitCost,
                i.Subtotal);
        }).ToList();

        string rowVersionBase64 = po.RowVersion != null && po.RowVersion.Length > 0
            ? Convert.ToBase64String(po.RowVersion)
            : string.Empty;

        return Result.Success(new PurchaseOrderDetailDto(
            po.Id,
            po.PoNumber,
            po.SupplierId,
            po.Supplier?.Name ?? string.Empty,
            po.Status,
            po.TotalAmount,
            po.Note,
            po.CreatedBy,
            po.SubmittedAt,
            po.ClosedAt,
            po.CreatedAt,
            rowVersionBase64,
            lineDtos));
    }

    public async Task<Result<PurchaseOrderDetailDto>> CreatePurchaseOrderAsync(
        CreatePurchaseOrderRequest request,
        Guid createdBy,
        Guid? actorUserId,
        string? actorRole,
        string? ipAddress,
        string? traceId,
        CancellationToken ct = default)
    {
        var supplier = await _dbContext.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId && s.DeletedAt == null && s.IsActive, ct);

        if (supplier is null)
        {
            return Result.Failure<PurchaseOrderDetailDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Supplier {request.SupplierId} not found or inactive."));
        }

        var productIds = request.Items.Select(static i => i.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.DeletedAt == null)
            .ToDictionaryAsync(p => p.Id, ct);

        if (products.Count < productIds.Count)
        {
            return Result.Failure<PurchaseOrderDetailDto>(
                DomainError.Validation(ErrorCodes.ValidationFailed, "One or more products specified in the purchase order do not exist."));
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        string poNumber = $"PO{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = poNumber,
            SupplierId = request.SupplierId,
            Status = PurchaseOrderStatus.Draft,
            Note = request.Note?.Trim(),
            CreatedBy = createdBy,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        long totalAmount = 0;
        foreach (var itemReq in request.Items)
        {
            long subtotal = (long)itemReq.OrderedQuantity * itemReq.UnitCost;
            totalAmount += subtotal;

            var poItem = new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = po.Id,
                ProductId = itemReq.ProductId,
                OrderedQuantity = itemReq.OrderedQuantity,
                ReceivedQuantity = 0,
                UnitCost = itemReq.UnitCost,
                Subtotal = subtotal,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };

            po.Items.Add(poItem);
        }

        po.TotalAmount = totalAmount;
        _dbContext.PurchaseOrders.Add(po);

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            actorRole,
            "PurchaseOrderCreated",
            "PurchaseOrder",
            po.Id.ToString(),
            null,
            new { po.Id, po.PoNumber, po.SupplierId, po.TotalAmount, ItemsCount = po.Items.Count },
            ipAddress,
            traceId), ct);

        await _dbContext.SaveChangesAsync(ct);

        return await GetPurchaseOrderByIdAsync(po.Id, ct);
    }

    public async Task<Result<PurchaseOrderDetailDto>> UpdatePurchaseOrderAsync(
        Guid id,
        UpdatePurchaseOrderRequest request,
        Guid? actorUserId,
        string? actorRole,
        string? ipAddress,
        string? traceId,
        CancellationToken ct = default)
    {
        var po = await _dbContext.PurchaseOrders
            .Include(static p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null)
        {
            return Result.Failure<PurchaseOrderDetailDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Purchase order {id} was not found."));
        }

        if (!string.Equals(po.Status, PurchaseOrderStatus.Draft, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<PurchaseOrderDetailDto>(
                DomainError.Conflict(ErrorCodes.Conflict, $"Only purchase orders in Draft state can be updated. Current state: '{po.Status}'."));
        }

        var productIds = request.Items.Select(static i => i.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.DeletedAt == null)
            .ToDictionaryAsync(p => p.Id, ct);

        if (products.Count < productIds.Count)
        {
            return Result.Failure<PurchaseOrderDetailDto>(
                DomainError.Validation(ErrorCodes.ValidationFailed, "One or more products specified in the purchase order do not exist."));
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        _dbContext.PurchaseOrderItems.RemoveRange(po.Items);
        po.Items.Clear();

        long totalAmount = 0;
        foreach (var itemReq in request.Items)
        {
            long subtotal = (long)itemReq.OrderedQuantity * itemReq.UnitCost;
            totalAmount += subtotal;

            var poItem = new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = po.Id,
                ProductId = itemReq.ProductId,
                OrderedQuantity = itemReq.OrderedQuantity,
                ReceivedQuantity = 0,
                UnitCost = itemReq.UnitCost,
                Subtotal = subtotal,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };

            po.Items.Add(poItem);
        }

        po.TotalAmount = totalAmount;
        po.Note = request.Note?.Trim();
        po.UpdatedAt = utcNow;

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            actorRole,
            "PurchaseOrderUpdated",
            "PurchaseOrder",
            po.Id.ToString(),
            null,
            new { po.Id, po.PoNumber, po.TotalAmount, ItemsCount = po.Items.Count },
            ipAddress,
            traceId), ct);

        await _dbContext.SaveChangesAsync(ct);

        return await GetPurchaseOrderByIdAsync(po.Id, ct);
    }

    public async Task<Result> SubmitPurchaseOrderAsync(
        Guid id,
        Guid? actorUserId,
        string? actorRole,
        string? ipAddress,
        string? traceId,
        CancellationToken ct = default)
    {
        var po = await _dbContext.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null)
        {
            return Result.Failure(
                DomainError.NotFound(ErrorCodes.NotFound, $"Purchase order {id} was not found."));
        }

        if (!PurchaseOrderStateMachine.CanTransition(po.Status, PurchaseOrderStates.Submitted))
        {
            return Result.Failure(
                DomainError.Conflict(
                    ErrorCodes.PoInvalidState,
                    $"Cannot submit purchase order from state '{po.Status}'."));
        }

        string oldStatus = po.Status;
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        po.Status = PurchaseOrderStatus.Submitted;
        po.SubmittedAt = utcNow;
        po.UpdatedAt = utcNow;

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            actorRole,
            "PurchaseOrderSubmitted",
            "PurchaseOrder",
            po.Id.ToString(),
            new { Status = oldStatus },
            new { po.Status, po.SubmittedAt },
            ipAddress,
            traceId), ct);

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> CancelPurchaseOrderAsync(
        Guid id,
        string? reason,
        Guid? actorUserId,
        string? actorRole,
        string? ipAddress,
        string? traceId,
        CancellationToken ct = default)
    {
        var po = await _dbContext.PurchaseOrders
            .Include(static p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null)
        {
            return Result.Failure(
                DomainError.NotFound(ErrorCodes.NotFound, $"Purchase order {id} was not found."));
        }

        int totalReceived = po.Items.Sum(static i => i.ReceivedQuantity);
        if (!PurchaseOrderStateMachine.CanTransition(po.Status, PurchaseOrderStates.Cancelled, totalReceived))
        {
            return Result.Failure(
                DomainError.Conflict(
                    ErrorCodes.PoInvalidState,
                    $"Cannot cancel purchase order in state '{po.Status}' with {totalReceived} items received."));
        }

        string oldStatus = po.Status;
        po.Status = PurchaseOrderStatus.Cancelled;
        po.UpdatedAt = _timeProvider.GetUtcNow();

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            actorRole,
            "PurchaseOrderCancelled",
            "PurchaseOrder",
            po.Id.ToString(),
            new { Status = oldStatus },
            new { po.Status, Reason = reason },
            ipAddress,
            traceId), ct);

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<GoodsReceiptIntakeResult>> ReceivePurchaseOrderAsync(
        Guid id,
        ReceivePurchaseOrderRequest request,
        Guid receivedBy,
        Guid? actorUserId,
        string? actorRole,
        string? ipAddress,
        string? traceId,
        CancellationToken ct = default)
    {
        var po = await _dbContext.PurchaseOrders
            .Include(static p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null)
        {
            return Result.Failure<GoodsReceiptIntakeResult>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Purchase order {id} was not found."));
        }

        if (!PurchaseOrderStates.IsSubmittedOrOrdered(po.Status) &&
            !string.Equals(po.Status, PurchaseOrderStatus.PartiallyReceived, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<GoodsReceiptIntakeResult>(
                DomainError.Conflict(
                    ErrorCodes.PoInvalidState,
                    $"Cannot receive goods for purchase order in state '{po.Status}'. Must be Submitted or PartiallyReceived."));
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        string receiptNumber = string.IsNullOrWhiteSpace(request.ReceiptNumber)
            ? $"GR{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}"
            : request.ReceiptNumber.Trim();

        var intakeLines = new List<GoodsReceiptLineInput>();
        foreach (var itemReq in request.Items)
        {
            Guid? poItemId = itemReq.PurchaseOrderItemId;
            if (!poItemId.HasValue || poItemId.Value == Guid.Empty)
            {
                var matchingItem = po.Items.FirstOrDefault(i => i.ProductId == itemReq.ProductId);
                poItemId = matchingItem?.Id;
            }

            intakeLines.Add(new GoodsReceiptLineInput(
                itemReq.ProductId,
                itemReq.ReceivedQuantity,
                itemReq.UnitCost,
                poItemId));
        }

        var intakeCommand = new GoodsReceiptIntakeCommand(
            receiptNumber,
            receivedBy,
            intakeLines,
            id,
            request.Note);

        var result = await _purchasingPersistenceService.ExecuteGoodsReceiptIntakeAsync(intakeCommand, ct);
        if (result.IsFailure)
        {
            return result;
        }

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            actorRole,
            "PurchaseOrderReceived",
            "PurchaseOrder",
            id.ToString(),
            new { po.Status },
            new { GoodsReceiptId = result.Value.GoodsReceiptId, result.Value.ReceiptNumber, NewStatus = result.Value.PurchaseOrderStatus },
            ipAddress,
            traceId), ct);

        return result;
    }

    public async Task<Result> ClosePurchaseOrderAsync(
        Guid id,
        Guid? actorUserId,
        string? actorRole,
        string? ipAddress,
        string? traceId,
        CancellationToken ct = default)
    {
        var po = await _dbContext.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null)
        {
            return Result.Failure(
                DomainError.NotFound(ErrorCodes.NotFound, $"Purchase order {id} was not found."));
        }

        if (!PurchaseOrderStateMachine.CanTransition(po.Status, PurchaseOrderStates.Closed))
        {
            return Result.Failure(
                DomainError.Conflict(
                    ErrorCodes.PoInvalidState,
                    $"Cannot close purchase order in state '{po.Status}'. Only PartiallyReceived or Received orders can be closed."));
        }

        string oldStatus = po.Status;
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        po.Status = PurchaseOrderStatus.Closed;
        po.ClosedAt = utcNow;
        po.UpdatedAt = utcNow;

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            actorRole,
            "PurchaseOrderClosed",
            "PurchaseOrder",
            po.Id.ToString(),
            new { Status = oldStatus },
            new { po.Status, po.ClosedAt },
            ipAddress,
            traceId), ct);

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    #endregion

    #region Goods Receipts Queries

    public async Task<Result<PagedResponse<GoodsReceiptSummaryDto>>> GetGoodsReceiptsAsync(
        Guid? purchaseOrderId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.GoodsReceipts
            .AsNoTracking()
            .Include(static g => g.PurchaseOrder)
            .Include(static g => g.Items)
            .AsQueryable();

        if (purchaseOrderId.HasValue && purchaseOrderId.Value != Guid.Empty)
        {
            query = query.Where(g => g.PurchaseOrderId == purchaseOrderId.Value);
        }

        int totalCount = await query.CountAsync(ct);

        var receipts = await query
            .OrderByDescending(g => g.ReceivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = receipts.Select(g => new GoodsReceiptSummaryDto(
            g.Id,
            g.ReceiptNumber,
            g.PurchaseOrderId,
            g.PurchaseOrder?.PoNumber,
            g.ReceivedBy,
            g.ReceivedAt,
            g.Items.Sum(static i => i.ReceivedQuantity),
            g.Note,
            g.CreatedAt)).ToList();

        return Result.Success(PagedResponse<GoodsReceiptSummaryDto>.Create(dtos, page, pageSize, totalCount));
    }

    public async Task<Result<GoodsReceiptDetailDto>> GetGoodsReceiptByIdAsync(Guid id, CancellationToken ct = default)
    {
        var receipt = await _dbContext.GoodsReceipts
            .AsNoTracking()
            .Include(static g => g.PurchaseOrder)
            .Include(static g => g.Items)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (receipt is null)
        {
            return Result.Failure<GoodsReceiptDetailDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Goods receipt {id} was not found."));
        }

        var productIds = receipt.Items.Select(static i => i.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var lineDtos = receipt.Items.Select(i =>
        {
            products.TryGetValue(i.ProductId, out var prod);
            return new GoodsReceiptLineDto(
                i.Id,
                i.ProductId,
                prod?.Sku ?? string.Empty,
                prod?.Name ?? string.Empty,
                prod?.Unit ?? string.Empty,
                i.ReceivedQuantity,
                i.UnitCost,
                i.PurchaseOrderItemId);
        }).ToList();

        return Result.Success(new GoodsReceiptDetailDto(
            receipt.Id,
            receipt.ReceiptNumber,
            receipt.PurchaseOrderId,
            receipt.PurchaseOrder?.PoNumber,
            receipt.ReceivedBy,
            receipt.ReceivedAt,
            receipt.Note,
            receipt.CreatedAt,
            lineDtos));
    }

    public async Task<Result<GoodsReceiptDetailDto>> CreateGoodsReceiptAsync(
        CreateGoodsReceiptRequest request,
        Guid receivedBy,
        Guid? actorUserId,
        string? actorRole,
        string? ipAddress,
        string? traceId,
        CancellationToken ct = default)
    {
        var supplier = await _dbContext.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId && s.DeletedAt == null && s.IsActive, ct);

        if (supplier is null)
        {
            return Result.Failure<GoodsReceiptDetailDto>(
                DomainError.NotFound(ErrorCodes.NotFound, $"Supplier {request.SupplierId} not found or inactive."));
        }

        PurchaseOrder? po = null;
        if (request.PurchaseOrderId.HasValue && request.PurchaseOrderId.Value != Guid.Empty)
        {
            po = await _dbContext.PurchaseOrders
                .Include(static p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId.Value, ct);

            if (po is null)
            {
                return Result.Failure<GoodsReceiptDetailDto>(
                    DomainError.NotFound(ErrorCodes.NotFound, $"Purchase order {request.PurchaseOrderId.Value} not found."));
            }

            if (!PurchaseOrderStates.IsSubmittedOrOrdered(po.Status) &&
                !string.Equals(po.Status, PurchaseOrderStatus.PartiallyReceived, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<GoodsReceiptDetailDto>(
                    DomainError.Conflict(
                        ErrorCodes.PoInvalidState,
                        $"Cannot receive goods for purchase order in state '{po.Status}'. Must be Submitted or PartiallyReceived."));
            }
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        string receiptNumber = string.IsNullOrWhiteSpace(request.InvoiceNo)
            ? $"GR{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}"
            : request.InvoiceNo.Trim();

        var intakeLines = new List<GoodsReceiptLineInput>();
        foreach (var line in request.Lines)
        {
            Guid? poItemId = line.PurchaseOrderItemId;
            if (po != null && (!poItemId.HasValue || poItemId.Value == Guid.Empty))
            {
                var matchingItem = po.Items.FirstOrDefault(i => i.ProductId == line.ProductId);
                poItemId = matchingItem?.Id;
            }

            intakeLines.Add(new GoodsReceiptLineInput(
                line.ProductId,
                line.Quantity,
                line.UnitCost,
                poItemId));
        }

        var intakeCommand = new GoodsReceiptIntakeCommand(
            receiptNumber,
            receivedBy,
            intakeLines,
            request.PurchaseOrderId,
            request.Note);

        var result = await _purchasingPersistenceService.ExecuteGoodsReceiptIntakeAsync(intakeCommand, ct);
        if (result.IsFailure)
        {
            return Result.Failure<GoodsReceiptDetailDto>(result.Error);
        }

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            actorRole,
            "GoodsReceiptCreated",
            "GoodsReceipt",
            result.Value.GoodsReceiptId.ToString(),
            null,
            new { result.Value.GoodsReceiptId, result.Value.ReceiptNumber, request.PurchaseOrderId, LinesCount = request.Lines.Count },
            ipAddress,
            traceId), ct);

        return await GetGoodsReceiptByIdAsync(result.Value.GoodsReceiptId, ct);
    }

    #endregion
}
