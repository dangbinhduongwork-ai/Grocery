using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using CartEntity = GroceryHub.Persistence.Entities.Cart;

namespace GroceryHub.Modules.Cart.Application.Services;

public sealed class CartService : ICartService, ICartMergeService
{
    private const int MaxQuantityPerLine = 20;
    private readonly GroceryHubDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _timeProvider;

    public CartService(
        GroceryHubDbContext dbContext,
        IMemoryCache cache,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<(CartViewDto CartView, string? NewGuestToken)>> GetOrCreateCartViewAsync(
        Guid? userId,
        string? guestToken,
        CancellationToken ct = default)
    {
        var (cart, newGuestToken) = await EnsureCartEntityAsync(userId, guestToken, createIfMissing: true, ct);
        CartViewDto view = await BuildCartViewDtoAsync(cart, userId, ct);
        return Result.Success((view, newGuestToken));
    }

    public async Task<Result<(CartViewDto CartView, string? NewGuestToken)>> AddItemAsync(
        Guid? userId,
        string? guestToken,
        AddCartItemRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Quantity < 1 || request.Quantity > MaxQuantityPerLine)
        {
            return Result.Failure<(CartViewDto, string?)>(DomainError.Validation(
                ErrorCodes.QuantityLimitExceeded,
                $"Quantity must be between 1 and {MaxQuantityPerLine}."));
        }

        Product? product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.DeletedAt == null, ct);

        if (product is null || product.Status != "Active")
        {
            return Result.Failure<(CartViewDto, string?)>(DomainError.Conflict(
                ErrorCodes.ProductUnavailable,
                "The selected product is not available for purchase."));
        }

        StockItem? stock = await _dbContext.StockItems
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ProductId == request.ProductId, ct);

        int available = stock is not null ? Math.Max(0, stock.OnHand - stock.Reserved) : 0;

        var (cart, newGuestToken) = await EnsureCartEntityAsync(userId, guestToken, createIfMissing: true, ct);
        DateTimeOffset now = _timeProvider.GetUtcNow();

        CartItem? existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        int targetQuantity = request.Quantity;

        if (existingItem is not null)
        {
            targetQuantity = existingItem.Quantity + request.Quantity;
            if (targetQuantity > MaxQuantityPerLine)
            {
                return Result.Failure<(CartViewDto, string?)>(DomainError.Validation(
                    ErrorCodes.QuantityLimitExceeded,
                    $"Cannot add {request.Quantity} units. Maximum allowed per line is {MaxQuantityPerLine} (current in cart: {existingItem.Quantity})."));
            }

            if (targetQuantity > available)
            {
                return Result.Failure<(CartViewDto, string?)>(DomainError.Conflict(
                    ErrorCodes.InsufficientStock,
                    $"Insufficient stock for '{product.Name}'. Only {available} units available (requested total: {targetQuantity})."));
            }

            existingItem.Quantity = targetQuantity;
            existingItem.UpdatedAt = now;
        }
        else
        {
            if (request.Quantity > available)
            {
                return Result.Failure<(CartViewDto, string?)>(DomainError.Conflict(
                    ErrorCodes.InsufficientStock,
                    $"Insufficient stock for '{product.Name}'. Only {available} units available (requested: {request.Quantity})."));
            }

            CartItem newItem = new()
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CreatedAt = now,
                UpdatedAt = now
            };

            cart.Items.Add(newItem);
            _dbContext.CartItems.Add(newItem);
        }

        cart.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(ct);

        CartViewDto view = await BuildCartViewDtoAsync(cart, userId, ct);
        return Result.Success((view, newGuestToken));
    }

    public async Task<Result<CartViewDto>> UpdateItemQuantityAsync(
        Guid? userId,
        string? guestToken,
        Guid itemOrProductId,
        int newQuantity,
        CancellationToken ct = default)
    {
        if (newQuantity < 0 || newQuantity > MaxQuantityPerLine)
        {
            return Result.Failure<CartViewDto>(DomainError.Validation(
                ErrorCodes.QuantityLimitExceeded,
                $"Quantity must be between 0 and {MaxQuantityPerLine}."));
        }

        var (cart, _) = await EnsureCartEntityAsync(userId, guestToken, createIfMissing: false, ct);
        if (cart is null)
        {
            return Result.Failure<CartViewDto>(DomainError.NotFound("Cart not found."));
        }

        CartItem? item = cart.Items.FirstOrDefault(i => i.Id == itemOrProductId || i.ProductId == itemOrProductId);
        if (item is null)
        {
            return Result.Failure<CartViewDto>(DomainError.NotFound("Item not found in cart."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        if (newQuantity == 0)
        {
            cart.Items.Remove(item);
            _dbContext.CartItems.Remove(item);
        }
        else
        {
            StockItem? stock = await _dbContext.StockItems
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ProductId == item.ProductId, ct);

            int available = stock is not null ? Math.Max(0, stock.OnHand - stock.Reserved) : 0;
            if (newQuantity > available)
            {
                return Result.Failure<CartViewDto>(DomainError.Conflict(
                    ErrorCodes.InsufficientStock,
                    $"Insufficient stock. Only {available} units available."));
            }

            item.Quantity = newQuantity;
            item.UpdatedAt = now;
        }

        cart.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(ct);

        CartViewDto view = await BuildCartViewDtoAsync(cart, userId, ct);
        return Result.Success(view);
    }

    public async Task<Result<CartViewDto>> RemoveItemAsync(
        Guid? userId,
        string? guestToken,
        Guid itemOrProductId,
        CancellationToken ct = default)
    {
        var (cart, _) = await EnsureCartEntityAsync(userId, guestToken, createIfMissing: false, ct);
        if (cart is null)
        {
            return Result.Failure<CartViewDto>(DomainError.NotFound("Cart not found."));
        }

        CartItem? item = cart.Items.FirstOrDefault(i => i.Id == itemOrProductId || i.ProductId == itemOrProductId);
        if (item is null)
        {
            return Result.Failure<CartViewDto>(DomainError.NotFound("Item not found in cart."));
        }

        cart.Items.Remove(item);
        _dbContext.CartItems.Remove(item);
        cart.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(ct);

        CartViewDto view = await BuildCartViewDtoAsync(cart, userId, ct);
        return Result.Success(view);
    }

    public async Task<Result> ClearCartAsync(
        Guid? userId,
        string? guestToken,
        CancellationToken ct = default)
    {
        var (cart, _) = await EnsureCartEntityAsync(userId, guestToken, createIfMissing: false, ct);
        if (cart is not null && cart.Items.Count > 0)
        {
            _dbContext.CartItems.RemoveRange(cart.Items);
            cart.Items.Clear();
            cart.UpdatedAt = _timeProvider.GetUtcNow();
            await _dbContext.SaveChangesAsync(ct);

            _cache.Remove(GetCouponCacheKey(cart.Id));
        }

        return Result.Success();
    }

    public async Task<Result<CartViewDto>> ApplyCouponAsync(
        Guid userId,
        string? guestToken,
        string couponCode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return Result.Failure<CartViewDto>(DomainError.Validation(
                ErrorCodes.CouponInvalid,
                "Coupon code is required."));
        }

        var (cart, _) = await EnsureCartEntityAsync(userId, guestToken, createIfMissing: false, ct);
        if (cart is null || cart.Items.Count == 0)
        {
            return Result.Failure<CartViewDto>(DomainError.Validation(
                ErrorCodes.CartEmpty,
                "Cannot apply coupon to an empty cart."));
        }

        string normalizedCode = couponCode.Trim().ToUpperInvariant();

        Coupon? coupon = await _dbContext.Coupons
            .AsNoTracking()
            .Include(c => c.Scopes)
            .FirstOrDefaultAsync(c => c.Code == normalizedCode, ct);

        if (coupon is null || !coupon.IsActive)
        {
            return Result.Failure<CartViewDto>(DomainError.Validation(
                ErrorCodes.CouponInvalid,
                $"Coupon '{normalizedCode}' is invalid or inactive."));
        }

        // Build items for evaluation
        var evalItems = await BuildEvaluationItemsAsync(cart.Items, ct);
        long subtotal = evalItems.Sum(i => i.LineSubtotal);

        int userRedemptions = await _dbContext.CouponRedemptions
            .AsNoTracking()
            .CountAsync(r => r.CouponId == coupon.Id && r.UserId == userId && r.ReleasedAt == null, ct);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        var evalResult = CouponValidationEngine.Evaluate(ToRuleInput(coupon), userId, subtotal, 0, evalItems, now, userRedemptions);

        if (!evalResult.IsSuccess)
        {
            return Result.Failure<CartViewDto>(evalResult.Error);
        }

        // Save coupon code in cache for 24h
        _cache.Set(GetCouponCacheKey(cart.Id), normalizedCode, TimeSpan.FromHours(24));

        CartViewDto view = await BuildCartViewDtoAsync(cart, userId, ct);
        return Result.Success(view);
    }

    public async Task<Result<CartViewDto>> RemoveCouponAsync(
        Guid userId,
        string? guestToken,
        CancellationToken ct = default)
    {
        var (cart, _) = await EnsureCartEntityAsync(userId, guestToken, createIfMissing: false, ct);
        if (cart is not null)
        {
            _cache.Remove(GetCouponCacheKey(cart.Id));
            CartViewDto view = await BuildCartViewDtoAsync(cart, userId, ct);
            return Result.Success(view);
        }

        return Result.Failure<CartViewDto>(DomainError.NotFound("Cart not found."));
    }

    public async Task<Result> MergeGuestCartOnLoginAsync(
        Guid userId,
        string guestToken,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(guestToken))
        {
            return Result.Success();
        }

        CartEntity? guestCart = await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.GuestCartToken == guestToken && c.UserId == null, ct);

        if (guestCart is null || guestCart.Items.Count == 0)
        {
            if (guestCart is not null)
            {
                _dbContext.Carts.Remove(guestCart);
                await _dbContext.SaveChangesAsync(ct);
            }
            return Result.Success();
        }

        CartEntity? userCart = await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        DateTimeOffset now = _timeProvider.GetUtcNow();

        if (userCart is null)
        {
            userCart = new CartEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GuestCartToken = null,
                CreatedAt = now,
                UpdatedAt = now
            };
            _dbContext.Carts.Add(userCart);
        }

        var productIds = guestCart.Items.Select(i => i.ProductId).Distinct().ToList();
        var stockItems = await _dbContext.StockItems
            .AsNoTracking()
            .Where(s => productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, s => Math.Max(0, s.OnHand - s.Reserved), ct);

        var activeProducts = await _dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.Status == "Active" && p.DeletedAt == null)
            .Select(p => p.Id)
            .ToHashSetAsync(ct);

        foreach (var guestItem in guestCart.Items)
        {
            if (!activeProducts.Contains(guestItem.ProductId))
            {
                continue;
            }

            int available = stockItems.GetValueOrDefault(guestItem.ProductId, 0);
            int cap = Math.Min(MaxQuantityPerLine, available);
            if (cap <= 0)
            {
                continue;
            }

            CartItem? userItem = userCart.Items.FirstOrDefault(i => i.ProductId == guestItem.ProductId);
            if (userItem is not null)
            {
                userItem.Quantity = Math.Min(userItem.Quantity + guestItem.Quantity, cap);
                userItem.UpdatedAt = now;
            }
            else
            {
                CartItem newItem = new()
                {
                    Id = Guid.NewGuid(),
                    CartId = userCart.Id,
                    ProductId = guestItem.ProductId,
                    Quantity = Math.Min(guestItem.Quantity, cap),
                    CreatedAt = now,
                    UpdatedAt = now
                };
                userCart.Items.Add(newItem);
                _dbContext.CartItems.Add(newItem);
            }
        }

        // Migrate coupon from guest cart if present
        string guestCouponKey = GetCouponCacheKey(guestCart.Id);
        if (_cache.TryGetValue(guestCouponKey, out string? cachedCoupon))
        {
            _cache.Set(GetCouponCacheKey(userCart.Id), cachedCoupon, TimeSpan.FromHours(24));
            _cache.Remove(guestCouponKey);
        }

        _dbContext.CartItems.RemoveRange(guestCart.Items);
        _dbContext.Carts.Remove(guestCart);

        userCart.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    private async Task<(CartEntity Cart, string? NewGuestToken)> EnsureCartEntityAsync(
        Guid? userId,
        string? guestToken,
        bool createIfMissing,
        CancellationToken ct)
    {
        CartEntity? cart = null;
        string? newGuestToken = null;

        if (userId.HasValue)
        {
            cart = await _dbContext.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value, ct);
        }
        else if (!string.IsNullOrWhiteSpace(guestToken))
        {
            cart = await _dbContext.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.GuestCartToken == guestToken && c.UserId == null, ct);
        }

        if (cart is null && createIfMissing)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (!userId.HasValue && string.IsNullOrWhiteSpace(guestToken))
            {
                newGuestToken = Guid.NewGuid().ToString("N");
            }

            cart = new CartEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GuestCartToken = userId.HasValue ? null : (guestToken ?? newGuestToken),
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.Carts.Add(cart);
            await _dbContext.SaveChangesAsync(ct);
        }

        return (cart!, newGuestToken);
    }

    private async Task<CartViewDto> BuildCartViewDtoAsync(
        CartEntity? cart,
        Guid? userId,
        CancellationToken ct)
    {
        if (cart is null || cart.Items.Count == 0)
        {
            return new CartViewDto(
                cart?.Id ?? Guid.Empty,
                [],
                0,
                null,
                0,
                0,
                MaxQuantityPerLine,
                []);
        }

        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();

        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var stockItems = await _dbContext.StockItems
            .AsNoTracking()
            .Where(s => productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, ct);

        List<CartItemViewDto> lineViews = [];
        List<CartWarningDto> warnings = [];
        List<CartItemEvaluationDto> evalItems = [];

        long subtotal = 0;

        foreach (var item in cart.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product) || product.DeletedAt is not null || product.Status != "Active")
            {
                warnings.Add(new CartWarningDto("PRODUCT_UNAVAILABLE", $"Product '{product?.Name ?? item.ProductId.ToString()}' is no longer available.", item.ProductId));
                continue;
            }

            StockItem? stock = stockItems.GetValueOrDefault(item.ProductId);
            int onHand = stock?.OnHand ?? 0;
            int reserved = stock?.Reserved ?? 0;
            int available = Math.Max(0, onHand - reserved);
            int reorderLevel = stock?.ReorderLevel ?? 5;

            bool inStock = available > 0;
            bool lowStock = available > 0 && available <= reorderLevel;

            if (item.Quantity > available)
            {
                warnings.Add(new CartWarningDto("INSUFFICIENT_STOCK", $"Only {available} units available for '{product.Name}'.", item.ProductId));
            }

            long lineSubtotal = product.SellingPrice * item.Quantity;
            subtotal += lineSubtotal;

            string? storageKey = product.Images
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => i.StorageKey)
                .FirstOrDefault();

            string? primaryImageUrl = string.IsNullOrWhiteSpace(storageKey)
                ? null
                : $"/uploads/{Path.GetFileName(storageKey)}";

            lineViews.Add(new CartItemViewDto(
                item.Id,
                item.ProductId,
                product.Sku,
                product.Name,
                product.Unit,
                product.PackSize,
                product.SellingPrice,
                product.ListPrice,
                item.Quantity,
                lineSubtotal,
                inStock,
                lowStock,
                available,
                primaryImageUrl));

            evalItems.Add(new CartItemEvaluationDto(
                product.Id,
                product.CategoryId,
                product.SellingPrice,
                item.Quantity,
                lineSubtotal));
        }

        string? couponCode = null;
        long couponDiscount = 0;

        string couponKey = GetCouponCacheKey(cart.Id);
        if (_cache.TryGetValue(couponKey, out string? cachedCode) && !string.IsNullOrWhiteSpace(cachedCode))
        {
            Coupon? coupon = await _dbContext.Coupons
                .AsNoTracking()
                .Include(c => c.Scopes)
                .FirstOrDefaultAsync(c => c.Code == cachedCode, ct);

            int userRedemptions = 0;
            if (userId.HasValue && coupon is not null)
            {
                userRedemptions = await _dbContext.CouponRedemptions
                    .AsNoTracking()
                    .CountAsync(r => r.CouponId == coupon.Id && r.UserId == userId.Value && r.ReleasedAt == null, ct);
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (coupon is not null)
            {
                var evalResult = CouponValidationEngine.Evaluate(ToRuleInput(coupon), userId, subtotal, 0, evalItems, now, userRedemptions);
                if (evalResult.IsSuccess)
                {
                    couponCode = cachedCode;
                    couponDiscount = evalResult.Value.DiscountAmount;
                }
                else
                {
                    _cache.Remove(couponKey);
                    warnings.Add(new CartWarningDto("COUPON_REMOVED", $"Coupon '{cachedCode}' was removed: {evalResult.Error.Message}"));
                }
            }
            else
            {
                _cache.Remove(couponKey);
            }
        }

        long estimatedTotal = Math.Max(0, subtotal - couponDiscount);

        return new CartViewDto(
            cart.Id,
            lineViews,
            subtotal,
            couponCode,
            couponDiscount,
            estimatedTotal,
            MaxQuantityPerLine,
            warnings);
    }

    private async Task<List<CartItemEvaluationDto>> BuildEvaluationItemsAsync(
        IEnumerable<CartItem> cartItems,
        CancellationToken ct)
    {
        var productIds = cartItems.Select(i => i.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.DeletedAt == null && p.Status == "Active")
            .ToDictionaryAsync(p => p.Id, ct);

        List<CartItemEvaluationDto> list = [];
        foreach (var item in cartItems)
        {
            if (products.TryGetValue(item.ProductId, out var product))
            {
                list.Add(new CartItemEvaluationDto(
                    product.Id,
                    product.CategoryId,
                    product.SellingPrice,
                    item.Quantity,
                    product.SellingPrice * item.Quantity));
            }
        }

        return list;
    }

    private static string GetCouponCacheKey(Guid cartId) => $"cart_coupon_{cartId}";

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
}
