using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Identity.Application;

public sealed class StaffAdminService
{
    private readonly GroceryHubDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly ITokenService _tokenService;
    private readonly IPasswordPolicyValidator _passwordValidator;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IAuditLogPort _auditLog;
    private readonly TimeProvider _timeProvider;

    public StaffAdminService(
        GroceryHubDbContext dbContext,
        IPermissionService permissionService,
        ITokenService tokenService,
        IPasswordPolicyValidator passwordValidator,
        IPasswordHasher<User> passwordHasher,
        IAuditLogPort auditLog,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _passwordValidator = passwordValidator ?? throw new ArgumentNullException(nameof(passwordValidator));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<PagedResponse<StaffAccountDto>>> GetStaffListAsync(
        string? q = null,
        string? role = null,
        string? status = null,
        int page = 1,
        int pageSize = 24,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<User> query = _dbContext.Users
            .AsNoTracking()
            .Include(u => u.StaffProfile)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.UserPermissions)
            .Where(u => u.StaffProfile != null);

        if (!string.IsNullOrWhiteSpace(q))
        {
            string search = q.Trim();
            query = query.Where(u =>
                u.FullName.Contains(search) ||
                u.Email.Contains(search) ||
                (u.StaffProfile != null && u.StaffProfile.EmployeeCode.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            string roleFilter = role.Trim();
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleFilter));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            string statusFilter = status.Trim();
            query = query.Where(u => u.Status == statusFilter);
        }

        int totalCount = await query.CountAsync(ct);

        List<User> users = await query
            .OrderBy(u => u.StaffProfile!.EmployeeCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        List<StaffAccountDto> items = users.Select(MapToStaffAccountDto).ToList();

        return Result.Success(PagedResponse<StaffAccountDto>.Create(items, page, pageSize, totalCount));
    }

    public async Task<Result<StaffAccountDto>> GetStaffByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        User? user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.StaffProfile)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.UserPermissions)
            .FirstOrDefaultAsync(u => u.Id == id && u.StaffProfile != null, ct);

        if (user is null)
        {
            return Result.Failure<StaffAccountDto>(DomainError.NotFound($"Staff account with ID '{id}' was not found."));
        }

        return Result.Success(MapToStaffAccountDto(user));
    }

    public async Task<Result<StaffAccountDto>> CreateStaffAsync(
        CreateStaffRequest request,
        Guid actorUserId,
        IReadOnlySet<string> actorPermissions,
        string? ipAddress = null,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        bool hasStaffManage = actorPermissions.Contains(Permissions.StaffManage);
        bool hasStaffCreate = actorPermissions.Contains(Permissions.StaffCreate);

        if (!hasStaffManage && !hasStaffCreate)
        {
            return Result.Failure<StaffAccountDto>(DomainError.Forbidden(
                ErrorCodes.Forbidden,
                "You do not have permission to create staff accounts."));
        }

        if (!hasStaffManage)
        {
            // Caller only has staff.create
            if (!string.Equals(request.Role, PredefinedRoles.Staff, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<StaffAccountDto>(DomainError.Forbidden(
                    ErrorCodes.Forbidden,
                    "Only administrators can create manager or admin accounts."));
            }

            if (request.ExtraPermissions is not null && request.ExtraPermissions.Count > 0)
            {
                return Result.Failure<StaffAccountDto>(DomainError.Forbidden(
                    ErrorCodes.Forbidden,
                    "Granting extra permissions requires staff.manage permission."));
            }
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result.Failure<StaffAccountDto>(DomainError.ValidationFailed(
                new Dictionary<string, string[]> { ["email"] = ["Email is required."] },
                "Email is required."));
        }

        string email = request.Email.Trim().ToLowerInvariant();
        string normalizedEmail = email.ToUpperInvariant();

        Result passwordValidation = _passwordValidator.Validate(request.Password);
        if (passwordValidation.IsFailure)
        {
            return Result.Failure<StaffAccountDto>(passwordValidation.Error);
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return Result.Failure<StaffAccountDto>(DomainError.ValidationFailed(
                new Dictionary<string, string[]> { ["fullName"] = ["Full name is required."] },
                "Full name is required."));
        }

        if (string.IsNullOrWhiteSpace(request.EmployeeCode))
        {
            return Result.Failure<StaffAccountDto>(DomainError.ValidationFailed(
                new Dictionary<string, string[]> { ["employeeCode"] = ["Employee code is required."] },
                "Employee code is required."));
        }

        string employeeCode = request.EmployeeCode.Trim();

        string? phone = null;
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            phone = request.Phone.Trim();
            if (!VietnamesePhone.IsValid(phone))
            {
                return Result.Failure<StaffAccountDto>(DomainError.ValidationFailed(
                    new Dictionary<string, string[]> { ["phone"] = ["Phone number must be a valid 10-digit Vietnamese mobile number."] },
                    "Invalid Vietnamese phone number."));
            }
        }

        bool emailExists = await _dbContext.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);
        if (emailExists)
        {
            return Result.Failure<StaffAccountDto>(DomainError.Conflict(
                ErrorCodes.EmailAlreadyRegistered,
                "Email is already registered."));
        }

        bool codeExists = await _dbContext.StaffProfiles.AnyAsync(sp => sp.EmployeeCode == employeeCode, ct);
        if (codeExists)
        {
            return Result.Failure<StaffAccountDto>(DomainError.Conflict(
                ErrorCodes.Conflict,
                $"Employee code '{employeeCode}' is already in use."));
        }

        string targetRoleName = request.Role?.Trim() ?? PredefinedRoles.Staff;
        Role? targetRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == targetRoleName, ct);
        if (targetRole is null)
        {
            return Result.Failure<StaffAccountDto>(DomainError.ValidationFailed(
                new Dictionary<string, string[]> { ["role"] = [$"Role '{targetRoleName}' does not exist."] },
                $"Role '{targetRoleName}' does not exist."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        User user = new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            FullName = request.FullName.Trim(),
            PhoneNumber = phone,
            Status = UserStatus.Active,
            EmailConfirmed = true,
            LockoutEnabled = true,
            CreatedAt = now,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = targetRole.Id,
            CreatedAt = now,
            Role = targetRole
        });

        StaffProfile staffProfile = new()
        {
            UserId = user.Id,
            EmployeeCode = employeeCode,
            Position = request.Position?.Trim() ?? string.Empty,
            HiredAt = now,
            CreatedAt = now
        };

        user.StaffProfile = staffProfile;

        if (request.ExtraPermissions is not null && request.ExtraPermissions.Count > 0)
        {
            List<Permission> validPermissions = await _dbContext.Permissions
                .Where(p => request.ExtraPermissions.Contains(p.Code) && p.AllowPerUserGrant)
                .ToListAsync(ct);

            foreach (Permission perm in validPermissions)
            {
                user.UserPermissions.Add(new UserPermission
                {
                    UserId = user.Id,
                    PermissionCode = perm.Code,
                    GrantedBy = actorUserId,
                    GrantedAt = now
                });
            }
        }

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(ct);

        await _auditLog.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            null,
            "Staff.Create",
            "StaffProfile",
            user.Id.ToString(),
            null,
            new
            {
                userId = user.Id,
                email = user.Email,
                employeeCode = staffProfile.EmployeeCode,
                role = targetRoleName,
                extraPermissions = request.ExtraPermissions
            },
            ipAddress,
            correlationId),
            ct);

        return Result.Success(MapToStaffAccountDto(user));
    }

    public async Task<Result<StaffAccountDto>> UpdateStaffAsync(
        Guid id,
        UpdateStaffRequest request,
        Guid actorUserId,
        string? ipAddress = null,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        User? user = await _dbContext.Users
            .Include(u => u.StaffProfile)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.UserPermissions)
            .FirstOrDefaultAsync(u => u.Id == id && u.StaffProfile != null, ct);

        if (user is null)
        {
            return Result.Failure<StaffAccountDto>(DomainError.NotFound($"Staff account with ID '{id}' was not found."));
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return Result.Failure<StaffAccountDto>(DomainError.ValidationFailed(
                new Dictionary<string, string[]> { ["fullName"] = ["Full name is required."] },
                "Full name is required."));
        }

        string? phone = null;
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            phone = request.Phone.Trim();
            if (!VietnamesePhone.IsValid(phone))
            {
                return Result.Failure<StaffAccountDto>(DomainError.ValidationFailed(
                    new Dictionary<string, string[]> { ["phone"] = ["Phone number must be a valid 10-digit Vietnamese mobile number."] },
                    "Invalid Vietnamese phone number."));
            }
        }

        if (!UserStatus.All.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure<StaffAccountDto>(DomainError.ValidationFailed(
                new Dictionary<string, string[]> { ["status"] = ["Invalid user status."] },
                $"Invalid status '{request.Status}'. Valid values are: {string.Join(", ", UserStatus.All)}"));
        }

        var beforeState = new
        {
            fullName = user.FullName,
            phone = user.PhoneNumber,
            position = user.StaffProfile!.Position,
            status = user.Status,
            note = user.StaffProfile.Note
        };

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = phone;
        user.Status = request.Status;
        user.StaffProfile!.Position = request.Position?.Trim() ?? string.Empty;
        user.StaffProfile.Note = request.Note?.Trim();

        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (request.Status != UserStatus.Active)
        {
            await _tokenService.RevokeAllUserTokensAsync(user.Id, ct);
        }

        _permissionService.InvalidateUserPermissionsCache(user.Id);
        await _dbContext.SaveChangesAsync(ct);

        await _auditLog.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            null,
            "Staff.Update",
            "StaffProfile",
            user.Id.ToString(),
            beforeState,
            new
            {
                fullName = user.FullName,
                phone = user.PhoneNumber,
                position = user.StaffProfile.Position,
                status = user.Status,
                note = user.StaffProfile.Note
            },
            ipAddress,
            correlationId),
            ct);

        return Result.Success(MapToStaffAccountDto(user));
    }

    public async Task<Result<StaffAccountDto>> UpdateStaffRolesAsync(
        Guid id,
        UpdateStaffRolesRequest request,
        Guid actorUserId,
        string? ipAddress = null,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Roles is null || request.Roles.Count == 0)
        {
            return Result.Failure<StaffAccountDto>(DomainError.ValidationFailed(
                new Dictionary<string, string[]> { ["roles"] = ["At least one role is required."] },
                "At least one role is required."));
        }

        User? user = await _dbContext.Users
            .Include(u => u.StaffProfile)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.UserPermissions)
            .FirstOrDefaultAsync(u => u.Id == id && u.StaffProfile != null, ct);

        if (user is null)
        {
            return Result.Failure<StaffAccountDto>(DomainError.NotFound($"Staff account with ID '{id}' was not found."));
        }

        List<string> beforeRoles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        List<Role> targetRoles = await _dbContext.Roles
            .Where(r => request.Roles.Contains(r.Name))
            .ToListAsync(ct);

        if (targetRoles.Count != request.Roles.Distinct(StringComparer.Ordinal).Count())
        {
            return Result.Failure<StaffAccountDto>(DomainError.ValidationFailed(
                new Dictionary<string, string[]> { ["roles"] = ["One or more requested roles do not exist."] },
                "One or more requested roles do not exist."));
        }

        _dbContext.UserRoles.RemoveRange(user.UserRoles);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        foreach (Role r in targetRoles)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = r.Id,
                CreatedAt = now,
                Role = r
            });
        }

        _permissionService.InvalidateUserPermissionsCache(user.Id);
        await _dbContext.SaveChangesAsync(ct);

        await _auditLog.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            null,
            "Staff.RolesUpdate",
            "UserRole",
            user.Id.ToString(),
            new { roles = beforeRoles },
            new { roles = request.Roles },
            ipAddress,
            correlationId),
            ct);

        return Result.Success(MapToStaffAccountDto(user));
    }

    public async Task<Result<StaffAccountDto>> UpdateStaffPermissionsAsync(
        Guid id,
        UpdateStaffPermissionsRequest request,
        Guid actorUserId,
        string? ipAddress = null,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        User? user = await _dbContext.Users
            .Include(u => u.StaffProfile)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.UserPermissions)
            .FirstOrDefaultAsync(u => u.Id == id && u.StaffProfile != null, ct);

        if (user is null)
        {
            return Result.Failure<StaffAccountDto>(DomainError.NotFound($"Staff account with ID '{id}' was not found."));
        }

        List<string> beforePermissions = user.UserPermissions.Select(up => up.PermissionCode).ToList();

        IReadOnlyList<string> requested = request.ExtraPermissions ?? [];

        List<Permission> validGrants = [];
        if (requested.Count > 0)
        {
            validGrants = await _dbContext.Permissions
                .Where(p => requested.Contains(p.Code) && p.AllowPerUserGrant)
                .ToListAsync(ct);

            if (validGrants.Count != requested.Distinct(StringComparer.Ordinal).Count())
            {
                return Result.Failure<StaffAccountDto>(DomainError.ValidationFailed(
                    new Dictionary<string, string[]> { ["extraPermissions"] = ["Permission cannot be granted per-user."] },
                    "One or more requested permissions do not exist or do not allow per-user grant."));
            }
        }

        _dbContext.UserPermissions.RemoveRange(user.UserPermissions);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        foreach (Permission p in validGrants)
        {
            user.UserPermissions.Add(new UserPermission
            {
                UserId = user.Id,
                PermissionCode = p.Code,
                GrantedBy = actorUserId,
                GrantedAt = now
            });
        }

        _permissionService.InvalidateUserPermissionsCache(user.Id);
        await _dbContext.SaveChangesAsync(ct);

        await _auditLog.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            null,
            "Staff.PermissionsUpdate",
            "UserPermission",
            user.Id.ToString(),
            new { extraPermissions = beforePermissions },
            new { extraPermissions = requested },
            ipAddress,
            correlationId),
            ct);

        return Result.Success(MapToStaffAccountDto(user));
    }

    public async Task<Result<IReadOnlyList<RolePermissionMatrixDto>>> GetRolesMatrixAsync(
        CancellationToken ct = default)
    {
        List<Role> roles = await _dbContext.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        List<RolePermissionMatrixDto> result = roles.Select(r => new RolePermissionMatrixDto(
            r.Id,
            r.Name,
            IsImmutableAdmin: string.Equals(r.Name, PredefinedRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
                              r.Id == PredefinedRoles.AdminRoleId,
            Permissions: r.RolePermissions.Select(rp => rp.PermissionCode).OrderBy(p => p, StringComparer.Ordinal).ToList()
        )).ToList();

        return Result.Success<IReadOnlyList<RolePermissionMatrixDto>>(result);
    }

    public async Task<Result<IReadOnlyList<PermissionDefinitionDto>>> GetPermissionsCatalogAsync(
        CancellationToken ct = default)
    {
        List<Permission> permissions = await _dbContext.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Group)
            .ThenBy(p => p.Code)
            .ToListAsync(ct);

        List<PermissionDefinitionDto> result = permissions.Select(p => new PermissionDefinitionDto(
            p.Code,
            p.Group,
            p.Description,
            p.AllowPerUserGrant
        )).ToList();

        return Result.Success<IReadOnlyList<PermissionDefinitionDto>>(result);
    }

    public async Task<Result<RolePermissionMatrixDto>> UpdateRolePermissionsAsync(
        Guid roleId,
        UpdateRolePermissionsRequest request,
        Guid actorUserId,
        string? ipAddress = null,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Role? role = await _dbContext.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, ct);

        if (role is null)
        {
            return Result.Failure<RolePermissionMatrixDto>(
                DomainError.NotFound($"Role with ID '{roleId}' was not found."));
        }

        bool isAdmin = string.Equals(role.Name, PredefinedRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
                       role.Id == PredefinedRoles.AdminRoleId;

        IReadOnlyList<string> requested = request.Permissions ?? [];

        // Protection for Admin role: cannot remove any permission from Admin
        if (isAdmin)
        {
            HashSet<string> requestedSet = new(requested, StringComparer.Ordinal);
            bool hasAll = Permissions.All.All(requestedSet.Contains);

            if (!hasAll)
            {
                return Result.Failure<RolePermissionMatrixDto>(
                    DomainError.Conflict(ErrorCodes.Conflict, "Cannot remove permissions from the Admin role."));
            }
        }

        // Validate all requested permissions exist
        List<Permission> validPermissions = await _dbContext.Permissions
            .Where(p => requested.Contains(p.Code))
            .ToListAsync(ct);

        if (validPermissions.Count != requested.Distinct(StringComparer.Ordinal).Count())
        {
            return Result.Failure<RolePermissionMatrixDto>(
                DomainError.ValidationFailed(
                    new Dictionary<string, string[]> { ["permissions"] = ["One or more requested permissions do not exist."] },
                    "One or more requested permissions do not exist."));
        }

        List<string> beforePerms = role.RolePermissions.Select(rp => rp.PermissionCode).ToList();

        _dbContext.RolePermissions.RemoveRange(role.RolePermissions);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        foreach (Permission p in validPermissions)
        {
            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionCode = p.Code,
                CreatedAt = now
            });
        }

        // Evict all cached permissions across the application
        _permissionService.InvalidateAllPermissionsCache();

        await _dbContext.SaveChangesAsync(ct);

        await _auditLog.WriteAsync(new WriteAuditLogCommand(
            actorUserId,
            null,
            "Role.PermissionsUpdate",
            "RolePermission",
            role.Id.ToString(),
            new { permissions = beforePerms },
            new { permissions = requested },
            ipAddress,
            correlationId),
            ct);

        RolePermissionMatrixDto matrixDto = new(
            role.Id,
            role.Name,
            isAdmin,
            role.RolePermissions.Select(rp => rp.PermissionCode).OrderBy(p => p, StringComparer.Ordinal).ToList());

        return Result.Success(matrixDto);
    }

    private static StaffAccountDto MapToStaffAccountDto(User user)
    {
        IReadOnlyList<string> roles = user.UserRoles
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        IReadOnlyList<string> extraPermissions = user.UserPermissions
            .Select(up => up.PermissionCode)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new StaffAccountDto(
            user.Id,
            user.StaffProfile?.EmployeeCode ?? string.Empty,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.StaffProfile?.Position,
            user.Status,
            roles,
            extraPermissions,
            user.StaffProfile?.HiredAt ?? user.CreatedAt);
    }
}
