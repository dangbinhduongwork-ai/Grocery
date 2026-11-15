namespace GroceryHub.Persistence.Entities;

public class AuditLog
{
    public long Id { get; set; }

    public DateTimeOffset At { get; set; }

    public Guid? ActorUserId { get; set; }

    public string? ActorRole { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }

    public string? Ip { get; set; }

    public string? CorrelationId { get; set; }
}
