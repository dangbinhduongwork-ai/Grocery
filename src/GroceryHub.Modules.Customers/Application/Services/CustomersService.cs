using GroceryHub.Modules.Customers.Application.DTOs;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Customers.Application.Services;

public sealed class CustomersService
{
    private readonly GroceryHubDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly IAuditLogPort _auditLogPort;

    public CustomersService(
        GroceryHubDbContext dbContext,
        TimeProvider timeProvider,
        IAuditLogPort auditLogPort)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _auditLogPort = auditLogPort ?? throw new ArgumentNullException(nameof(auditLogPort));
    }

    #region Locations

    public async Task<Result<ProvincesResponse>> GetProvincesAsync(CancellationToken ct = default)
    {
        var provinces = await _dbContext.Provinces
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        var serviceableCodes = await _dbContext.ShippingZoneProvinces
            .AsNoTracking()
            .Where(szp => szp.Zone != null && szp.Zone.IsActive)
            .Select(szp => szp.ProvinceCode)
            .Distinct()
            .ToListAsync(ct);

        HashSet<string> serviceableSet = new(serviceableCodes, StringComparer.OrdinalIgnoreCase);

        string version = await _dbContext.AdminUnitDataVersions
            .AsNoTracking()
            .Select(v => v.Version)
            .FirstOrDefaultAsync(ct) ?? "2025.07.01";

        List<ProvinceDto> items = provinces
            .Select(p => new ProvinceDto(
                p.Code,
                p.Name,
                p.FullName,
                p.Type,
                serviceableSet.Contains(p.Code)))
            .ToList();

        return Result.Success(new ProvincesResponse(version, items));
    }

    public async Task<Result<WardsResponse>> GetWardsAsync(string provinceCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(provinceCode))
        {
            return Result.Failure<WardsResponse>(DomainError.NotFound("Province code is required."));
        }

        bool provinceExists = await _dbContext.Provinces
            .AsNoTracking()
            .AnyAsync(p => p.Code == provinceCode, ct);

        if (!provinceExists)
        {
            return Result.Failure<WardsResponse>(DomainError.NotFound($"Province with code '{provinceCode}' was not found."));
        }

        var wards = await _dbContext.Wards
            .AsNoTracking()
            .Where(w => w.ProvinceCode == provinceCode)
            .OrderBy(w => w.Name)
            .Select(w => new WardDto(w.Code, w.ProvinceCode, w.Name, w.FullName, w.Type))
            .ToListAsync(ct);

        return Result.Success(new WardsResponse(provinceCode, wards));
    }

    #endregion

    #region Profile

    public async Task<Result<CustomerProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        User? user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return Result.Failure<CustomerProfileDto>(DomainError.NotFound($"User with ID '{userId}' was not found."));
        }

        return Result.Success(new CustomerProfileDto(
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.EmailConfirmed,
            user.CreatedAt));
    }

    public async Task<Result<UpdateProfileResponse>> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        User? user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return Result.Failure<UpdateProfileResponse>(DomainError.NotFound($"User with ID '{userId}' was not found."));
        }

        string? normalizedPhone = null;
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            if (!VietnamesePhone.IsValid(request.Phone))
            {
                return Result.Failure<UpdateProfileResponse>(DomainError.Validation(
                    ErrorCodes.ValidationFailed,
                    "Phone number must be a valid 10-digit Vietnamese mobile number.",
                    new Dictionary<string, string[]> { ["phone"] = ["Invalid Vietnamese mobile phone number."] }));
            }

            normalizedPhone = VietnamesePhone.Normalize(request.Phone);
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = normalizedPhone;
        user.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new UpdateProfileResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber));
    }

    #endregion

    #region Addresses

    public async Task<Result<IReadOnlyList<AddressDto>>> GetAddressesAsync(Guid userId, CancellationToken ct = default)
    {
        var addresses = await _dbContext.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.DeletedAt == null)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AddressDto(
                a.Id,
                a.RecipientName,
                a.Phone,
                a.ProvinceCode,
                a.ProvinceName,
                a.WardCode,
                a.WardName,
                a.Street,
                a.Note,
                a.IsDefault))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<AddressDto>>(addresses);
    }

    public async Task<Result<AddressDto>> CreateAddressAsync(
        Guid userId,
        CreateAddressRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        int activeCount = await _dbContext.Addresses
            .CountAsync(a => a.UserId == userId && a.DeletedAt == null, ct);

        if (activeCount >= 10)
        {
            return Result.Failure<AddressDto>(DomainError.Validation(
                ErrorCodes.AddressLimitReached,
                "Customer already has 10 active addresses.",
                new Dictionary<string, string[]> { ["address"] = ["Maximum active address limit (10) reached."] }));
        }

        if (!VietnamesePhone.IsValid(request.Phone))
        {
            return Result.Failure<AddressDto>(DomainError.Validation(
                ErrorCodes.ValidationFailed,
                "Phone number must be a valid 10-digit Vietnamese mobile number.",
                new Dictionary<string, string[]> { ["phone"] = ["Invalid Vietnamese mobile phone number."] }));
        }

        string normalizedPhone = VietnamesePhone.Normalize(request.Phone);

        Province? province = await _dbContext.Provinces
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == request.ProvinceCode, ct);

        if (province is null)
        {
            return Result.Failure<AddressDto>(DomainError.Validation(
                ErrorCodes.ValidationFailed,
                $"Province with code '{request.ProvinceCode}' does not exist.",
                new Dictionary<string, string[]> { ["provinceCode"] = ["Invalid province code."] }));
        }

        Ward? ward = await _dbContext.Wards
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Code == request.WardCode && w.ProvinceCode == request.ProvinceCode, ct);

        if (ward is null)
        {
            return Result.Failure<AddressDto>(DomainError.Validation(
                ErrorCodes.ValidationFailed,
                $"Ward with code '{request.WardCode}' does not belong to province '{request.ProvinceCode}'.",
                new Dictionary<string, string[]> { ["wardCode"] = ["Invalid ward code for selected province."] }));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        bool isDefault = request.IsDefault || activeCount == 0;

        if (isDefault)
        {
            await _dbContext.Addresses
                .Where(a => a.UserId == userId && a.DeletedAt == null && a.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDefault, false).SetProperty(b => b.UpdatedAt, now), ct);
        }

        Address address = new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RecipientName = request.RecipientName.Trim(),
            Phone = normalizedPhone,
            ProvinceCode = province.Code,
            ProvinceName = province.FullName,
            WardCode = ward.Code,
            WardName = ward.FullName,
            Street = request.Street.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            IsDefault = isDefault,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Addresses.Add(address);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new AddressDto(
            address.Id,
            address.RecipientName,
            address.Phone,
            address.ProvinceCode,
            address.ProvinceName,
            address.WardCode,
            address.WardName,
            address.Street,
            address.Note,
            address.IsDefault));
    }

    public async Task<Result<AddressDto>> UpdateAddressAsync(
        Guid userId,
        Guid addressId,
        UpdateAddressRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Address? address = await _dbContext.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.DeletedAt == null, ct);

        if (address is null)
        {
            return Result.Failure<AddressDto>(DomainError.NotFound($"Address with ID '{addressId}' was not found."));
        }

        if (address.UserId != userId)
        {
            return Result.Failure<AddressDto>(DomainError.Forbidden(
                ErrorCodes.Forbidden,
                "You do not have permission to modify this address."));
        }

        if (!VietnamesePhone.IsValid(request.Phone))
        {
            return Result.Failure<AddressDto>(DomainError.Validation(
                ErrorCodes.ValidationFailed,
                "Phone number must be a valid 10-digit Vietnamese mobile number.",
                new Dictionary<string, string[]> { ["phone"] = ["Invalid Vietnamese mobile phone number."] }));
        }

        string normalizedPhone = VietnamesePhone.Normalize(request.Phone);

        Province? province = await _dbContext.Provinces
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == request.ProvinceCode, ct);

        if (province is null)
        {
            return Result.Failure<AddressDto>(DomainError.Validation(
                ErrorCodes.ValidationFailed,
                $"Province with code '{request.ProvinceCode}' does not exist.",
                new Dictionary<string, string[]> { ["provinceCode"] = ["Invalid province code."] }));
        }

        Ward? ward = await _dbContext.Wards
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Code == request.WardCode && w.ProvinceCode == request.ProvinceCode, ct);

        if (ward is null)
        {
            return Result.Failure<AddressDto>(DomainError.Validation(
                ErrorCodes.ValidationFailed,
                $"Ward with code '{request.WardCode}' does not belong to province '{request.ProvinceCode}'.",
                new Dictionary<string, string[]> { ["wardCode"] = ["Invalid ward code for selected province."] }));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        if (request.IsDefault && !address.IsDefault)
        {
            await _dbContext.Addresses
                .Where(a => a.UserId == userId && a.Id != addressId && a.DeletedAt == null && a.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDefault, false).SetProperty(b => b.UpdatedAt, now), ct);
        }

        address.RecipientName = request.RecipientName.Trim();
        address.Phone = normalizedPhone;
        address.ProvinceCode = province.Code;
        address.ProvinceName = province.FullName;
        address.WardCode = ward.Code;
        address.WardName = ward.FullName;
        address.Street = request.Street.Trim();
        address.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        address.IsDefault = request.IsDefault;
        address.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new AddressDto(
            address.Id,
            address.RecipientName,
            address.Phone,
            address.ProvinceCode,
            address.ProvinceName,
            address.WardCode,
            address.WardName,
            address.Street,
            address.Note,
            address.IsDefault));
    }

    public async Task<Result> DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default)
    {
        Address? address = await _dbContext.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.DeletedAt == null, ct);

        if (address is null)
        {
            return Result.Failure(DomainError.NotFound($"Address with ID '{addressId}' was not found."));
        }

        if (address.UserId != userId)
        {
            return Result.Failure(DomainError.Forbidden(
                ErrorCodes.Forbidden,
                "You do not have permission to delete this address."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        address.DeletedAt = now;
        address.UpdatedAt = now;

        if (address.IsDefault)
        {
            address.IsDefault = false;
            await _dbContext.SaveChangesAsync(ct);

            Address? nextDefault = await _dbContext.Addresses
                .Where(a => a.UserId == userId && a.Id != addressId && a.DeletedAt == null)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (nextDefault is not null)
            {
                nextDefault.IsDefault = true;
                nextDefault.UpdatedAt = now;
                await _dbContext.SaveChangesAsync(ct);
            }
        }
        else
        {
            await _dbContext.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    public async Task<Result<AddressDto>> SetDefaultAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default)
    {
        Address? address = await _dbContext.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.DeletedAt == null, ct);

        if (address is null)
        {
            return Result.Failure<AddressDto>(DomainError.NotFound($"Address with ID '{addressId}' was not found."));
        }

        if (address.UserId != userId)
        {
            return Result.Failure<AddressDto>(DomainError.Forbidden(
                ErrorCodes.Forbidden,
                "You do not have permission to modify this address."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        await _dbContext.Addresses
            .Where(a => a.UserId == userId && a.Id != addressId && a.DeletedAt == null && a.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDefault, false).SetProperty(b => b.UpdatedAt, now), ct);

        address.IsDefault = true;
        address.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new AddressDto(
            address.Id,
            address.RecipientName,
            address.Phone,
            address.ProvinceCode,
            address.ProvinceName,
            address.WardCode,
            address.WardName,
            address.Street,
            address.Note,
            address.IsDefault));
    }

    #endregion

    #region Admin Customers

    public async Task<Result<PagedResponse<AdminCustomerListItemDto>>> GetAdminCustomersAsync(
        string? q,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Customer") && u.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(q))
        {
            string search = q.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(search) ||
                u.FullName.ToLower().Contains(search) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(u => u.Status == status);
        }

        int totalItems = await query.CountAsync(ct);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.Status,
                u.CreatedAt,
                OrderCount = _dbContext.Orders.Count(o => o.UserId == u.Id),
                TotalSpent = _dbContext.Orders.Where(o => o.UserId == u.Id && o.Status == "Delivered").Sum(o => (long?)o.Total) ?? 0L,
                LastOrderAt = _dbContext.Orders.Where(o => o.UserId == u.Id).OrderByDescending(o => o.CreatedAt).Select(o => (DateTimeOffset?)o.CreatedAt).FirstOrDefault()
            })
            .ToListAsync(ct);

        var items = users.Select(u => new AdminCustomerListItemDto(
            u.Id,
            u.Email,
            u.FullName,
            u.PhoneNumber,
            u.Status,
            u.OrderCount,
            u.TotalSpent,
            u.CreatedAt,
            u.LastOrderAt)).ToList();

        return Result.Success(PagedResponse<AdminCustomerListItemDto>.Create(items, page, pageSize, totalItems));
    }

    public async Task<Result<AdminCustomerDetailDto>> GetAdminCustomerByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);

        if (user is null)
        {
            return Result.Failure<AdminCustomerDetailDto>(DomainError.NotFound($"Customer with ID '{id}' was not found."));
        }

        var addresses = await _dbContext.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == id && a.DeletedAt == null)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AddressDto(
                a.Id,
                a.RecipientName,
                a.Phone,
                a.ProvinceCode,
                a.ProvinceName,
                a.WardCode,
                a.WardName,
                a.Street,
                a.Note,
                a.IsDefault))
            .ToListAsync(ct);

        int totalOrders = await _dbContext.Orders.CountAsync(o => o.UserId == id, ct);
        long totalSpent = await _dbContext.Orders
            .Where(o => o.UserId == id && o.Status == "Delivered")
            .SumAsync(o => (long?)o.Total, ct) ?? 0L;
        DateTimeOffset? lastOrderAt = await _dbContext.Orders
            .Where(o => o.UserId == id)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => (DateTimeOffset?)o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        AdminCustomerStatsDto stats = new(totalOrders, totalSpent, lastOrderAt);

        return Result.Success(new AdminCustomerDetailDto(
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.Status,
            user.EmailConfirmed,
            user.CreatedAt,
            addresses,
            stats));
    }

    public async Task<Result> LockCustomerAsync(Guid id, string reason, Guid actorId, CancellationToken ct = default)
    {
        User? user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);

        if (user is null)
        {
            return Result.Failure(DomainError.NotFound($"Customer with ID '{id}' was not found."));
        }

        string oldStatus = user.Status;
        user.Status = "Locked";
        user.UpdatedAt = _timeProvider.GetUtcNow();

        // Revoke active refresh tokens
        await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == id && rt.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAt, _timeProvider.GetUtcNow()), ct);

        await _dbContext.SaveChangesAsync(ct);

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorId,
            "Staff",
            "CustomerLocked",
            "User",
            id.ToString(),
            new { Status = oldStatus },
            new { Status = "Locked", Reason = reason }), ct);

        return Result.Success();
    }

    public async Task<Result> UnlockCustomerAsync(Guid id, string reason, Guid actorId, CancellationToken ct = default)
    {
        User? user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);

        if (user is null)
        {
            return Result.Failure(DomainError.NotFound($"Customer with ID '{id}' was not found."));
        }

        string oldStatus = user.Status;
        user.Status = "Active";
        user.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(ct);

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorId,
            "Staff",
            "CustomerUnlocked",
            "User",
            id.ToString(),
            new { Status = oldStatus },
            new { Status = "Active", Reason = reason }), ct);

        return Result.Success();
    }

    public async Task<Result<DateTimeOffset>> AnonymizeCustomerAsync(Guid id, string reason, Guid actorId, CancellationToken ct = default)
    {
        User? user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);

        if (user is null)
        {
            return Result.Failure<DateTimeOffset>(DomainError.NotFound($"Customer with ID '{id}' was not found."));
        }

        // Active non-terminal orders check
        string[] activeStatuses = ["Placed", "Confirmed", "Preparing", "Packed", "Shipping"];
        bool hasActiveOrders = await _dbContext.Orders
            .AnyAsync(o => o.UserId == id && activeStatuses.Contains(o.Status), ct);

        if (hasActiveOrders)
        {
            return Result.Failure<DateTimeOffset>(DomainError.Conflict(
                ErrorCodes.Conflict,
                "Cannot anonymize customer with active orders in progress. Complete or cancel orders first."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        // Anonymize user PII
        user.FullName = "Khách hàng " + id.ToString()[..8];
        user.Email = $"anonymized_{id:N}@groceryhub.vn";
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        user.PhoneNumber = null;
        user.Status = "Disabled";
        user.DeletedAt = now;
        user.UpdatedAt = now;

        // Anonymize addresses
        var addresses = await _dbContext.Addresses
            .Where(a => a.UserId == id)
            .ToListAsync(ct);

        foreach (var addr in addresses)
        {
            addr.RecipientName = "Khách hàng " + id.ToString()[..8];
            addr.Phone = "0000000000";
            addr.Street = "Địa chỉ đã ẩn danh";
            addr.Note = null;
            addr.DeletedAt = now;
            addr.UpdatedAt = now;
        }

        // Revoke all refresh tokens
        await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == id && rt.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAt, now), ct);

        await _dbContext.SaveChangesAsync(ct);

        await _auditLogPort.WriteAsync(new WriteAuditLogCommand(
            actorId,
            "Admin",
            "CustomerAnonymized",
            "User",
            id.ToString(),
            null,
            new { AnonymizedAt = now, Reason = reason }), ct);

        return Result.Success(now);
    }

    #endregion
}
