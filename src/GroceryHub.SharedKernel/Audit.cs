namespace GroceryHub.SharedKernel;

public sealed record WriteAuditLogCommand(
    Guid? ActorUserId,
    string? ActorRole,
    string Action,
    string EntityType,
    string EntityId,
    object? BeforeState,
    object? AfterState,
    string? IpAddress = null,
    string? CorrelationId = null);

public interface IAuditLogPort
{
    Task<Result> WriteAsync(
        WriteAuditLogCommand command,
        CancellationToken ct = default);
}
