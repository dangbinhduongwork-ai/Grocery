using System.Security.Claims;
using GroceryHub.Modules.Identity.Application;
using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GroceryHub.Modules.Identity.Api;

internal static class AdminStaffEndpoints
{
    public static RouteGroupBuilder MapAdminStaffEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/admin")
            .WithTags("Admin");

        group.MapGet("/staff", async (
            [FromQuery] string? q,
            [FromQuery] string? role,
            [FromQuery] string? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            StaffAdminService staffAdminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Result<PagedResponse<StaffAccountDto>> result = await staffAdminService.GetStaffListAsync(
                q,
                role,
                status,
                page ?? 1,
                pageSize ?? 24,
                ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("GetStaffList")
        .WithSummary("List staff and manager accounts with pagination and filtering")
        .Produces<PagedResponse<StaffAccountDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(Permissions.StaffRead);

        group.MapPost("/staff", async (
            [FromBody] CreateStaffRequest request,
            StaffAdminService staffAdminService,
            IPermissionService permissionService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? actorUserId = GetUserIdFromClaims(context.User);
            if (!actorUserId.HasValue)
            {
                return IdentityResults.Problem(context, DomainError.Unauthorized());
            }

            Result<IReadOnlySet<string>> permsResult = await permissionService.GetEffectivePermissionsAsync(actorUserId.Value, ct);
            IReadOnlySet<string> actorPermissions = permsResult.IsSuccess ? permsResult.Value : new HashSet<string>();

            string? ip = context.Connection.RemoteIpAddress?.ToString();
            string? correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? context.TraceIdentifier;

            Result<StaffAccountDto> result = await staffAdminService.CreateStaffAsync(
                request,
                actorUserId.Value,
                actorPermissions,
                ip,
                correlationId,
                ct);

            return result.IsSuccess
                ? TypedResults.Created($"/api/v1/admin/staff/{result.Value.UserId}", result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("CreateStaff")
        .WithSummary("Create a new staff or manager account")
        .Produces<StaffAccountDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(DynamicPermissionPolicyProvider.StaffCreateOrManagePolicy);

        group.MapGet("/staff/{id:guid}", async (
            [FromRoute] Guid id,
            StaffAdminService staffAdminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Result<StaffAccountDto> result = await staffAdminService.GetStaffByIdAsync(id, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("GetStaffById")
        .WithSummary("Retrieve details of a staff or manager account")
        .Produces<StaffAccountDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(Permissions.StaffRead);

        group.MapPut("/staff/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateStaffRequest request,
            StaffAdminService staffAdminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? actorUserId = GetUserIdFromClaims(context.User);
            if (!actorUserId.HasValue)
            {
                return IdentityResults.Problem(context, DomainError.Unauthorized());
            }

            string? ip = context.Connection.RemoteIpAddress?.ToString();
            string? correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? context.TraceIdentifier;

            Result<StaffAccountDto> result = await staffAdminService.UpdateStaffAsync(
                id,
                request,
                actorUserId.Value,
                ip,
                correlationId,
                ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("UpdateStaff")
        .WithSummary("Update staff profile details and status")
        .Produces<StaffAccountDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(Permissions.StaffManage);

        group.MapPut("/staff/{id:guid}/roles", async (
            [FromRoute] Guid id,
            [FromBody] UpdateStaffRolesRequest request,
            StaffAdminService staffAdminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? actorUserId = GetUserIdFromClaims(context.User);
            if (!actorUserId.HasValue)
            {
                return IdentityResults.Problem(context, DomainError.Unauthorized());
            }

            string? ip = context.Connection.RemoteIpAddress?.ToString();
            string? correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? context.TraceIdentifier;

            Result<StaffAccountDto> result = await staffAdminService.UpdateStaffRolesAsync(
                id,
                request,
                actorUserId.Value,
                ip,
                correlationId,
                ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("UpdateStaffRoles")
        .WithSummary("Update assigned roles for a staff account")
        .Produces<StaffAccountDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(Permissions.StaffManage);

        group.MapPut("/staff/{id:guid}/permissions", async (
            [FromRoute] Guid id,
            [FromBody] UpdateStaffPermissionsRequest request,
            StaffAdminService staffAdminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? actorUserId = GetUserIdFromClaims(context.User);
            if (!actorUserId.HasValue)
            {
                return IdentityResults.Problem(context, DomainError.Unauthorized());
            }

            string? ip = context.Connection.RemoteIpAddress?.ToString();
            string? correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? context.TraceIdentifier;

            Result<StaffAccountDto> result = await staffAdminService.UpdateStaffPermissionsAsync(
                id,
                request,
                actorUserId.Value,
                ip,
                correlationId,
                ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("UpdateStaffPermissions")
        .WithSummary("Update extra granted permissions for a staff account")
        .Produces<StaffAccountDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(Permissions.StaffManage);

        group.MapGet("/roles", async (
            StaffAdminService staffAdminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Result<IReadOnlyList<RolePermissionMatrixDto>> result = await staffAdminService.GetRolesMatrixAsync(ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("GetRoles")
        .WithSummary("List all system roles and their assigned permissions")
        .Produces<IReadOnlyList<RolePermissionMatrixDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(Permissions.RolesManage);

        group.MapGet("/permissions", async (
            StaffAdminService staffAdminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Result<IReadOnlyList<PermissionDefinitionDto>> result = await staffAdminService.GetPermissionsCatalogAsync(ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("GetPermissions")
        .WithSummary("List all system permissions catalog")
        .Produces<IReadOnlyList<PermissionDefinitionDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(Permissions.RolesManage);

        group.MapPut("/roles/{id:guid}/permissions", async (
            [FromRoute] Guid id,
            [FromBody] UpdateRolePermissionsRequest request,
            StaffAdminService staffAdminService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? actorUserId = GetUserIdFromClaims(context.User);
            if (!actorUserId.HasValue)
            {
                return IdentityResults.Problem(context, DomainError.Unauthorized());
            }

            string? ip = context.Connection.RemoteIpAddress?.ToString();
            string? correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? context.TraceIdentifier;

            Result<RolePermissionMatrixDto> result = await staffAdminService.UpdateRolePermissionsAsync(
                id,
                request,
                actorUserId.Value,
                ip,
                correlationId,
                ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("UpdateRolePermissions")
        .WithSummary("Update permissions for a role (Admin role cannot lose permissions)")
        .Produces<RolePermissionMatrixDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(Permissions.RolesManage);

        return group;
    }

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        string? sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(sub, out Guid id) ? id : null;
    }
}
