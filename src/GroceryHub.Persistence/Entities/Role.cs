using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public static class PredefinedRoles
{
    public static readonly Guid AdminRoleId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ManagerRoleId = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid StaffRoleId = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid CustomerRoleId = new("44444444-4444-4444-4444-444444444444");

    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
    public const string Customer = "Customer";

    public static readonly IReadOnlyList<string> All = [Admin, Manager, Staff, Customer];
}

public class Role : IAuditableEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? NormalizedName { get; set; }

    public string? Description { get; set; }

    public bool IsSystem { get; set; } = true;

    public string? ConcurrencyStamp { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
