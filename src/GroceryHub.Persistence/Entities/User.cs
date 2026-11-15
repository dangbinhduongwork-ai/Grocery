using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public static class UserStatus
{
    public const string Active = "Active";
    public const string Locked = "Locked";
    public const string Disabled = "Disabled";

    public static readonly IReadOnlyList<string> All = [Active, Locked, Disabled];
}

public class User : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string? NormalizedUserName { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public bool EmailConfirmed { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public string Status { get; set; } = UserStatus.Active;

    public DateTimeOffset? LockoutEnd { get; set; }

    public bool LockoutEnabled { get; set; } = true;

    public int AccessFailedCount { get; set; }

    public string? SecurityStamp { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public bool ForcePasswordChange { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public StaffProfile? StaffProfile { get; set; }
}
