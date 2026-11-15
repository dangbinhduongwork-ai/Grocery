using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public class StaffProfile : IAuditableEntity
{
    public Guid UserId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public DateTimeOffset HiredAt { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
