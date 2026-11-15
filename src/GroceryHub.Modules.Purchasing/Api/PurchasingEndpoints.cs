using System.Security.Claims;
using FluentValidation;
using GroceryHub.Modules.Purchasing.Application.DTOs;
using GroceryHub.Modules.Purchasing.Application.Services;
using GroceryHub.Persistence.Services;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GroceryHub.Modules.Purchasing.Api;

public static class PurchasingEndpoints
{
    private const string ReadPolicy = "purchasing.read";
    private const string WritePolicy = "purchasing.write";
    private const string ReceivePolicy = "purchasing.receive";

    public static IEndpointRouteBuilder MapPurchasingApiEndpoints(this IEndpointRouteBuilder app)
    {
        MapSupplierEndpoints(app, "/admin/suppliers");
        MapSupplierEndpoints(app, "/admin/purchasing/suppliers");

        MapPurchaseOrderEndpoints(app, "/admin/purchase-orders");
        MapPurchaseOrderEndpoints(app, "/admin/purchasing/orders");

        MapGoodsReceiptEndpoints(app, "/admin/goods-receipts");

        return app;
    }

    private static void MapSupplierEndpoints(IEndpointRouteBuilder app, string prefix)
    {
        var group = app.MapGroup(prefix)
            .WithTags("Admin Suppliers");

        // GET
        group.MapGet("/", async (
            string? q,
            bool? isActive,
            int? page,
            int? pageSize,
            PurchasingAdminService service,
            CancellationToken ct) =>
        {
            var result = await service.GetSuppliersAsync(q, isActive, page ?? 1, pageSize ?? 20, ct);
            return Results.Ok(result.Value);
        })
        .RequireAuthorization(ReadPolicy)
        .WithSummary("List suppliers with search and pagination")
        .Produces<PagedResponse<SupplierDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // GET /{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            PurchasingAdminService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var result = await service.GetSupplierByIdAsync(id, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(ReadPolicy)
        .WithSummary("Get supplier detail by ID")
        .Produces<SupplierDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // POST /
        group.MapPost("/", async (
            CreateSupplierRequest request,
            IValidator<CreateSupplierRequest> validator,
            PurchasingAdminService service,
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

            var result = await service.CreateSupplierAsync(request, actorUserId, actorRole, ip, traceId, ct);
            return result.IsSuccess
                ? Results.Created($"{prefix}/{result.Value.Id}", result.Value)
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(WritePolicy)
        .WithSummary("Create a new supplier")
        .Produces<SupplierDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

        // PUT /{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateSupplierRequest request,
            IValidator<UpdateSupplierRequest> validator,
            PurchasingAdminService service,
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

            var result = await service.UpdateSupplierAsync(id, request, actorUserId, actorRole, ip, traceId, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(WritePolicy)
        .WithSummary("Update supplier details")
        .Produces<SupplierDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

        // DELETE /{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            PurchasingAdminService service,
            ClaimsPrincipal user,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            Guid? actorUserId = GetUserId(user);
            string? actorRole = user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role");
            string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
            string? traceId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? httpContext.TraceIdentifier;

            var result = await service.DeleteSupplierAsync(id, actorUserId, actorRole, ip, traceId, ct);
            return result.IsSuccess
                ? Results.NoContent()
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(WritePolicy)
        .WithSummary("Soft-delete a supplier")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapPurchaseOrderEndpoints(IEndpointRouteBuilder app, string prefix)
    {
        var group = app.MapGroup(prefix)
            .WithTags("Admin Purchase Orders");

        // GET /
        group.MapGet("/", async (
            string? status,
            Guid? supplierId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int? page,
            int? pageSize,
            PurchasingAdminService service,
            CancellationToken ct) =>
        {
            var result = await service.GetPurchaseOrdersAsync(status, supplierId, from, to, page ?? 1, pageSize ?? 20, ct);
            return Results.Ok(result.Value);
        })
        .RequireAuthorization(ReadPolicy)
        .WithSummary("List purchase orders with filters and pagination")
        .Produces<PagedResponse<PurchaseOrderSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // GET /{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            PurchasingAdminService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var result = await service.GetPurchaseOrderByIdAsync(id, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(ReadPolicy)
        .WithSummary("Get purchase order detail with items")
        .Produces<PurchaseOrderDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // POST /
        group.MapPost("/", async (
            CreatePurchaseOrderRequest request,
            IValidator<CreatePurchaseOrderRequest> validator,
            PurchasingAdminService service,
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
            if (!actorUserId.HasValue || actorUserId.Value == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            string? actorRole = user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role");
            string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
            string? traceId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? httpContext.TraceIdentifier;

            var result = await service.CreatePurchaseOrderAsync(request, actorUserId.Value, actorUserId, actorRole, ip, traceId, ct);
            return result.IsSuccess
                ? Results.Created($"{prefix}/{result.Value.Id}", result.Value)
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(WritePolicy)
        .WithSummary("Create purchase order in Draft state")
        .Produces<PurchaseOrderDetailDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

        // PUT /{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdatePurchaseOrderRequest request,
            IValidator<UpdatePurchaseOrderRequest> validator,
            PurchasingAdminService service,
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

            var result = await service.UpdatePurchaseOrderAsync(id, request, actorUserId, actorRole, ip, traceId, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(WritePolicy)
        .WithSummary("Update Draft purchase order items and note")
        .Produces<PurchaseOrderDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

        // POST /{id}/submit and /{id}/order
        group.MapPost("/{id:guid}/submit", SubmitOrderHandler)
            .RequireAuthorization(WritePolicy)
            .WithSummary("Submit purchase order to supplier (Draft -> Submitted)")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/order", SubmitOrderHandler)
            .RequireAuthorization(WritePolicy)
            .WithSummary("Submit purchase order to supplier (Draft -> Submitted/Ordered)")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // POST /{id}/cancel
        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            CancelPurchaseOrderRequest? request,
            PurchasingAdminService service,
            ClaimsPrincipal user,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            Guid? actorUserId = GetUserId(user);
            string? actorRole = user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role");
            string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
            string? traceId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? httpContext.TraceIdentifier;

            var result = await service.CancelPurchaseOrderAsync(id, request?.Reason, actorUserId, actorRole, ip, traceId, ct);
            return result.IsSuccess
                ? Results.Ok()
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(WritePolicy)
        .WithSummary("Cancel purchase order before goods receipt")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        // POST /{id}/receive
        group.MapPost("/{id:guid}/receive", async (
            Guid id,
            ReceivePurchaseOrderRequest request,
            IValidator<ReceivePurchaseOrderRequest> validator,
            PurchasingAdminService service,
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
            if (!actorUserId.HasValue || actorUserId.Value == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            string? actorRole = user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role");
            string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
            string? traceId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? httpContext.TraceIdentifier;

            var result = await service.ReceivePurchaseOrderAsync(
                id,
                request,
                actorUserId.Value,
                actorUserId,
                actorRole,
                ip,
                traceId,
                ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(ReceivePolicy)
        .WithSummary("Receive goods for purchase order intake")
        .Produces<GoodsReceiptIntakeResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

        // POST /{id}/close
        group.MapPost("/{id:guid}/close", async (
            Guid id,
            PurchasingAdminService service,
            ClaimsPrincipal user,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            Guid? actorUserId = GetUserId(user);
            string? actorRole = user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role");
            string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
            string? traceId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? httpContext.TraceIdentifier;

            var result = await service.ClosePurchaseOrderAsync(id, actorUserId, actorRole, ip, traceId, ct);
            return result.IsSuccess
                ? Results.Ok()
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(WritePolicy)
        .WithSummary("Close received purchase order")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> SubmitOrderHandler(
        Guid id,
        PurchasingAdminService service,
        ClaimsPrincipal user,
        HttpContext httpContext,
        CancellationToken ct)
    {
        Guid? actorUserId = GetUserId(user);
        string? actorRole = user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role");
        string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
        string? traceId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? httpContext.TraceIdentifier;

        var result = await service.SubmitPurchaseOrderAsync(id, actorUserId, actorRole, ip, traceId, ct);
        return result.IsSuccess
            ? Results.Ok()
            : ToProblem(httpContext, result.Error);
    }

    private static void MapGoodsReceiptEndpoints(IEndpointRouteBuilder app, string prefix)
    {
        var group = app.MapGroup(prefix)
            .WithTags("Admin Goods Receipts");

        // GET /
        group.MapGet("/", async (
            Guid? purchaseOrderId,
            int? page,
            int? pageSize,
            PurchasingAdminService service,
            CancellationToken ct) =>
        {
            var result = await service.GetGoodsReceiptsAsync(purchaseOrderId, page ?? 1, pageSize ?? 20, ct);
            return Results.Ok(result.Value);
        })
        .RequireAuthorization(ReadPolicy)
        .WithSummary("List goods receipts")
        .Produces<PagedResponse<GoodsReceiptSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // GET /{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            PurchasingAdminService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var result = await service.GetGoodsReceiptByIdAsync(id, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(ReadPolicy)
        .WithSummary("Get goods receipt detail by ID")
        .Produces<GoodsReceiptDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // POST /
        group.MapPost("/", async (
            CreateGoodsReceiptRequest request,
            IValidator<CreateGoodsReceiptRequest> validator,
            PurchasingAdminService service,
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
            if (!actorUserId.HasValue || actorUserId.Value == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            string? actorRole = user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role");
            string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
            string? traceId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? httpContext.TraceIdentifier;

            var result = await service.CreateGoodsReceiptAsync(
                request,
                actorUserId.Value,
                actorUserId,
                actorRole,
                ip,
                traceId,
                ct);

            return result.IsSuccess
                ? Results.Created($"{prefix}/{result.Value.Id}", result.Value)
                : ToProblem(httpContext, result.Error);
        })
        .RequireAuthorization(ReceivePolicy)
        .WithSummary("Create goods receipt intake")
        .Produces<GoodsReceiptDetailDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        string? val = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(val, out Guid id) ? id : null;
    }

    private static IResult ToProblem(HttpContext context, DomainError error)
    {
        return PurchasingResults.Problem(context, error);
    }

    private static IResult ToValidationProblem(HttpContext context, FluentValidation.Results.ValidationResult validation)
    {
        var dict = validation.Errors
            .GroupBy(static e => e.PropertyName)
            .ToDictionary(
                static g => g.Key,
                static g => g.Select(static e => e.ErrorMessage).ToArray());

        DomainError domainError = DomainError.Validation(ErrorCodes.ValidationFailed, "Validation failed.", dict);
        return PurchasingResults.Problem(context, domainError);
    }
}
