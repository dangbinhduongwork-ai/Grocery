namespace GroceryHub.Persistence.Entities;

public class RolePermission
{
    public Guid RoleId { get; set; }

    public string PermissionCode { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public Role Role { get; set; } = null!;

    public Permission Permission { get; set; } = null!;
}
