namespace GroceryHub.Persistence.Entities;

public class UserPermission
{
    public Guid UserId { get; set; }

    public string PermissionCode { get; set; } = string.Empty;

    public Guid GrantedBy { get; set; }

    public DateTimeOffset GrantedAt { get; set; }

    public User User { get; set; } = null!;

    public Permission Permission { get; set; } = null!;

    public User? Granter { get; set; }
}
