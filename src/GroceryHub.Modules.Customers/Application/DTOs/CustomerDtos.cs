namespace GroceryHub.Modules.Customers.Application.DTOs;

public sealed record ProvinceDto(
    string Code,
    string Name,
    string FullName,
    string Type,
    bool IsServiceable);

public sealed record ProvincesResponse(
    string Version,
    IReadOnlyList<ProvinceDto> Items);

public sealed record WardDto(
    string Code,
    string ProvinceCode,
    string Name,
    string FullName,
    string Type);

public sealed record WardsResponse(
    string ProvinceCode,
    IReadOnlyList<WardDto> Items);

public sealed record CustomerProfileDto(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    bool EmailConfirmed,
    DateTimeOffset CreatedAt);

public sealed record UpdateProfileRequest(
    string FullName,
    string? Phone);

public sealed record UpdateProfileResponse(
    Guid Id,
    string Email,
    string FullName,
    string? Phone);

public sealed record AddressDto(
    Guid Id,
    string RecipientName,
    string Phone,
    string ProvinceCode,
    string ProvinceName,
    string WardCode,
    string WardName,
    string Street,
    string? Note,
    bool IsDefault);

public sealed record CreateAddressRequest(
    string RecipientName,
    string Phone,
    string ProvinceCode,
    string WardCode,
    string Street,
    string? Note,
    bool IsDefault);

public sealed record UpdateAddressRequest(
    string RecipientName,
    string Phone,
    string ProvinceCode,
    string WardCode,
    string Street,
    string? Note,
    bool IsDefault);

public sealed record AdminCustomerListItemDto(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    string Status,
    int OrderCount,
    long TotalSpent,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastOrderAt);

public sealed record AdminCustomerStatsDto(
    int TotalOrders,
    long TotalSpent,
    DateTimeOffset? LastOrderAt);

public sealed record AdminCustomerDetailDto(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    string Status,
    bool EmailConfirmed,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AddressDto> Addresses,
    AdminCustomerStatsDto Stats);

public sealed record LockCustomerRequest(string Reason);

public sealed record AnonymizeCustomerRequest(string Reason);
