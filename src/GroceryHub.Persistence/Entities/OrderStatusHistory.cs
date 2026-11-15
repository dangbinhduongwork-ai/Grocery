namespace GroceryHub.Persistence.Entities;

public class OrderStatusHistory
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string? FromStatus { get; set; }

    public string ToStatus { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public Guid? ActorUserId { get; set; }

    public string? ActorRole { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Order? Order { get; set; }

    public User? ActorUser { get; set; }
}
