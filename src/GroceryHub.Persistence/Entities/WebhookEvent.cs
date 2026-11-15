namespace GroceryHub.Persistence.Entities;

public static class WebhookProcessingStatusConstants
{
    public const string Received = "Received";
    public const string Processed = "Processed";
    public const string Ignored = "Ignored";
    public const string Failed = "Failed";

    public static readonly IReadOnlyList<string> All =
    [
        Received,
        Processed,
        Ignored,
        Failed
    ];
}

public class WebhookEvent
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = "SEPAY";

    public string ProviderTransactionId { get; set; } = string.Empty;

    public string RawBody { get; set; } = string.Empty;

    public bool SignatureValid { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public string ProcessingStatus { get; set; } = WebhookProcessingStatusConstants.Received;

    public string? Error { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public int DeliveryCount { get; set; } = 1;

    public PaymentTransaction? Transaction { get; set; }
}
