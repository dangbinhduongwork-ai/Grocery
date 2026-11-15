namespace GroceryHub.Persistence.Entities;

public static class RefundStatusConstants
{
    public const string Requested = "Requested";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Completed = "Completed";

    public static readonly IReadOnlyList<string> All =
    [
        Requested,
        Approved,
        Rejected,
        Completed
    ];
}

public class Refund
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public Guid OrderId { get; set; }

    public long Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = RefundStatusConstants.Requested;

    public Guid RequestedBy { get; set; }

    public DateTimeOffset RequestedAt { get; set; }

    public Guid? DecidedBy { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }

    public Guid? CompletedBy { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? BankReference { get; set; }

    public string? Note { get; set; }

    public Payment? Payment { get; set; }

    public Order? Order { get; set; }

    public User? Requester { get; set; }

    public User? Decider { get; set; }

    public User? Completer { get; set; }
}
