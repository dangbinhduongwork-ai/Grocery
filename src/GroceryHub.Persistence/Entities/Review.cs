using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public static class ReviewStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static readonly IReadOnlyList<string> All = [Pending, Approved, Rejected];
}

public class Review : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid UserId { get; set; }

    public Guid OrderItemId { get; set; }

    public int Rating { get; set; }

    public string? Title { get; set; }

    public string Body { get; set; } = string.Empty;

    public string Status { get; set; } = ReviewStatus.Pending;

    public Guid? ModeratedBy { get; set; }

    public DateTimeOffset? ModeratedAt { get; set; }

    public string? RejectionReason { get; set; }

    public string? ReplyBody { get; set; }

    public Guid? ReplyBy { get; set; }

    public DateTimeOffset? ReplyAt { get; set; }

    public DateTimeOffset? EditedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Product? Product { get; set; }

    public User? User { get; set; }

    public OrderItem? OrderItem { get; set; }

    public User? Moderator { get; set; }

    public User? Replier { get; set; }
}
