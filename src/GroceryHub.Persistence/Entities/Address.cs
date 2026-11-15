using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public class Address : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string RecipientName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string ProvinceCode { get; set; } = string.Empty;

    public string WardCode { get; set; } = string.Empty;

    public string ProvinceName { get; set; } = string.Empty;

    public string WardName { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string? Note { get; set; }

    public bool IsDefault { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public User? User { get; set; }
}
