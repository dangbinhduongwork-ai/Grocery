using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GroceryHub.Modules.Ordering.Application.DTOs;
using GroceryHub.Modules.Ordering.Domain;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.Persistence.Services;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Ordering.Application.Services;

public sealed class OrderingService
{
    private const long DefaultCodMaxAmount = 2_000_000L;
    private const long DefaultFreeShippingThreshold = 300_000L;
    private const int DefaultMaxQuantityPerLine = 20;

    private readonly GroceryHubDbContext _dbContext;
    private readonly IStockPersistenceService _stockPersistenceService;
    private readonly IOutboxPublisher _outboxPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly IAuditLogPort _auditLogPort;
    private readonly ICartService _cartService;

    public OrderingService(
        GroceryHubDbContext dbContext,
        IStockPersistenceService stockPersistenceService,
        IOutboxPublisher outboxPublisher,
        TimeProvider timeProvider,
        IAuditLogPort auditLogPort,
        ICartService cartService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _stockPersistenceService = stockPersistenceService ?? throw new ArgumentNullException(nameof(stockPersistenceService));
        _outboxPublisher = outboxPublisher ?? throw new ArgumentNullException(nameof(outboxPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _auditLogPort = auditLogPort ?? throw new ArgumentNullException(nameof(auditLogPort));
        _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
    }

    #region Shipping Quotes

    public async Task<Result<ShippingQuoteResponse>> GetShippingQuoteAsync(
        ShippingQuoteRequest request,
        Guid? currentUserId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? provinceCode = request.ProvinceCode;

        if (request.AddressId.HasValue)
        {
            var address = await _dbContext.Addresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.AddressId.Value && a.DeletedAt == null, ct);

            if (address is null)
            {
                return Result.Failure<ShippingQuoteResponse>(DomainError.NotFound("Address not found."));
            }

            if (currentUserId.HasValue && address.UserId != currentUserId.Value)
            {
                return Result.Failure<ShippingQuoteResponse>(DomainError.Forbidden(
                    ErrorCodes.Forbidden,
                    "Address does not belong to the current user."));
            }

            provinceCode = address.ProvinceCode;
        }

        if (string.IsNullOrWhiteSpace(provinceCode))
        {
            return Result.Failure<ShippingQuoteResponse>(DomainError.Validation(
                ErrorCodes.ValidationFailed,
                "Province code is required to calculate shipping."));
        }

        var province = await _dbContext.Provinces
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == provinceCode, ct);

        if (province is null)
        {
            return Result.Failure<ShippingQuoteResponse>(DomainError.NotFound($"Province '{provinceCode}' not found."));
        }

        var zoneMapping = await _dbContext.ShippingZoneProvinces
            .AsNoTracking()
            .Include(szp => szp.Zone)
            .ThenInclude(z => z!.ShippingRates)
            .FirstOrDefaultAsync(szp => szp.ProvinceCode == provinceCode && szp.Zone != null && szp.Zone.IsActive, ct);

        if (zoneMapping?.Zone is null)
        {
            return Result.Failure<ShippingQuoteResponse>(DomainError.Validation(
                ErrorCodes.AddressNotServiceable,
                $"Province '{province.FullName}' is not currently in a serviceable shipping zone."));
        }

        var rate = zoneMapping.Zone.ShippingRates.FirstOrDefault(r => r.IsActive);
        long baseFee = rate?.BaseFee ?? 25_000L;
        long freeThreshold = rate?.FreeShippingThreshold ?? DefaultFreeShippingThreshold;

        long fee = request.Subtotal >= freeThreshold ? 0L : baseFee;
        long remaining = Math.Max(0, freeThreshold - request.Subtotal);

        return Result.Success(new ShippingQuoteResponse(
            province.Code,
            province.FullName,
            Serviceable: true,
            zoneMapping.Zone.Name,
            fee,
            freeThreshold,
            remaining));
    }

    #endregion

    #region Checkout Quote

    public async Task<Result<CheckoutQuoteDto>> GetCheckoutQuoteAsync(
        Guid userId,
        CheckoutQuoteRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Load user cart
        var cart = await _dbContext.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is null || cart.Items.Count == 0)
        {
            return Result.Failure<CheckoutQuoteDto>(DomainError.Validation(
                ErrorCodes.CartEmpty,
                "Your shopping cart is empty."));
        }

        // 2. Validate address
        var address = await _dbContext.Addresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.DeletedAt == null, ct);

        if (address is null)
        {
            return Result.Failure<CheckoutQuoteDto>(DomainError.NotFound("Selected delivery address was not found."));
        }

        if (address.UserId != userId)
        {
            return Result.Failure<CheckoutQuoteDto>(DomainError.Forbidden(
                ErrorCodes.Forbidden,
                "Delivery address does not belong to the current user."));
        }

        // 3. Compute shipping
        var shippingQuoteResult = await GetShippingQuoteAsync(
            new ShippingQuoteRequest(address.Id, address.ProvinceCode, 0),
            userId,
            ct);

        if (!shippingQuoteResult.IsSuccess)
        {
            return Result.Failure<CheckoutQuoteDto>(shippingQuoteResult.Error);
        }

        // 4. Validate products and live prices
        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var stockItems = await _dbContext.StockItems
            .AsNoTracking()
            .Where(s => productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, ct);

        List<CartItemEvaluationDto> evalItems = [];
        List<string> warnings = [];

        long subtotal = 0;

        foreach (var item in cart.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product) || product.DeletedAt is not null || product.Status != "Active")
            {
                return Result.Failure<CheckoutQuoteDto>(DomainError.Conflict(
                    ErrorCodes.ProductUnavailable,
                    $"Product '{product?.Name ?? item.ProductId.ToString()}' is no longer available."));
            }

            StockItem? stock = stockItems.GetValueOrDefault(item.ProductId);
            int available = stock is not null ? Math.Max(0, stock.OnHand - stock.Reserved) : 0;

            if (item.Quantity > available)
            {
                return Result.Failure<CheckoutQuoteDto>(DomainError.Conflict(
                    ErrorCodes.InsufficientStock,
                    $"Insufficient stock for '{product.Name}'. Only {available} available (requested: {item.Quantity})."));
            }

            long lineSubtotal = product.SellingPrice * item.Quantity;
            subtotal += lineSubtotal;

            evalItems.Add(new CartItemEvaluationDto(
                product.Id,
                product.CategoryId,
                product.SellingPrice,
                item.Quantity,
                lineSubtotal));
        }

        // Recompute shipping fee based on live subtotal
        long freeThreshold = shippingQuoteResult.Value.FreeShippingThreshold ?? DefaultFreeShippingThreshold;
        long shippingFee = subtotal >= freeThreshold ? 0L : shippingQuoteResult.Value.Fee;

        // 5. Coupon calculation
        long couponDiscount = 0;
        string? activeCouponCode = null;
        Dictionary<Guid, long> lineAllocations = evalItems.ToDictionary(i => i.ProductId, _ => 0L);

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            string normalizedCode = request.CouponCode.Trim().ToUpperInvariant();
            var coupon = await _dbContext.Coupons
                .AsNoTracking()
                .Include(c => c.Scopes)
                .FirstOrDefaultAsync(c => c.Code == normalizedCode, ct);

            if (coupon is null || !coupon.IsActive)
            {
                return Result.Failure<CheckoutQuoteDto>(DomainError.Validation(
                    ErrorCodes.CouponInvalid,
                    $"Coupon '{normalizedCode}' is invalid or inactive."));
            }

            int userRedemptions = await _dbContext.CouponRedemptions
                .AsNoTracking()
                .CountAsync(r => r.CouponId == coupon.Id && r.UserId == userId && r.ReleasedAt == null, ct);

            DateTimeOffset now = _timeProvider.GetUtcNow();
            var evalResult = CouponValidationEngine.Evaluate(
                ToRuleInput(coupon),
                userId,
                subtotal,
                shippingFee,
                evalItems,
                now,
                userRedemptions);

            if (!evalResult.IsSuccess)
            {
                return Result.Failure<CheckoutQuoteDto>(evalResult.Error);
            }

            activeCouponCode = normalizedCode;
            couponDiscount = evalResult.Value.DiscountAmount;
            foreach (var kvp in evalResult.Value.LineDiscountAllocations)
            {
                lineAllocations[kvp.Key] = kvp.Value;
            }
        }

        long total = Math.Max(0, subtotal - couponDiscount + shippingFee);

        // 6. Payment methods check
        long codMaxAmount = DefaultCodMaxAmount;
        List<string> availablePaymentMethods = [];
        if (total <= codMaxAmount)
        {
            availablePaymentMethods.Add("COD");
        }
        else
        {
            warnings.Add($"Order total exceeds maximum COD limit ({codMaxAmount:N0} ₫).");
        }

        if (string.Equals(request.PaymentMethod, "SEPAY_QR", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<CheckoutQuoteDto>(DomainError.Conflict(
                ErrorCodes.PaymentMethodUnavailable,
                "VietQR (SePay) payment method is temporarily unavailable. Please choose COD."));
        }

        if (string.Equals(request.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase) && total > codMaxAmount)
        {
            return Result.Failure<CheckoutQuoteDto>(DomainError.Validation(
                ErrorCodes.CodLimitExceeded,
                $"Order total ({total:N0} ₫) exceeds COD limit ({codMaxAmount:N0} ₫)."));
        }

        // Build item lines
        List<CheckoutQuoteItemDto> quoteItems = [];
        foreach (var eval in evalItems)
        {
            var product = products[eval.ProductId];
            long lineDisc = lineAllocations.GetValueOrDefault(eval.ProductId, 0L);
            long lineTotal = Math.Max(0, eval.LineSubtotal - lineDisc);

            quoteItems.Add(new CheckoutQuoteItemDto(
                product.Id,
                product.Sku,
                product.Name,
                product.Unit,
                product.SellingPrice,
                product.ListPrice,
                eval.Quantity,
                eval.LineSubtotal,
                lineDisc,
                lineTotal));
        }

        return Result.Success(new CheckoutQuoteDto(
            subtotal,
            activeCouponCode,
            couponDiscount,
            shippingFee,
            total,
            codMaxAmount,
            availablePaymentMethods,
            quoteItems,
            warnings));
    }

    #endregion

    #region Order Placement

    public async Task<Result<PlaceOrderResponseDto>> PlaceOrderAsync(
        Guid userId,
        string idempotencyKey,
        PlaceOrderRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result.Failure<PlaceOrderResponseDto>(DomainError.Validation(
                ErrorCodes.ValidationFailed,
                "Idempotency-Key header is required."));
        }

        string scope = $"checkout.place_order:{userId}";
        string requestJson = JsonSerializer.Serialize(request);
        string requestHash = ComputeSha256(requestJson);

        // Check idempotency record
        var existingRecord = await _dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == idempotencyKey && r.Scope == scope, ct);

        if (existingRecord is not null)
        {
            if (string.Equals(existingRecord.RequestHash, requestHash, StringComparison.Ordinal))
            {
                var cachedResponse = JsonSerializer.Deserialize<PlaceOrderResponseDto>(existingRecord.ResponseBody);
                if (cachedResponse is not null)
                {
                    return Result.Success(cachedResponse);
                }
            }
            else
            {
                return Result.Failure<PlaceOrderResponseDto>(DomainError.Conflict(
                    ErrorCodes.IdempotencyKeyReused,
                    "Idempotency key was reused with a different request payload."));
            }
        }

        if (string.Equals(request.PaymentMethod, "SEPAY_QR", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<PlaceOrderResponseDto>(DomainError.Conflict(
                ErrorCodes.PaymentMethodUnavailable,
                "VietQR (SePay) payment method is temporarily unavailable. Please choose COD."));
        }

        if (!string.Equals(request.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<PlaceOrderResponseDto>(DomainError.Validation(
                ErrorCodes.PaymentMethodUnavailable,
                $"Payment method '{request.PaymentMethod}' is not supported."));
        }

        // Get checkout quote for authoritative server totals and validations
        var quoteResult = await GetCheckoutQuoteAsync(userId, new CheckoutQuoteRequest(
            request.AddressId,
            request.CouponCode,
            request.PaymentMethod), ct);

        if (!quoteResult.IsSuccess)
        {
            return Result.Failure<PlaceOrderResponseDto>(quoteResult.Error);
        }

        var quote = quoteResult.Value;
        if (quote.Total > quote.CodMaxAmount)
        {
            return Result.Failure<PlaceOrderResponseDto>(DomainError.Validation(
                ErrorCodes.CodLimitExceeded,
                $"Order total ({quote.Total:N0} ₫) exceeds COD limit ({quote.CodMaxAmount:N0} ₫)."));
        }

        var address = await _dbContext.Addresses
            .AsNoTracking()
            .FirstAsync(a => a.Id == request.AddressId, ct);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        Guid orderId = Guid.NewGuid();

        var reservationLines = quote.Items
            .Select(i => (ProductId: i.ProductId, Quantity: i.Quantity))
            .ToList();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            // 1. Reserve stock atomically
            var reserveResult = await _stockPersistenceService.ReserveStockAsync(
                orderId,
                reservationLines,
                now.AddMinutes(30),
                ct);

            if (!reserveResult.IsSuccess)
            {
                await transaction.RollbackAsync(ct);
                return Result.Failure<PlaceOrderResponseDto>(reserveResult.Error);
            }

            // 2. Redeem coupon if applicable
            Coupon? couponEntity = null;
            if (!string.IsNullOrWhiteSpace(quote.CouponCode))
            {
                couponEntity = await _dbContext.Coupons
                    .FirstOrDefaultAsync(c => c.Code == quote.CouponCode, ct);

                if (couponEntity is not null)
                {
                    couponEntity.UsedCount++;
                    couponEntity.UpdatedAt = now;

                    _dbContext.CouponRedemptions.Add(new CouponRedemption
                    {
                        Id = Guid.NewGuid(),
                        CouponId = couponEntity.Id,
                        UserId = userId,
                        OrderId = orderId,
                        Amount = quote.CouponDiscount,
                        RedeemedAt = now,
                        ReleasedAt = null
                    });
                }
            }

            // 3. Sequence numbers
            long orderSeq = await _dbContext.Database
                .SqlQueryRaw<long>("SELECT NEXT VALUE FOR [ordering].[OrderNumberSeq]")
                .SingleAsync(ct);
            string orderNumber = $"ORD-{now:yyyyMMdd}-{orderSeq:D6}";

            long paymentSeq = await _dbContext.Database
                .SqlQueryRaw<long>("SELECT NEXT VALUE FOR [payments].[PaymentCodeSeq]")
                .SingleAsync(ct);
            string paymentCode = $"GH{paymentSeq:D10}";

            // 4. Create Order
            Order order = new()
            {
                Id = orderId,
                OrderNumber = orderNumber,
                UserId = userId,
                Status = OrderStatusConstants.Placed,
                Subtotal = quote.Subtotal,
                CouponDiscount = quote.CouponDiscount,
                ShippingFee = quote.ShippingFee,
                Total = quote.Total,
                PaymentMethod = OrderPaymentMethodConstants.COD,
                PaymentStatus = OrderPaymentStatusConstants.Unpaid,
                ShippingRecipientName = address.RecipientName,
                ShippingPhone = address.Phone,
                ShippingProvinceCode = address.ProvinceCode,
                ShippingWardCode = address.WardCode,
                ShippingProvinceName = address.ProvinceName,
                ShippingWardName = address.WardName,
                ShippingStreet = address.Street,
                ShippingNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            foreach (var qItem in quote.Items)
            {
                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = qItem.ProductId,
                    ProductName = qItem.Name,
                    ProductSku = qItem.Sku,
                    ProductUnit = qItem.Unit,
                    UnitPrice = qItem.UnitPrice,
                    Quantity = qItem.Quantity,
                    Subtotal = qItem.LineSubtotal,
                    DiscountAllocated = qItem.DiscountAllocated,
                    UnitCostSnapshot = 0L,
                    CreatedAt = now
                });
            }

            order.StatusHistory.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                FromStatus = null,
                ToStatus = OrderStatusConstants.Placed,
                Reason = "Order placed by customer",
                ActorUserId = userId,
                ActorRole = "Customer",
                CreatedAt = now
            });

            _dbContext.Orders.Add(order);

            // 5. Create Payment
            Payment payment = new()
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                PaymentCode = paymentCode,
                Method = PaymentMethodConstants.COD,
                Status = PaymentStatusConstants.Unpaid,
                Amount = quote.Total,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.Payments.Add(payment);

            // 6. Clear user cart
            var userCart = await _dbContext.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, ct);

            if (userCart is not null)
            {
                _dbContext.CartItems.RemoveRange(userCart.Items);
                userCart.Items.Clear();
                userCart.UpdatedAt = now;
            }

            // 7. Prepare response
            OrderSummaryDto orderSummary = new(
                order.Id,
                order.OrderNumber,
                order.Status,
                order.PaymentMethod,
                order.PaymentStatus,
                order.Subtotal,
                quote.CouponCode,
                order.CouponDiscount,
                order.ShippingFee,
                order.Total,
                order.CreatedAt,
                null);

            OrderPaymentViewDto paymentView = new(
                payment.Id,
                payment.Status,
                payment.Method,
                payment.PaymentCode,
                AttemptNumber: 1,
                MaxAttempts: 1,
                ExpiresAt: null,
                Qr: null);

            PlaceOrderResponseDto responseDto = new(orderSummary, paymentView);

            // 8. Save Idempotency record
            _dbContext.IdempotencyRecords.Add(new IdempotencyRecord
            {
                Id = Guid.NewGuid(),
                Key = idempotencyKey,
                Scope = scope,
                UserId = userId,
                RequestHash = requestHash,
                ResponseStatus = 201,
                ResponseContentType = "application/json",
                ResponseBody = JsonSerializer.Serialize(responseDto),
                CreatedAt = now,
                ExpiresAt = now.AddDays(1)
            });

            // 9. Outbox event
            var eventItems = quote.Items.Select(i => new OrderPlacedLineItem(
                i.ProductId,
                i.Sku,
                i.Quantity,
                i.UnitPrice,
                i.DiscountAllocated,
                i.LineTotal)).ToList();

            _outboxPublisher.Enqueue(new OrderPlacedEvent(
                Guid.NewGuid(),
                orderId,
                orderNumber,
                userId,
                "COD",
                "Placed",
                order.Subtotal,
                quote.CouponCode,
                order.CouponDiscount,
                order.ShippingFee,
                order.Total,
                eventItems,
                now));

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Result.Success(responseDto);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    #endregion

    #region Customer Orders Operations

    public async Task<Result<PagedResponse<CustomerOrderListItemDto>>> GetCustomerOrdersAsync(
        Guid userId,
        string? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (from.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= to.Value);
        }

        int totalItems = await query.CountAsync(ct);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        List<CustomerOrderListItemDto> items = [];
        foreach (var o in orders)
        {
            var firstItem = o.Items.FirstOrDefault();
            items.Add(new CustomerOrderListItemDto(
                o.Id,
                o.OrderNumber,
                o.Status,
                o.PaymentMethod,
                o.PaymentStatus,
                o.Total,
                o.Items.Sum(i => i.Quantity),
                firstItem?.ProductName ?? "Grocery Items",
                null,
                o.CreatedAt,
                null));
        }

        return Result.Success(PagedResponse<CustomerOrderListItemDto>.Create(items, page, pageSize, totalItems));
    }

    public async Task<Result<CustomerOrderDetailDto>> GetCustomerOrderByIdAsync(
        Guid userId,
        Guid orderId,
        CancellationToken ct = default)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return Result.Failure<CustomerOrderDetailDto>(DomainError.NotFound("Order not found."));
        }

        if (order.UserId != userId)
        {
            return Result.Failure<CustomerOrderDetailDto>(DomainError.Forbidden(
                ErrorCodes.Forbidden,
                "Order does not belong to the current user."));
        }

        var payment = await _dbContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

        return Result.Success(MapToCustomerOrderDetailDto(order, payment));
    }

    public async Task<Result<CustomerOrderDetailDto>> CustomerCancelOrderAsync(
        Guid userId,
        Guid orderId,
        string reason,
        CancellationToken ct = default)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return Result.Failure<CustomerOrderDetailDto>(DomainError.NotFound("Order not found."));
        }

        if (order.UserId != userId)
        {
            return Result.Failure<CustomerOrderDetailDto>(DomainError.Forbidden(
                ErrorCodes.Forbidden,
                "Order does not belong to the current user."));
        }

        if (!OrderStateMachine.CanCustomerCancel(order.Status))
        {
            return Result.Failure<CustomerOrderDetailDto>(DomainError.Conflict(
                ErrorCodes.OrderNotCancellable,
                $"Order in status '{order.Status}' cannot be cancelled by customer."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        string previousStatus = order.Status;
        string previousPaymentStatus = order.PaymentStatus;

        order.Status = OrderStatusConstants.Cancelled;
        order.CancelledAt = now;
        order.CancelledBy = userId;
        order.CancellationReason = reason.Trim();
        order.UpdatedAt = now;

        // Release stock reservation
        await _stockPersistenceService.ReleaseStockReservationAsync(orderId, ct);

        // Release coupon redemption
        var redemption = await _dbContext.CouponRedemptions
            .Include(r => r.Coupon)
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.ReleasedAt == null, ct);

        if (redemption is not null)
        {
            redemption.ReleasedAt = now;
            if (redemption.Coupon is not null && redemption.Coupon.UsedCount > 0)
            {
                redemption.Coupon.UsedCount--;
            }
        }

        // Update payment to expired if unpaid
        var payment = await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

        if (payment is not null && payment.Status == PaymentStatusConstants.Unpaid)
        {
            payment.Status = PaymentStatusConstants.Expired;
            payment.UpdatedAt = now;
            order.PaymentStatus = OrderPaymentStatusConstants.Expired;
        }

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            FromStatus = previousStatus,
            ToStatus = OrderStatusConstants.Cancelled,
            Reason = reason.Trim(),
            ActorUserId = userId,
            ActorRole = "Customer",
            CreatedAt = now
        });

        _outboxPublisher.Enqueue(new OrderCancelledEvent(
            Guid.NewGuid(),
            orderId,
            order.OrderNumber,
            userId,
            previousStatus,
            previousPaymentStatus,
            userId,
            "Customer",
            reason.Trim(),
            StockReturnedFromShipping: false,
            AutoCreatedRefundId: null,
            now,
            now));

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(MapToCustomerOrderDetailDto(order, payment));
    }

    public async Task<Result<ReorderResponseDto>> ReorderAsync(
        Guid userId,
        Guid orderId,
        CancellationToken ct = default)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return Result.Failure<ReorderResponseDto>(DomainError.NotFound("Order not found."));
        }

        if (order.UserId != userId)
        {
            return Result.Failure<ReorderResponseDto>(DomainError.Forbidden(
                ErrorCodes.Forbidden,
                "Order does not belong to the current user."));
        }

        int added = 0;
        List<SkippedReorderItemDto> skipped = [];

        foreach (var item in order.Items)
        {
            var addResult = await _cartService.AddItemAsync(
                userId,
                null,
                new AddCartItemRequest(item.ProductId, item.Quantity),
                ct);

            if (addResult.IsSuccess)
            {
                added++;
            }
            else
            {
                skipped.Add(new SkippedReorderItemDto(item.ProductId, item.ProductName, addResult.Error.Message));
            }
        }

        var cartViewResult = await _cartService.GetOrCreateCartViewAsync(userId, null, ct);
        return Result.Success(new ReorderResponseDto(cartViewResult.Value.CartView, added, skipped));
    }

    #endregion

    #region Admin Orders Operations

    public async Task<Result<PagedResponse<AdminOrderListItemDto>>> GetAdminOrdersAsync(
        string? q,
        string? status,
        string? paymentStatus,
        string? paymentMethod,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Orders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            string s = q.Trim().ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(s) ||
                o.ShippingRecipientName.ToLower().Contains(s) ||
                o.ShippingPhone.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            query = query.Where(o => o.PaymentStatus == paymentStatus);
        }

        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            query = query.Where(o => o.PaymentMethod == paymentMethod);
        }

        if (from.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= to.Value);
        }

        int totalItems = await query.CountAsync(ct);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new AdminOrderListItemDto(
                o.Id,
                o.OrderNumber,
                o.Status,
                o.PaymentMethod,
                o.PaymentStatus,
                o.Total,
                o.ShippingRecipientName,
                o.ShippingPhone,
                o.ShippingProvinceName,
                o.CreatedAt))
            .ToListAsync(ct);

        return Result.Success(PagedResponse<AdminOrderListItemDto>.Create(orders, page, pageSize, totalItems));
    }

    public async Task<Result<AdminOrderDetailDto>> GetAdminOrderByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return Result.Failure<AdminOrderDetailDto>(DomainError.NotFound("Order not found."));
        }

        return Result.Success(MapToAdminOrderDetailDto(order));
    }

    public async Task<Result<AdminOrderDetailDto>> StaffTransitionOrderAsync(
        Guid orderId,
        TransitionOrderRequest request,
        Guid actorId,
        IReadOnlyCollection<string> permissions,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return Result.Failure<AdminOrderDetailDto>(DomainError.NotFound("Order not found."));
        }

        var validationResult = OrderStateMachine.ValidateStaffTransition(
            order.Status,
            request.To,
            order.PaymentStatus,
            permissions,
            request.Reason,
            request.CodAmount,
            order.Total);

        if (!validationResult.IsSuccess)
        {
            return Result.Failure<AdminOrderDetailDto>(validationResult.Error);
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        string previousStatus = order.Status;
        string previousPaymentStatus = order.PaymentStatus;
        string targetStatus = request.To;

        if (string.Equals(targetStatus, OrderStatusConstants.Shipping, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(previousStatus, OrderStatusConstants.Packed, StringComparison.OrdinalIgnoreCase))
        {
            // Commits stock
            var commitResult = await _stockPersistenceService.CommitStockReservationAsync(orderId, ct);
            if (!commitResult.IsSuccess)
            {
                return Result.Failure<AdminOrderDetailDto>(commitResult.Error);
            }

            // Snapshot unit cost
            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var costPrices = await _dbContext.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.CostPrice, ct);

            foreach (var item in order.Items)
            {
                item.UnitCostSnapshot = costPrices.GetValueOrDefault(item.ProductId, 0L);
            }

            order.Status = OrderStatusConstants.Shipping;
            order.UpdatedAt = now;

            order.StatusHistory.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                FromStatus = previousStatus,
                ToStatus = OrderStatusConstants.Shipping,
                Reason = request.Reason ?? "Dispatched for shipping",
                ActorUserId = actorId,
                ActorRole = "Staff",
                CreatedAt = now
            });

            var shippedCostItems = order.Items.Select(i => new OrderShippedCostSnapshotItem(
                i.Id,
                i.ProductId,
                i.Quantity,
                i.UnitCostSnapshot)).ToList();

            _outboxPublisher.Enqueue(new OrderShippedEvent(
                Guid.NewGuid(),
                orderId,
                order.OrderNumber,
                actorId,
                shippedCostItems,
                now,
                now));
        }
        else if (string.Equals(targetStatus, OrderStatusConstants.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(order.PaymentMethod, OrderPaymentMethodConstants.COD, StringComparison.OrdinalIgnoreCase))
            {
                var payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);
                if (payment is not null)
                {
                    payment.Status = PaymentStatusConstants.Paid;
                    payment.PaidAt = now;
                    payment.UpdatedAt = now;

                    _dbContext.CodCollections.Add(new CodCollection
                    {
                        Id = Guid.NewGuid(),
                        PaymentId = payment.Id,
                        OrderId = orderId,
                        CollectedBy = actorId,
                        Amount = request.CodAmount ?? order.Total,
                        CollectedAt = now,
                        Note = request.Reason
                    });
                }

                order.PaymentStatus = OrderPaymentStatusConstants.Paid;
            }

            order.Status = OrderStatusConstants.Delivered;
            order.UpdatedAt = now;

            order.StatusHistory.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                FromStatus = previousStatus,
                ToStatus = OrderStatusConstants.Delivered,
                Reason = request.Reason ?? "Order successfully delivered",
                ActorUserId = actorId,
                ActorRole = "Staff",
                CreatedAt = now
            });

            var soldItems = order.Items.Select(i => new ProductSoldItem(i.ProductId, i.Quantity)).ToList();

            _outboxPublisher.Enqueue(new OrderDeliveredEvent(
                Guid.NewGuid(),
                orderId,
                order.OrderNumber,
                order.UserId,
                actorId,
                order.PaymentMethod,
                order.Total,
                soldItems,
                now,
                now));
        }
        else if (string.Equals(targetStatus, OrderStatusConstants.Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            bool stockReturned = false;
            if (string.Equals(previousStatus, OrderStatusConstants.DeliveryFailed, StringComparison.OrdinalIgnoreCase))
            {
                // Return stock from DeliveryFailed
                var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
                var stockItems = await _dbContext.StockItems
                    .Where(s => productIds.Contains(s.ProductId))
                    .ToDictionaryAsync(s => s.ProductId, ct);

                foreach (var item in order.Items)
                {
                    if (stockItems.TryGetValue(item.ProductId, out var stockItem))
                    {
                        int before = stockItem.OnHand;
                        stockItem.OnHand += item.Quantity;
                        stockItem.UpdatedAt = now;

                        _dbContext.StockMovements.Add(new StockMovement
                        {
                            ProductId = item.ProductId,
                            Type = StockMovementType.Return,
                            Quantity = item.Quantity,
                            OnHandBefore = before,
                            OnHandAfter = before + item.Quantity,
                            UnitCost = item.UnitCostSnapshot,
                            ReferenceType = "Order",
                            ReferenceId = orderId,
                            Reason = "Return from cancelled delivery failure",
                            ActorUserId = actorId,
                            At = now
                        });
                    }
                }
                stockReturned = true;
            }
            else
            {
                await _stockPersistenceService.ReleaseStockReservationAsync(orderId, ct);
            }

            // Release coupon redemption
            var redemption = await _dbContext.CouponRedemptions
                .Include(r => r.Coupon)
                .FirstOrDefaultAsync(r => r.OrderId == orderId && r.ReleasedAt == null, ct);

            if (redemption is not null)
            {
                redemption.ReleasedAt = now;
                if (redemption.Coupon is not null && redemption.Coupon.UsedCount > 0)
                {
                    redemption.Coupon.UsedCount--;
                }
            }

            var payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);
            if (payment is not null && payment.Status == PaymentStatusConstants.Unpaid)
            {
                payment.Status = PaymentStatusConstants.Expired;
                payment.UpdatedAt = now;
                order.PaymentStatus = OrderPaymentStatusConstants.Expired;
            }

            order.Status = OrderStatusConstants.Cancelled;
            order.CancelledAt = now;
            order.CancelledBy = actorId;
            order.CancellationReason = request.Reason;
            order.UpdatedAt = now;

            order.StatusHistory.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                FromStatus = previousStatus,
                ToStatus = OrderStatusConstants.Cancelled,
                Reason = request.Reason,
                ActorUserId = actorId,
                ActorRole = permissions.Contains("orders.cancel") ? "Manager" : "Staff",
                CreatedAt = now
            });

            _outboxPublisher.Enqueue(new OrderCancelledEvent(
                Guid.NewGuid(),
                orderId,
                order.OrderNumber,
                order.UserId,
                previousStatus,
                previousPaymentStatus,
                actorId,
                permissions.Contains("orders.cancel") ? "Manager" : "Staff",
                request.Reason ?? "Cancelled by staff",
                stockReturned,
                null,
                now,
                now));
        }
        else
        {
            // Normal transition
            order.Status = targetStatus;
            order.UpdatedAt = now;

            order.StatusHistory.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                FromStatus = previousStatus,
                ToStatus = targetStatus,
                Reason = request.Reason,
                ActorUserId = actorId,
                ActorRole = "Staff",
                CreatedAt = now
            });

            if (string.Equals(targetStatus, OrderStatusConstants.Confirmed, StringComparison.OrdinalIgnoreCase))
            {
                _outboxPublisher.Enqueue(new OrderConfirmedEvent(
                    Guid.NewGuid(),
                    orderId,
                    order.OrderNumber,
                    actorId,
                    "Staff",
                    now,
                    now));
            }
            else if (string.Equals(targetStatus, OrderStatusConstants.Packed, StringComparison.OrdinalIgnoreCase))
            {
                _outboxPublisher.Enqueue(new OrderPackedEvent(
                    Guid.NewGuid(),
                    orderId,
                    order.OrderNumber,
                    actorId,
                    now,
                    now));
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        return Result.Success(MapToAdminOrderDetailDto(order));
    }

    public async Task<Result<OrderNoteResponseDto>> UpdateInternalNoteAsync(
        Guid orderId,
        string note,
        Guid actorId,
        CancellationToken ct = default)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            return Result.Failure<OrderNoteResponseDto>(DomainError.NotFound("Order not found."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        _dbContext.OrderStatusHistories.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            FromStatus = order.Status,
            ToStatus = order.Status,
            Reason = $"[Ghi chú nội bộ] {note.Trim()}",
            ActorUserId = actorId,
            ActorRole = "Staff",
            CreatedAt = now
        });

        order.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new OrderNoteResponseDto(order.Id, note.Trim()));
    }

    public async Task<Result<AssignOrderResponseDto>> AssignOrderAsync(
        Guid orderId,
        Guid? assignedToUserId,
        Guid actorId,
        CancellationToken ct = default)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            return Result.Failure<AssignOrderResponseDto>(DomainError.NotFound("Order not found."));
        }

        string assignedToName = "Unassigned";
        if (assignedToUserId.HasValue)
        {
            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == assignedToUserId.Value, ct);
            if (user is null)
            {
                return Result.Failure<AssignOrderResponseDto>(DomainError.NotFound("Staff user not found."));
            }
            assignedToName = user.FullName;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        _dbContext.OrderStatusHistories.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            FromStatus = order.Status,
            ToStatus = order.Status,
            Reason = $"[Phân công] {assignedToName}",
            ActorUserId = actorId,
            ActorRole = "Staff",
            CreatedAt = now
        });

        order.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new AssignOrderResponseDto(order.Id, assignedToUserId, assignedToName));
    }

    public async Task<Result<PickListDto>> GetPickListAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return Result.Failure<PickListDto>(DomainError.NotFound("Order not found."));
        }

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var groups = order.Items
            .GroupBy(i => products.TryGetValue(i.ProductId, out var prod) && prod.Category is not null ? prod.Category.Name : "General")
            .Select(g => new PickListCategoryGroupDto(
                g.Key,
                g.Select(i => new PickListItemDto(
                    i.ProductSku,
                    products.TryGetValue(i.ProductId, out var p) ? p.Barcode : null,
                    i.ProductName,
                    i.ProductUnit,
                    i.ProductPackSize,
                    i.Quantity,
                    null)).ToList()))
            .ToList();

        return Result.Success(new PickListDto(order.Id, order.OrderNumber, order.Status, order.ShippingNote, groups));
    }

    public async Task<Result<DeliveryNoteDto>> GetDeliveryNoteAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            return Result.Failure<DeliveryNoteDto>(DomainError.NotFound("Order not found."));
        }

        long codToCollect = string.Equals(order.PaymentMethod, OrderPaymentMethodConstants.COD, StringComparison.OrdinalIgnoreCase) &&
                            order.PaymentStatus != OrderPaymentStatusConstants.Paid
            ? order.Total
            : 0L;

        string fullAddress = $"{order.ShippingStreet}, {order.ShippingWardName}, {order.ShippingProvinceName}";

        var itemViews = order.Items.Select(i => new OrderItemViewDto(
            i.Id,
            i.ProductId,
            i.ProductSku,
            i.ProductName,
            i.ProductUnit,
            i.ProductPackSize,
            i.UnitPrice,
            i.Quantity,
            i.Subtotal,
            i.DiscountAllocated,
            Math.Max(0, i.Subtotal - i.DiscountAllocated),
            null)).ToList();

        return Result.Success(new DeliveryNoteDto(
            order.Id,
            order.OrderNumber,
            order.CreatedAt,
            order.ShippingRecipientName,
            order.ShippingPhone,
            fullAddress,
            order.PaymentMethod,
            codToCollect,
            itemViews,
            order.Subtotal,
            order.CouponDiscount,
            order.ShippingFee,
            order.Total));
    }

    #endregion

    #region Background Sweep

    public async Task<int> SweepExpiredReservationsAsync(CancellationToken ct = default)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        var expiredOrderIds = await _dbContext.StockReservations
            .AsNoTracking()
            .Where(r => r.Status == StockReservationStatus.Active && r.ExpiresAt != null && r.ExpiresAt.Value < now)
            .Select(r => r.OrderId)
            .Distinct()
            .ToListAsync(ct);

        int count = 0;
        foreach (var orderId in expiredOrderIds)
        {
            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order is not null && (order.Status == OrderStatusConstants.PendingPayment || order.Status == OrderStatusConstants.Placed))
            {
                await _stockPersistenceService.ReleaseStockReservationAsync(orderId, ct);

                order.Status = OrderStatusConstants.Cancelled;
                order.CancelledAt = now;
                order.CancellationReason = "Reservation expired";
                order.UpdatedAt = now;

                var payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);
                if (payment is not null && payment.Status == PaymentStatusConstants.Unpaid)
                {
                    payment.Status = PaymentStatusConstants.Expired;
                    payment.UpdatedAt = now;
                    order.PaymentStatus = OrderPaymentStatusConstants.Expired;
                }

                await _dbContext.SaveChangesAsync(ct);
                count++;
            }
        }

        return count;
    }

    #endregion

    #region Shipping Zones & Rates Management

    public async Task<Result<IReadOnlyList<ShippingZoneDto>>> GetShippingZonesAsync(CancellationToken ct = default)
    {
        var zones = await _dbContext.ShippingZones
            .AsNoTracking()
            .Include(z => z.ServiceableProvinces)
            .Include(z => z.ShippingRates)
            .ToListAsync(ct);

        var dtos = zones.Select(z => new ShippingZoneDto(
            z.Id,
            z.Name,
            z.IsActive,
            z.ServiceableProvinces.Select(p => p.ProvinceCode).ToList(),
            z.ShippingRates.Select(r => new ShippingRateDto(
                r.Id,
                r.BaseFee,
                r.FreeShippingThreshold,
                r.EstimatedDaysMin,
                r.EstimatedDaysMax,
                r.IsActive)).ToList())).ToList();

        return Result.Success<IReadOnlyList<ShippingZoneDto>>(dtos);
    }

    public async Task<Result<ShippingZoneDto>> CreateShippingZoneAsync(
        CreateShippingZoneRequest request,
        Guid actorId,
        CancellationToken ct = default)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        ShippingZone zone = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var code in request.ProvinceCodes)
        {
            zone.ServiceableProvinces.Add(new ShippingZoneProvince
            {
                ZoneId = zone.Id,
                ProvinceCode = code.Trim(),
                CreatedAt = now
            });
        }

        foreach (var rateInput in request.Rates)
        {
            zone.ShippingRates.Add(new ShippingRate
            {
                Id = Guid.NewGuid(),
                ZoneId = zone.Id,
                BaseFee = rateInput.BaseFee,
                FreeShippingThreshold = rateInput.FreeShippingThreshold,
                EstimatedDaysMin = rateInput.EstimatedDaysMin,
                EstimatedDaysMax = rateInput.EstimatedDaysMax,
                IsActive = rateInput.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        _dbContext.ShippingZones.Add(zone);
        await _dbContext.SaveChangesAsync(ct);

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorId,
            "Admin",
            "ShippingZoneCreated",
            "ShippingZone",
            zone.Id.ToString(),
            null,
            new { zone.Name, request.ProvinceCodes }), ct);

        return await GetShippingZoneByIdAsync(zone.Id, ct);
    }

    public async Task<Result<ShippingZoneDto>> UpdateShippingZoneAsync(
        Guid id,
        UpdateShippingZoneRequest request,
        Guid actorId,
        CancellationToken ct = default)
    {
        var zone = await _dbContext.ShippingZones
            .Include(z => z.ServiceableProvinces)
            .Include(z => z.ShippingRates)
            .FirstOrDefaultAsync(z => z.Id == id, ct);

        if (zone is null)
        {
            return Result.Failure<ShippingZoneDto>(DomainError.NotFound("Shipping zone not found."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        zone.Name = request.Name.Trim();
        zone.IsActive = request.IsActive;
        zone.UpdatedAt = now;

        // Replace provinces
        _dbContext.ShippingZoneProvinces.RemoveRange(zone.ServiceableProvinces);
        zone.ServiceableProvinces.Clear();
        foreach (var code in request.ProvinceCodes)
        {
            zone.ServiceableProvinces.Add(new ShippingZoneProvince
            {
                ZoneId = zone.Id,
                ProvinceCode = code.Trim(),
                CreatedAt = now
            });
        }

        // Replace rates
        _dbContext.ShippingRates.RemoveRange(zone.ShippingRates);
        zone.ShippingRates.Clear();
        foreach (var rateInput in request.Rates)
        {
            zone.ShippingRates.Add(new ShippingRate
            {
                Id = Guid.NewGuid(),
                ZoneId = zone.Id,
                BaseFee = rateInput.BaseFee,
                FreeShippingThreshold = rateInput.FreeShippingThreshold,
                EstimatedDaysMin = rateInput.EstimatedDaysMin,
                EstimatedDaysMax = rateInput.EstimatedDaysMax,
                IsActive = rateInput.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _dbContext.SaveChangesAsync(ct);

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorId,
            "Admin",
            "ShippingZoneUpdated",
            "ShippingZone",
            id.ToString(),
            null,
            new { zone.Name, request.ProvinceCodes }), ct);

        return await GetShippingZoneByIdAsync(id, ct);
    }

    public async Task<Result> DeleteShippingZoneAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var zone = await _dbContext.ShippingZones
            .Include(z => z.ServiceableProvinces)
            .Include(z => z.ShippingRates)
            .FirstOrDefaultAsync(z => z.Id == id, ct);

        if (zone is null)
        {
            return Result.Failure(DomainError.NotFound("Shipping zone not found."));
        }

        _dbContext.ShippingZoneProvinces.RemoveRange(zone.ServiceableProvinces);
        _dbContext.ShippingRates.RemoveRange(zone.ShippingRates);
        _dbContext.ShippingZones.Remove(zone);

        await _dbContext.SaveChangesAsync(ct);

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorId,
            "Admin",
            "ShippingZoneDeleted",
            "ShippingZone",
            id.ToString(),
            new { zone.Name },
            null), ct);

        return Result.Success();
    }

    private async Task<Result<ShippingZoneDto>> GetShippingZoneByIdAsync(Guid id, CancellationToken ct)
    {
        var zone = await _dbContext.ShippingZones
            .AsNoTracking()
            .Include(z => z.ServiceableProvinces)
            .Include(z => z.ShippingRates)
            .FirstOrDefaultAsync(z => z.Id == id, ct);

        if (zone is null)
        {
            return Result.Failure<ShippingZoneDto>(DomainError.NotFound("Shipping zone not found."));
        }

        return Result.Success(new ShippingZoneDto(
            zone.Id,
            zone.Name,
            zone.IsActive,
            zone.ServiceableProvinces.Select(p => p.ProvinceCode).ToList(),
            zone.ShippingRates.Select(r => new ShippingRateDto(
                r.Id,
                r.BaseFee,
                r.FreeShippingThreshold,
                r.EstimatedDaysMin,
                r.EstimatedDaysMax,
                r.IsActive)).ToList()));
    }

    #endregion

    #region Store Settings

    public async Task<Result<StoreSettingsDto>> GetStoreSettingsAsync(CancellationToken ct = default)
    {
        var settingsList = await _dbContext.Settings.AsNoTracking().ToListAsync(ct);
        var map = settingsList.ToDictionary(s => s.Key, s => s.Value);

        return Result.Success(new StoreSettingsDto(
            map.GetValueOrDefault("StoreName", "GroceryHub"),
            map.GetValueOrDefault("SupportPhone", "0901234567"),
            map.GetValueOrDefault("SupportEmail", "support@groceryhub.vn"),
            bool.TryParse(map.GetValueOrDefault("SePayEnabled", "false"), out bool sepay) && sepay,
            map.GetValueOrDefault("BankShortName", "MBBank"),
            map.GetValueOrDefault("BankAccountNumber", "0000000000"),
            map.GetValueOrDefault("BankAccountName", "CONG TY TNHH GROCERYHUB"),
            map.GetValueOrDefault("TransferContentPrefix", "GH"),
            int.TryParse(map.GetValueOrDefault("QrExpiryMinutes", "30"), out int exp) ? exp : 30,
            long.TryParse(map.GetValueOrDefault("FreeShippingThreshold", "300000"), out long fs) ? fs : DefaultFreeShippingThreshold,
            long.TryParse(map.GetValueOrDefault("CodMaxAmount", "2000000"), out long cod) ? cod : DefaultCodMaxAmount,
            int.TryParse(map.GetValueOrDefault("MaxQuantityPerLine", "20"), out int maxQ) ? maxQ : DefaultMaxQuantityPerLine,
            int.TryParse(map.GetValueOrDefault("DefaultLowStockThreshold", "5"), out int low) ? low : 5));
    }

    public async Task<Result<StoreSettingsDto>> UpdateStoreSettingsAsync(
        StoreSettingsDto dto,
        Guid actorId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        DateTimeOffset now = _timeProvider.GetUtcNow();

        Dictionary<string, string> values = new()
        {
            ["StoreName"] = dto.StoreName.Trim(),
            ["SupportPhone"] = dto.SupportPhone.Trim(),
            ["SupportEmail"] = dto.SupportEmail.Trim(),
            ["SePayEnabled"] = dto.SePayEnabled ? "true" : "false",
            ["BankShortName"] = dto.BankShortName.Trim(),
            ["BankAccountNumber"] = dto.BankAccountNumber.Trim(),
            ["BankAccountName"] = dto.BankAccountName.Trim(),
            ["TransferContentPrefix"] = dto.TransferContentPrefix?.Trim() ?? "GH",
            ["QrExpiryMinutes"] = dto.QrExpiryMinutes.ToString(),
            ["FreeShippingThreshold"] = dto.FreeShippingThreshold.ToString(),
            ["CodMaxAmount"] = dto.CodMaxAmount.ToString(),
            ["MaxQuantityPerLine"] = dto.MaxQuantityPerLine.ToString(),
            ["DefaultLowStockThreshold"] = dto.DefaultLowStockThreshold.ToString()
        };

        var existing = await _dbContext.Settings.ToListAsync(ct);
        var existingMap = existing.ToDictionary(s => s.Key);

        foreach (var (k, v) in values)
        {
            if (existingMap.TryGetValue(k, out var setting))
            {
                setting.Value = v;
                setting.UpdatedAt = now;
            }
            else
            {
                _dbContext.Settings.Add(new Setting
                {
                    Key = k,
                    Value = v,
                    UpdatedBy = actorId,
                    UpdatedAt = now
                });
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorId,
            "Admin",
            "StoreSettingsUpdated",
            "Settings",
            "Global",
            null,
            dto), ct);

        return Result.Success(dto);
    }

    #endregion

    private static string ComputeSha256(string content)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static CouponRuleInput ToRuleInput(Coupon coupon)
    {
        return new CouponRuleInput(
            coupon.Id,
            coupon.Code,
            coupon.Type,
            coupon.Value,
            coupon.MaxDiscount,
            coupon.MinSubtotal,
            coupon.StartsAt,
            coupon.EndsAt,
            coupon.UsageLimit,
            coupon.PerUserLimit,
            coupon.UsedCount,
            coupon.IsActive,
            coupon.Scopes.Select(s => new CouponScopeRuleDto(s.ScopeType, s.ScopeId)).ToList());
    }

    private static CustomerOrderDetailDto MapToCustomerOrderDetailDto(Order order, Payment? payment)
    {
        var address = new AddressSnapshotDto(
            order.ShippingRecipientName,
            order.ShippingPhone,
            order.ShippingProvinceName,
            order.ShippingWardName,
            order.ShippingStreet,
            order.ShippingNote);

        var items = order.Items.Select(i => new OrderItemViewDto(
            i.Id,
            i.ProductId,
            i.ProductSku,
            i.ProductName,
            i.ProductUnit,
            i.ProductPackSize,
            i.UnitPrice,
            i.Quantity,
            i.Subtotal,
            i.DiscountAllocated,
            Math.Max(0, i.Subtotal - i.DiscountAllocated),
            null)).ToList();

        var timeline = order.StatusHistory
            .OrderBy(h => h.CreatedAt)
            .Select(h => new OrderStatusHistoryDto(
                h.FromStatus,
                h.ToStatus,
                h.Reason,
                h.ActorRole,
                h.CreatedAt)).ToList();

        var paymentView = new OrderPaymentViewDto(
            payment?.Id ?? Guid.Empty,
            payment?.Status ?? order.PaymentStatus,
            payment?.Method ?? order.PaymentMethod,
            payment?.PaymentCode ?? string.Empty,
            AttemptNumber: 1,
            MaxAttempts: 1,
            ExpiresAt: null,
            Qr: null);

        bool canCancel = OrderStateMachine.CanCustomerCancel(order.Status);

        return new CustomerOrderDetailDto(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.PaymentMethod,
            order.PaymentStatus,
            order.Subtotal,
            order.CouponDiscount > 0 ? "COUPON" : null,
            order.CouponDiscount,
            order.ShippingFee,
            order.Total,
            address,
            order.ShippingNote,
            items,
            timeline,
            paymentView,
            canCancel,
            CanRetryPayment: false,
            order.CreatedAt,
            null);
    }

    private static AdminOrderDetailDto MapToAdminOrderDetailDto(Order order)
    {
        var address = new AddressSnapshotDto(
            order.ShippingRecipientName,
            order.ShippingPhone,
            order.ShippingProvinceName,
            order.ShippingWardName,
            order.ShippingStreet,
            order.ShippingNote);

        var items = order.Items.Select(i => new OrderItemViewDto(
            i.Id,
            i.ProductId,
            i.ProductSku,
            i.ProductName,
            i.ProductUnit,
            i.ProductPackSize,
            i.UnitPrice,
            i.Quantity,
            i.Subtotal,
            i.DiscountAllocated,
            Math.Max(0, i.Subtotal - i.DiscountAllocated),
            null)).ToList();

        var timeline = order.StatusHistory
            .OrderBy(h => h.CreatedAt)
            .Select(h => new OrderStatusHistoryDto(
                h.FromStatus,
                h.ToStatus,
                h.Reason,
                h.ActorRole,
                h.CreatedAt)).ToList();

        return new AdminOrderDetailDto(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.PaymentMethod,
            order.PaymentStatus,
            order.Subtotal,
            order.CouponDiscount > 0 ? "COUPON" : null,
            order.CouponDiscount,
            order.ShippingFee,
            order.Total,
            address,
            order.ShippingNote,
            order.CancellationReason,
            items,
            timeline,
            order.CreatedAt,
            order.CancelledAt,
            order.CancellationReason);
    }
}
