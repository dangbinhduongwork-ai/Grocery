using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public class Brand : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? LogoKey { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
