namespace GroceryHub.Persistence.Entities;

public static class PaymentAttemptStatusConstants
{
    public const string Open = "Open";
    public const string Paid = "Paid";
    public const string Expired = "Expired";
    public const string Superseded = "Superseded";

    public static readonly IReadOnlyList<string> All =
    [
        Open,
        Paid,
        Expired,
        Superseded
    ];
}

public class PaymentAttempt
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public int AttemptNumber { get; set; }

    public long Amount { get; set; }

    public string Status { get; set; } = PaymentAttemptStatusConstants.Open;

    public DateTimeOffset CreatedAt { get; set; }

    public Payment? Payment { get; set; }
}
