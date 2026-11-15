using System.Security.Claims;
using FluentValidation;
using GroceryHub.Modules.Inventory.Application.DTOs;
using GroceryHub.Modules.Inventory.Application.Services;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GroceryHub.Modules.Inventory.Api;

public static class InventoryEndpoints
{
    private const string ReadPolicy = "inventory.read";
    private const string AdjustPolicy = "inventory.adjust";
    private const string CostReadPolicy = "catalog.cost.read";

    public static IEndpointRouteBuilder MapInventoryApiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/inventory")
            .WithTags("Admin Inventory");

        // GET /api/v1/admin/inventory
        group.MapGet("/", async (
            string? q,
            Guid? categoryId,
            bool? lowStockOnly,
            bool? outOfStockOnly,
            int? page,
            int? pageSize,
            InventoryAdminService service,
            IAuthorizationService authService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            bool canReadCost = await CanReadCostAsync(authService, user);
            var result = await service.GetInventoryAsync(
                q,
                categoryId,
                lowStockOnly,
                outOfStockOnly,
                page ?? 1,
                pageSize ?? 20,
                canReadCost,
                ct);

            return Results.Ok(result.Value);
        })
        .RequireAuthorization(ReadPolicy)
        .WithName("GetAdminInventory")
        .WithSummary("List inventory items with stock levels")
        .Produces<PagedResponse<AdminStockItemDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // GET /admin/inventory/low-stock
        group.MapGet("/low-stock", async (
            int? page,
            int? pageSize,
            InventoryAdminService service,
            IAuthorizationService authService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            bool canReadCost = await CanReadCostAsync(authService, user);
            var result = await service.GetLowStockAsync(
                page ?? 1,
                pageSize ?? 20,
                canReadCost,
                ct);

            return Results.Ok(result.Value);
        })
        .RequireAuthorization(ReadPolicy)
        .WithName("GetAdminLowStock")
        .WithSummary("List low stock inventory items")
        .Produces<PagedResponse<AdminStockItemDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // GET /admin/inventory/near-expiry
        group.MapGet("/near-expiry", async (
            int? withinDays,
            int? page,
            int? pageSize,
            InventoryAdminService service,
            CancellationToken ct) =>
        {
            var result = await service.GetNearExpiryAsync(
                withinDays ?? 14,
                page ?? 1,
                pageSize ?? 20,
                ct);

            return Results.Ok(result.Value);
        })
        .RequireAuthorization(ReadPolicy)
        .WithName("GetAdminNearExpiry")
        .WithSummary("List near-expiry inventory items")
        .Produces<PagedResponse<ExpiryLotDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // GET /api/v1/admin/inventory/movements
        group.MapGet("/movements", async (
            Guid? productId,
            string? type,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int? page,
            int? pageSize,
            InventoryAdminService service,
            IAuthorizationService authService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            bool canReadCost = await CanReadCostAsync(authService, user);
            var result = await service.GetMovementsAsync(
                productId,
                type,
                from,
                to,
                page ?? 1,
                pageSize ?? 20,
                canReadCost,
                ct);

            return Results.Ok(result.Value);
        })
        .RequireAuthorization(ReadPolicy)
        .WithName("GetAdminInventoryMovements")
        .WithSummary("Query stock movement ledger")
        .Produces<PagedResponse<StockMovementDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // GET /api/v1/admin/inventory/{productId}/movements
        group.MapGet("/{productId:guid}/movements", async (
            Guid productId,
            string? type,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int? page,
            int? pageSize,
            InventoryAdminService service,
            IAuthorizationService authService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            bool canReadCost = await CanReadCostAsync(authService, user);
            var result = await service.GetMovementsAsync(
                productId,
                type,
                from,
                to,
                page ?? 1,
                pageSize ?? 20,
                canReadCost,
                ct);

            return Results.Ok(result.Value);
        })
        .RequireAuthorization(ReadPolicy)
        .WithName("GetAdminProductMovements")
        .WithSummary("Query product stock movement ledger")
        .Produces<PagedResponse<StockMovementDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // POST /api/v1/admin/inventory/adjust
        group.MapPost("/adjust", async (
            AdminAdjustStockRequest request,
            IValidator<AdminAdjustStockRequest> validator,
            InventoryAdminService service,
            IAuthorizationService authService,
            ClaimsPrincipal user,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var valResult = await validator.ValidateAsync(request, ct);
            if (!valResult.IsValid)
            {
                return ToValidationProblem(httpContext, valResult);
            }

            Guid? actorUserId = GetUserId(user);
            string? actorRole = user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role");
            string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
            string? traceId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? httpContext.TraceIdentifier;

            bool canReadCost = await CanReadCostAsync(authService, user);

            var result = await service.AdjustStockAsync(
                request.ProductId,
                request.Delta,
                request.Reason,
                actorUserId,
                actorRole,
                ip,
                traceId,
                canReadCost,
                ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(AdjustPolicy)
        .WithName("AdjustInventoryStock")
        .WithSummary("Adjust product stock on hand")
        .Produces<AdminStockItemDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

        // POST /api/v1/admin/inventory/{productId}/adjust
        group.MapPost("/{productId:guid}/adjust", async (
            Guid productId,
            AdjustStockRequest request,
            IValidator<AdjustStockRequest> validator,
            InventoryAdminService service,
            IAuthorizationService authService,
            ClaimsPrincipal user,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var valResult = await validator.ValidateAsync(request, ct);
            if (!valResult.IsValid)
            {
                return ToValidationProblem(httpContext, valResult);
            }

            Guid? actorUserId = GetUserId(user);
            string? actorRole = user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role");
            string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
            string? traceId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? httpContext.TraceIdentifier;

            bool canReadCost = await CanReadCostAsync(authService, user);

            var result = await service.AdjustStockAsync(
                productId,
                request.Delta,
                request.Reason,
                actorUserId,
                actorRole,
                ip,
                traceId,
                canReadCost,
                ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(AdjustPolicy)
        .WithName("AdjustProductStock")
        .WithSummary("Adjust stock on hand for a specific product")
        .Produces<AdminStockItemDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

        // PUT /api/v1/admin/inventory/{productId}/reorder-level
        group.MapPut("/{productId:guid}/reorder-level", async (
            Guid productId,
            UpdateReorderLevelRequest request,
            IValidator<UpdateReorderLevelRequest> validator,
            InventoryAdminService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var valResult = await validator.ValidateAsync(request, ct);
            if (!valResult.IsValid)
            {
                return ToValidationProblem(httpContext, valResult);
            }

            var result = await service.UpdateReorderLevelAsync(productId, request.ReorderLevel, ct);
            return result.IsSuccess
                ? Results.NoContent()
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(AdjustPolicy)
        .WithName("UpdateProductReorderLevel")
        .WithSummary("Update reorder level threshold for product")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<bool> CanReadCostAsync(IAuthorizationService authService, ClaimsPrincipal user)
    {
        var authResult = await authService.AuthorizeAsync(user, CostReadPolicy);
        return authResult.Succeeded;
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        string? val = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(val, out Guid id) ? id : null;
    }

    private static IResult ToProblem(HttpContext context, DomainError error)
    {
        return InventoryResults.Problem(context, error);
    }

    private static IResult ToValidationProblem(HttpContext context, FluentValidation.Results.ValidationResult validation)
    {
        var dict = validation.Errors
            .GroupBy(static e => e.PropertyName)
            .ToDictionary(
                static g => g.Key,
                static g => g.Select(static e => e.ErrorMessage).ToArray());

        DomainError domainError = DomainError.Validation(ErrorCodes.ValidationFailed, "Validation failed.", dict);
        return InventoryResults.Problem(context, domainError);
    }
}
