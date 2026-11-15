using System.Text.Json;
using System.Text.Json.Serialization;
using GroceryHub.Modules.Audit.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Audit.Application;

public sealed class AuditLogService : IAuditModuleLogPort, IAuditLogPort
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly GroceryHubDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public AuditLogService(GroceryHubDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result> WriteAsync(WriteAuditLogCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        string? beforeJson = SerializeState(command.BeforeState);
        string? afterJson = SerializeState(command.AfterState);

        AuditLog auditLog = new()
        {
            At = _timeProvider.GetUtcNow(),
            ActorUserId = command.ActorUserId,
            ActorRole = command.ActorRole,
            Action = command.Action ?? string.Empty,
            EntityType = command.EntityType ?? string.Empty,
            EntityId = command.EntityId ?? string.Empty,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            Ip = command.IpAddress,
            CorrelationId = command.CorrelationId
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    private static string? SerializeState(object? state)
    {
        if (state is null)
        {
            return null;
        }

        if (state is string str)
        {
            return str;
        }

        return JsonSerializer.Serialize(state, JsonOptions);
    }
}
