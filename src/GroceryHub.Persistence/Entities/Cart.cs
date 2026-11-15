using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public class Cart : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string? GuestCartToken { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public User? User { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
