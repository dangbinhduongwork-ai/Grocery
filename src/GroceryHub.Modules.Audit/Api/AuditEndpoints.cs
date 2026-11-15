using GroceryHub.Modules.Audit.Contracts;
using GroceryHub.Persistence;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Audit.Api;

internal static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/admin/audit-logs")
            .WithTags("Admin Audit")
            .RequireAuthorization("audit.read");

        // GET /api/v1/admin/audit-logs
        group.MapGet("/", async (
            GroceryHubDbContext db,
            [FromQuery] Guid? actorUserId,
            [FromQuery] string? entityType,
            [FromQuery] string? entityId,
            [FromQuery] string? action,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default) =>
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var query = db.AuditLogs.AsNoTracking();

            if (actorUserId.HasValue)
            {
                query = query.Where(a => a.ActorUserId == actorUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            if (!string.IsNullOrWhiteSpace(entityId))
            {
                query = query.Where(a => a.EntityId == entityId);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(a => a.Action == action);
            }

            if (from.HasValue)
            {
                query = query.Where(a => a.At >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(a => a.At <= to.Value);
            }

            int totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(a => a.At)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogDto(
                    a.Id,
                    a.At,
                    a.ActorUserId,
                    a.ActorRole,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.BeforeJson,
                    a.AfterJson,
                    a.Ip,
                    a.CorrelationId))
                .ToListAsync(ct);

            var paged = PagedResponse<AuditLogDto>.Create(
                items,
                page,
                pageSize,
                totalCount);

            return TypedResults.Ok(paged);
        })
        .WithName("GetAuditLogs")
        .WithSummary("Query security and business audit logs");

        return endpoints;
    }
}
