namespace GroceryHub.Modules.Audit.Contracts;

public sealed record AuditLogDto(
    long Id,
    DateTimeOffset At,
    Guid? ActorUserId,
    string? ActorRole,
    string Action,
    string EntityType,
    string EntityId,
    string? BeforeJson,
    string? AfterJson,
    string? Ip,
    string? CorrelationId);
