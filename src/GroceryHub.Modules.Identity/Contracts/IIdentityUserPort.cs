using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Identity.Contracts;

public enum AccountStatus
{
    Active = 1,
    Locked = 2,
    Disabled = 3
}

public sealed record UserIdentitySummaryDto(
    Guid UserId,
    string Email,
    string FullName,
    string? PhoneNumber,
    AccountStatus Status,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeletedAt);

public sealed record LockUserAccountCommand(
    Guid UserId,
    bool IsLocked,
    string Reason,
    Guid ActorUserId);

public sealed record AnonymizeUserIdentityCommand(
    Guid UserId,
    string Reason,
    Guid ActorUserId);

public interface IIdentityUserPort
{
    Task<Result<UserIdentitySummaryDto>> GetUserSummaryAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<Result> SetCustomerLockStateAsync(
        LockUserAccountCommand command,
        CancellationToken ct = default);

    Task<Result> AnonymizeCustomerIdentityAsync(
        AnonymizeUserIdentityCommand command,
        CancellationToken ct = default);
}
