using GroceryHub.Modules.Promotions.Application.DTOs;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Promotions.Application.Services;

public sealed class PromotionsService
{
    private readonly GroceryHubDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly IAuditLogPort _auditLogPort;

    public PromotionsService(
        GroceryHubDbContext dbContext,
        TimeProvider timeProvider,
        IAuditLogPort auditLogPort)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _auditLogPort = auditLogPort ?? throw new ArgumentNullException(nameof(auditLogPort));
    }

    #region Admin Coupon CRUD

    public async Task<Result<PagedResponse<CouponDto>>> GetCouponsAsync(
        string? q,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Coupons
            .AsNoTracking()
            .Include(c => c.Scopes)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            string search = q.Trim().ToLower();
            query = query.Where(c => c.Code.ToLower().Contains(search) || c.Name.ToLower().Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        int totalItems = await query.CountAsync(ct);

        var coupons = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = coupons.Select(MapToDto).ToList();

        return Result.Success(PagedResponse<CouponDto>.Create(items, page, pageSize, totalItems));
    }

    public async Task<Result<CouponDto>> GetCouponByIdAsync(Guid id, CancellationToken ct = default)
    {
        Coupon? coupon = await _dbContext.Coupons
            .AsNoTracking()
            .Include(c => c.Scopes)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (coupon is null)
        {
            return Result.Failure<CouponDto>(DomainError.NotFound($"Coupon with ID '{id}' was not found."));
        }

        return Result.Success(MapToDto(coupon));
    }

    public async Task<Result<CouponDto>> CreateCouponAsync(
        CreateCouponRequest request,
        Guid actorId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string normalizedCode = request.Code.Trim().ToUpperInvariant();

        bool codeExists = await _dbContext.Coupons
            .AsNoTracking()
            .AnyAsync(c => c.Code == normalizedCode, ct);

        if (codeExists)
        {
            return Result.Failure<CouponDto>(DomainError.Conflict(
                ErrorCodes.Conflict,
                $"Coupon code '{normalizedCode}' already exists."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        Coupon coupon = new()
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Type = request.Type,
            Value = request.Value,
            MaxDiscount = request.MaxDiscount,
            MinSubtotal = request.MinSubtotal,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            UsageLimit = request.UsageLimit,
            PerUserLimit = request.PerUserLimit,
            UsedCount = 0,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (request.Scopes != null && request.Scopes.Count > 0)
        {
            foreach (var scope in request.Scopes)
            {
                coupon.Scopes.Add(new CouponScope
                {
                    CouponId = coupon.Id,
                    ScopeType = scope.ScopeType,
                    ScopeId = scope.ScopeId,
                    CreatedAt = now
                });
            }
        }

        _dbContext.Coupons.Add(coupon);
        await _dbContext.SaveChangesAsync(ct);

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorId,
            "Manager",
            "CouponCreated",
            "Coupon",
            coupon.Id.ToString(),
            null,
            new { coupon.Code, coupon.Type, coupon.Value, coupon.MinSubtotal }), ct);

        return Result.Success(MapToDto(coupon));
    }

    public async Task<Result<CouponDto>> UpdateCouponAsync(
        Guid id,
        UpdateCouponRequest request,
        string? ifMatch,
        Guid actorId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Coupon? coupon = await _dbContext.Coupons
            .Include(c => c.Scopes)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (coupon is null)
        {
            return Result.Failure<CouponDto>(DomainError.NotFound($"Coupon with ID '{id}' was not found."));
        }

        if (!string.IsNullOrWhiteSpace(ifMatch))
        {
            string currentRowVersion = Convert.ToBase64String(coupon.RowVersion);
            string cleanIfMatch = ifMatch.Trim('\"');
            if (!string.Equals(currentRowVersion, cleanIfMatch, StringComparison.Ordinal))
            {
                return Result.Failure<CouponDto>(DomainError.Concurrency(
                    "The coupon was updated by another request."));
            }
        }

        string normalizedCode = request.Code.Trim().ToUpperInvariant();
        bool duplicateCode = await _dbContext.Coupons
            .AsNoTracking()
            .AnyAsync(c => c.Code == normalizedCode && c.Id != id, ct);

        if (duplicateCode)
        {
            return Result.Failure<CouponDto>(DomainError.Conflict(
                ErrorCodes.Conflict,
                $"Coupon code '{normalizedCode}' already exists on another coupon."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        coupon.Code = normalizedCode;
        coupon.Name = request.Name.Trim();
        coupon.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        coupon.Type = request.Type;
        coupon.Value = request.Value;
        coupon.MaxDiscount = request.MaxDiscount;
        coupon.MinSubtotal = request.MinSubtotal;
        coupon.StartsAt = request.StartsAt;
        coupon.EndsAt = request.EndsAt;
        coupon.UsageLimit = request.UsageLimit;
        coupon.PerUserLimit = request.PerUserLimit;
        coupon.IsActive = request.IsActive;
        coupon.UpdatedAt = now;

        // Replace scopes
        _dbContext.CouponScopes.RemoveRange(coupon.Scopes);
        coupon.Scopes.Clear();

        if (request.Scopes != null && request.Scopes.Count > 0)
        {
            foreach (var scope in request.Scopes)
            {
                coupon.Scopes.Add(new CouponScope
                {
                    CouponId = coupon.Id,
                    ScopeType = scope.ScopeType,
                    ScopeId = scope.ScopeId,
                    CreatedAt = now
                });
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorId,
            "Manager",
            "CouponUpdated",
            "Coupon",
            coupon.Id.ToString(),
            null,
            new { coupon.Code, coupon.Type, coupon.Value, coupon.IsActive }), ct);

        return Result.Success(MapToDto(coupon));
    }

    public async Task<Result> DeleteCouponAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        Coupon? coupon = await _dbContext.Coupons
            .Include(c => c.Scopes)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (coupon is null)
        {
            return Result.Failure(DomainError.NotFound($"Coupon with ID '{id}' was not found."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        if (coupon.UsedCount > 0)
        {
            // Deactivate instead of hard deleting if already used
            coupon.IsActive = false;
            coupon.UpdatedAt = now;
            await _dbContext.SaveChangesAsync(ct);

            await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
                actorId,
                "Manager",
                "CouponDeactivatedOnDelete",
                "Coupon",
                coupon.Id.ToString(),
                new { IsActive = true, coupon.UsedCount },
                new { IsActive = false }), ct);
        }
        else
        {
            _dbContext.CouponScopes.RemoveRange(coupon.Scopes);
            _dbContext.Coupons.Remove(coupon);
            await _dbContext.SaveChangesAsync(ct);

            await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
                actorId,
                "Manager",
                "CouponDeleted",
                "Coupon",
                id.ToString(),
                new { coupon.Code },
                null), ct);
        }

        return Result.Success();
    }

    public async Task<Result<PagedResponse<CouponRedemptionDto>>> GetCouponRedemptionsAsync(
        Guid couponId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        bool couponExists = await _dbContext.Coupons
            .AsNoTracking()
            .AnyAsync(c => c.Id == couponId, ct);

        if (!couponExists)
        {
            return Result.Failure<PagedResponse<CouponRedemptionDto>>(DomainError.NotFound($"Coupon with ID '{couponId}' was not found."));
        }

        var query = _dbContext.CouponRedemptions
            .AsNoTracking()
            .Where(r => r.CouponId == couponId);

        int totalItems = await query.CountAsync(ct);

        var redemptions = await query
            .OrderByDescending(r => r.RedeemedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new CouponRedemptionDto(
                r.Id,
                r.CouponId,
                r.UserId,
                r.OrderId,
                r.Amount,
                r.RedeemedAt,
                r.ReleasedAt))
            .ToListAsync(ct);

        return Result.Success(PagedResponse<CouponRedemptionDto>.Create(redemptions, page, pageSize, totalItems));
    }

    public async Task<Result<CouponUsageSummaryDto>> GetCouponUsageAsync(Guid couponId, CancellationToken ct = default)
    {
        Coupon? coupon = await _dbContext.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == couponId, ct);

        if (coupon is null)
        {
            return Result.Failure<CouponUsageSummaryDto>(DomainError.NotFound($"Coupon with ID '{couponId}' was not found."));
        }

        long totalDiscountGiven = await _dbContext.CouponRedemptions
            .Where(r => r.CouponId == couponId && r.ReleasedAt == null)
            .SumAsync(r => (long?)r.Amount, ct) ?? 0L;

        int uniqueUsersCount = await _dbContext.CouponRedemptions
            .Where(r => r.CouponId == couponId && r.ReleasedAt == null)
            .Select(r => r.UserId)
            .Distinct()
            .CountAsync(ct);

        return Result.Success(new CouponUsageSummaryDto(
            coupon.Id,
            coupon.Code,
            coupon.UsedCount,
            coupon.UsageLimit,
            totalDiscountGiven,
            uniqueUsersCount));
    }

    #endregion

    #region Evaluation & Redemption Helpers

    public async Task<Result<CouponEvaluationResult>> EvaluateCouponAsync(
        string code,
        Guid? userId,
        long subtotal,
        long shippingFee,
        IReadOnlyList<CartItemEvaluationDto> items,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<CouponEvaluationResult>(DomainError.Validation(
                ErrorCodes.CouponInvalid,
                "Coupon code is required."));
        }

        string normalizedCode = code.Trim().ToUpperInvariant();

        Coupon? coupon = await _dbContext.Coupons
            .AsNoTracking()
            .Include(c => c.Scopes)
            .FirstOrDefaultAsync(c => c.Code == normalizedCode, ct);

        if (coupon is null)
        {
            return Result.Failure<CouponEvaluationResult>(DomainError.Validation(
                ErrorCodes.CouponInvalid,
                $"Coupon '{normalizedCode}' was not found."));
        }

        int userExistingRedemptions = 0;
        if (userId.HasValue)
        {
            userExistingRedemptions = await _dbContext.CouponRedemptions
                .AsNoTracking()
                .CountAsync(r => r.CouponId == coupon.Id && r.UserId == userId.Value && r.ReleasedAt == null, ct);
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        return CouponValidationEngine.Evaluate(
            ToRuleInput(coupon),
            userId,
            subtotal,
            shippingFee,
            items,
            now,
            userExistingRedemptions);
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

    #endregion

    private static CouponDto MapToDto(Coupon coupon)
    {
        var scopes = coupon.Scopes
            .Select(s => new CouponScopeDto(s.ScopeType, s.ScopeId))
            .ToList();

        return new CouponDto(
            coupon.Id,
            coupon.Code,
            coupon.Name,
            coupon.Description,
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
            coupon.CreatedAt,
            coupon.UpdatedAt,
            Convert.ToBase64String(coupon.RowVersion),
            scopes);
    }
}
