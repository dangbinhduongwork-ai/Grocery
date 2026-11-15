namespace GroceryHub.Modules.Promotions.Application.DTOs;

public sealed record CouponScopeDto(
    string ScopeType,
    Guid ScopeId);

public sealed record CouponDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Type,
    long Value,
    long? MaxDiscount,
    long MinSubtotal,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? UsageLimit,
    int? PerUserLimit,
    int UsedCount,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string RowVersion,
    IReadOnlyList<CouponScopeDto> Scopes);

public sealed record CreateCouponRequest(
    string Code,
    string Name,
    string? Description,
    string Type,
    long Value,
    long? MaxDiscount,
    long MinSubtotal,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? UsageLimit,
    int? PerUserLimit,
    bool IsActive,
    IReadOnlyList<CouponScopeDto>? Scopes);

public sealed record UpdateCouponRequest(
    string Code,
    string Name,
    string? Description,
    string Type,
    long Value,
    long? MaxDiscount,
    long MinSubtotal,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? UsageLimit,
    int? PerUserLimit,
    bool IsActive,
    IReadOnlyList<CouponScopeDto>? Scopes);

public sealed record CouponRedemptionDto(
    Guid Id,
    Guid CouponId,
    Guid UserId,
    Guid OrderId,
    long Amount,
    DateTimeOffset RedeemedAt,
    DateTimeOffset? ReleasedAt);

public sealed record CouponUsageSummaryDto(
    Guid CouponId,
    string Code,
    int UsedCount,
    int? UsageLimit,
    long TotalDiscountGiven,
    int UniqueUsersCount);
