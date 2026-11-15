namespace GroceryHub.Persistence.Entities;

public static class MatchStatusConstants
{
    public const string Matched = "Matched";
    public const string UnknownCode = "UnknownCode";
    public const string WrongAccount = "WrongAccount";
    public const string AmountMismatch = "AmountMismatch";
    public const string Underpaid = "Underpaid";
    public const string Overpaid = "Overpaid";
    public const string LateAfterExpiry = "LateAfterExpiry";
    public const string DuplicatePayment = "DuplicatePayment";
    public const string OutgoingIgnored = "OutgoingIgnored";

    public static readonly IReadOnlyList<string> All =
    [
        Matched,
        UnknownCode,
        WrongAccount,
        AmountMismatch,
        Underpaid,
        Overpaid,
        LateAfterExpiry,
        DuplicatePayment,
        OutgoingIgnored
    ];
}

public static class PaymentReviewStatusConstants
{
    public const string None = "None";
    public const string NeedsReview = "NeedsReview";
    public const string Resolved = "Resolved";

    public static readonly IReadOnlyList<string> All =
    [
        None,
        NeedsReview,
        Resolved
    ];
}

public class PaymentTransaction
{
    public Guid Id { get; set; }

    public Guid WebhookEventId { get; set; }

    public Guid? PaymentAttemptId { get; set; }

    public string Direction { get; set; } = "in";

    public long Amount { get; set; }

    public string AccountNumber { get; set; } = string.Empty;

    public string Gateway { get; set; } = string.Empty;

    public string? ReferenceCode { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset TransactionTime { get; set; }

    public string MatchStatus { get; set; } = MatchStatusConstants.Matched;

    public string ReviewStatus { get; set; } = PaymentReviewStatusConstants.None;

    public Guid? ResolvedBy { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public string? ResolutionNote { get; set; }

    public WebhookEvent? WebhookEvent { get; set; }

    public PaymentAttempt? PaymentAttempt { get; set; }

    public User? Resolver { get; set; }
}
