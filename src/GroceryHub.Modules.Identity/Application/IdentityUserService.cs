using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Identity.Application;

public sealed class IdentityUserService : IIdentityUserPort
{
    private readonly GroceryHubDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IPermissionService _permissionService;
    private readonly IAuditLogPort _auditLog;
    private readonly TimeProvider _timeProvider;

    public IdentityUserService(
        GroceryHubDbContext dbContext,
        ITokenService tokenService,
        IPermissionService permissionService,
        IAuditLogPort auditLog,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<UserIdentitySummaryDto>> GetUserSummaryAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        User? user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return Result.Failure<UserIdentitySummaryDto>(
                DomainError.NotFound($"User with ID '{userId}' was not found."));
        }

        AccountStatus accountStatus = user.Status switch
        {
            UserStatus.Active => AccountStatus.Active,
            UserStatus.Locked => AccountStatus.Locked,
            UserStatus.Disabled => AccountStatus.Disabled,
            _ => AccountStatus.Active
        };

        IReadOnlyList<string> roles = user.UserRoles
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        UserIdentitySummaryDto dto = new(
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            accountStatus,
            user.EmailConfirmed,
            roles,
            user.CreatedAt,
            user.DeletedAt);

        return Result.Success(dto);
    }

    public async Task<Result> SetCustomerLockStateAsync(
        LockUserAccountCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        User? user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user is null)
        {
            return Result.Failure(DomainError.NotFound($"User with ID '{command.UserId}' was not found."));
        }

        string previousStatus = user.Status;
        string newStatus = command.IsLocked ? UserStatus.Locked : UserStatus.Active;

        if (string.Equals(previousStatus, newStatus, StringComparison.Ordinal))
        {
            return Result.Success();
        }

        user.Status = newStatus;
        if (command.IsLocked)
        {
            user.LockoutEnd = _timeProvider.GetUtcNow().AddYears(100);
            await _tokenService.RevokeAllUserTokensAsync(command.UserId, ct);
            _permissionService.InvalidateUserPermissionsCache(command.UserId);
        }
        else
        {
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            _permissionService.InvalidateUserPermissionsCache(command.UserId);
        }

        await _dbContext.SaveChangesAsync(ct);

        await _auditLog.WriteAsync(new WriteAuditLogCommand(
            command.ActorUserId,
            null,
            command.IsLocked ? "User.Lock" : "User.Unlock",
            "User",
            command.UserId.ToString(),
            new { status = previousStatus },
            new { status = newStatus, reason = command.Reason }),
            ct);

        return Result.Success();
    }

    public async Task<Result> AnonymizeCustomerIdentityAsync(
        AnonymizeUserIdentityCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        User? user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, ct);

        if (user is null)
        {
            return Result.Failure(DomainError.NotFound($"User with ID '{command.UserId}' was not found."));
        }

        string originalEmail = user.Email;
        string originalName = user.FullName;

        string anonymizedToken = command.UserId.ToString("N");
        user.Email = $"anonymized-{anonymizedToken}@groceryhub.invalid";
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        user.FullName = "Người dùng đã xóa";
        user.PhoneNumber = null;
        user.UserName = $"anonymized-{anonymizedToken}";
        user.NormalizedUserName = user.UserName.ToUpperInvariant();
        user.Status = UserStatus.Disabled;
        user.DeletedAt = _timeProvider.GetUtcNow();

        await _tokenService.RevokeAllUserTokensAsync(command.UserId, ct);
        _permissionService.InvalidateUserPermissionsCache(command.UserId);

        await _dbContext.SaveChangesAsync(ct);

        await _auditLog.WriteAsync(new WriteAuditLogCommand(
            command.ActorUserId,
            null,
            "User.Anonymize",
            "User",
            command.UserId.ToString(),
            new { email = originalEmail, fullName = originalName },
            new { status = user.Status, reason = command.Reason }),
            ct);

        return Result.Success();
    }
}
