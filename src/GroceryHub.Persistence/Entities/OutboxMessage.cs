namespace GroceryHub.Persistence.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public string? CorrelationId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}
