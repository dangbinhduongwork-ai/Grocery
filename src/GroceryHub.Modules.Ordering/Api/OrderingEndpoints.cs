using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using GroceryHub.Modules.Ordering.Application.DTOs;
using GroceryHub.Modules.Ordering.Application.Services;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GroceryHub.Modules.Ordering.Api;

public static class OrderingEndpoints
{
    private const string PolicyAuth = "auth";
    private const string PolicyOrdersRead = "orders.read";
    private const string PolicyOrdersProcess = "orders.process";
    private const string PolicySettingsManage = "settings.manage";

    public static void MapOrderingEndpoints(IEndpointRouteBuilder endpoints)
    {
        MapStorefrontEndpoints(endpoints);
        MapAdminEndpoints(endpoints);
    }

    private static void MapStorefrontEndpoints(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("")
            .WithTags("Storefront Ordering");

        // POST /shipping/quote
        group.MapPost("/shipping/quote", async (
            [FromBody] ShippingQuoteRequest request,
            IValidator<ShippingQuoteRequest> validator,
            OrderingService orderingService,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return OrderingResults.Problem(context, DomainError.Validation(
                    ErrorCodes.ValidationFailed,
                    "Invalid shipping quote request.",
                    ToFieldErrors(validation)));
            }

            Guid? userId = GetUserIdFromClaims(user);
            var result = await orderingService.GetShippingQuoteAsync(request, userId, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("GetShippingQuote")
        .WithSummary("Calculate shipping quote by province or address")
        .Produces<ShippingQuoteResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(PolicyAuth);

        // POST /checkout/quote
        group.MapPost("/checkout/quote", async (
            [FromBody] CheckoutQuoteRequest request,
            IValidator<CheckoutQuoteRequest> validator,
            OrderingService orderingService,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return OrderingResults.Problem(context, DomainError.Validation(
                    ErrorCodes.ValidationFailed,
                    "Invalid checkout quote request.",
                    ToFieldErrors(validation)));
            }

            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return OrderingResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await orderingService.GetCheckoutQuoteAsync(userId.Value, request, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("GetCheckoutQuote")
        .WithSummary("Generate live checkout quote with shipping, coupon and order items")
        .Produces<CheckoutQuoteDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(PolicyAuth);

        // POST /checkout/orders
        group.MapPost("/checkout/orders", async (
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            [FromBody] PlaceOrderRequest request,
            IValidator<PlaceOrderRequest> validator,
            OrderingService orderingService,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return OrderingResults.Problem(context, DomainError.Validation(
                    "IDEMPOTENCY_KEY_REQUIRED",
                    "The Idempotency-Key header is required for placing orders."));
            }

            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return OrderingResults.Problem(context, DomainError.Validation(
                    ErrorCodes.ValidationFailed,
                    "Invalid order placement request.",
                    ToFieldErrors(validation)));
            }

            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return OrderingResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await orderingService.PlaceOrderAsync(userId.Value, idempotencyKey, request, ct);
            return result.IsSuccess
                ? TypedResults.Created($"/api/v1/me/orders/{result.Value.Order.Id}", result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("PlaceOrder")
        .WithSummary("Place order with idempotency and atomic stock reservation")
        .Produces<PlaceOrderResponseDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(PolicyAuth)
        .RequireRateLimiting("checkout");

        // GET /me/orders
        group.MapGet("/me/orders", async (
            [FromQuery] string? status,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            OrderingService orderingService = null!,
            ClaimsPrincipal user = null!,
            HttpContext context = null!,
            CancellationToken ct = default) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return OrderingResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await orderingService.GetCustomerOrdersAsync(userId.Value, status, from, to, page, pageSize, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("GetMyOrders")
        .WithSummary("List orders for current customer")
        .Produces<PagedResponse<CustomerOrderListItemDto>>(StatusCodes.Status200OK)
        .RequireAuthorization(PolicyAuth);

        // GET /me/orders/{id}
        group.MapGet("/me/orders/{id:guid}", async (
            Guid id,
            OrderingService orderingService,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return OrderingResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await orderingService.GetCustomerOrderByIdAsync(userId.Value, id, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("GetMyOrderDetail")
        .WithSummary("Get detailed order info for current customer")
        .Produces<CustomerOrderDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(PolicyAuth);

        // POST /me/orders/{id}/cancel
        group.MapPost("/me/orders/{id:guid}/cancel", async (
            Guid id,
            [FromBody] CancelOrderRequest request,
            IValidator<CancelOrderRequest> validator,
            OrderingService orderingService,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return OrderingResults.Problem(context, DomainError.Validation(
                    ErrorCodes.ValidationFailed,
                    "Invalid cancel order request.",
                    ToFieldErrors(validation)));
            }

            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return OrderingResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await orderingService.CustomerCancelOrderAsync(userId.Value, id, request.Reason, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("CancelMyOrder")
        .WithSummary("Cancel order by customer (allowed in Placed, Confirmed, PendingPayment)")
        .Produces<CustomerOrderDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(PolicyAuth);

        // POST /me/orders/{id}/reorder
        group.MapPost("/me/orders/{id:guid}/reorder", async (
            Guid id,
            OrderingService orderingService,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return OrderingResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await orderingService.ReorderAsync(userId.Value, id, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("ReorderItems")
        .WithSummary("Add available items from past order to active cart")
        .Produces<ReorderResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(PolicyAuth);
    }

    private static void MapAdminEndpoints(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/admin")
            .WithTags("Admin Ordering & Operations");

        // GET /admin/orders
        group.MapGet("/orders", async (
            [FromQuery] string? q,
            [FromQuery] string? status,
            [FromQuery] string? paymentStatus,
            [FromQuery] string? paymentMethod,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 24,
            OrderingService orderingService = null!,
            HttpContext context = null!,
            CancellationToken ct = default) =>
        {
            var result = await orderingService.GetAdminOrdersAsync(q, status, paymentStatus, paymentMethod, from, to, page, pageSize, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("GetAdminOrders")
        .WithSummary("Search and list orders for management")
        .Produces<PagedResponse<AdminOrderListItemDto>>(StatusCodes.Status200OK)
        .RequireAuthorization(PolicyOrdersRead);

        // GET /admin/orders/{id}
        group.MapGet("/orders/{id:guid}", async (
            Guid id,
            OrderingService orderingService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await orderingService.GetAdminOrderByIdAsync(id, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("GetAdminOrderDetail")
        .WithSummary("Get full administrative order details")
        .Produces<AdminOrderDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(PolicyOrdersRead);

        // POST /admin/orders/{id}/transition
        group.MapPost("/orders/{id:guid}/transition", async (
            Guid id,
            [FromBody] TransitionOrderRequest request,
            IValidator<TransitionOrderRequest> validator,
            OrderingService orderingService,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return OrderingResults.Problem(context, DomainError.Validation(
                    ErrorCodes.ValidationFailed,
                    "Invalid order transition request.",
                    ToFieldErrors(validation)));
            }

            Guid? actorId = GetUserIdFromClaims(user);
            if (!actorId.HasValue)
            {
                return OrderingResults.Problem(context, DomainError.Unauthorized());
            }

            var permissions = GetUserPermissions(user);
            var result = await orderingService.StaffTransitionOrderAsync(id, request, actorId.Value, permissions, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("TransitionAdminOrder")
        .WithSummary("Transition order through state machine (requires appropriate permission per transition)")
        .Produces<AdminOrderDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(PolicyAuth);

        // POST /admin/orders/{id}/cancel
        group.MapPost("/orders/{id:guid}/cancel", async (
            Guid id,
            [FromBody] CancelOrderRequest request,
            IValidator<CancelOrderRequest> validator,
            OrderingService orderingService,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return OrderingResults.Problem(context, DomainError.Validation(
                    ErrorCodes.ValidationFailed,
                    "Invalid cancel order request.",
                    ToFieldErrors(validation)));
            }

            Guid? actorId = GetUserIdFromClaims(user);
            if (!actorId.HasValue)
            {
                return OrderingResults.Problem(context, DomainError.Unauthorized());
            }

            var permissions = GetUserPermissions(user);
            var result = await orderingService.StaffTransitionOrderAsync(
                id,
                new TransitionOrderRequest(OrderStatusConstants.Cancelled, request.Reason, null),
                actorId.Value,
                permissions,
                ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("CancelAdminOrder")
        .WithSummary("Cancel order by staff (requires orders.process or orders.cancel per payment status)")
        .Produces<AdminOrderDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(PolicyAuth);

        // PUT /admin/orders/{id}/note
        group.MapPut("/orders/{id:guid}/note", async (
            Guid id,
            [FromBody] OrderNoteRequest request,
            IValidator<OrderNoteRequest> validator,
            OrderingService orderingService,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return OrderingResults.Problem(context, DomainError.Validation(
                    ErrorCodes.ValidationFailed,
                    "Invalid internal note request.",
                    ToFieldErrors(validation)));
            }

            Guid? actorId = GetUserIdFromClaims(user);
            if (!actorId.HasValue)
            {
                return OrderingResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await orderingService.UpdateInternalNoteAsync(id, request.InternalNote, actorId.Value, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("UpdateAdminOrderStaffNote")
        .WithSummary("Update staff note on order")
        .Produces<OrderNoteResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(PolicyOrdersProcess);

        // POST /admin/orders/{id}/assign
        group.MapPost("/orders/{id:guid}/assign", async (
            Guid id,
            [FromBody] AssignOrderRequest request,
            OrderingService orderingService,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? actorId = GetUserIdFromClaims(user);
            if (!actorId.HasValue)
            {
                return OrderingResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await orderingService.AssignOrderAsync(id, request.AssignedToUserId ?? actorId.Value, actorId.Value, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("AssignAdminOrderStaff")
        .WithSummary("Assign order to staff member")
        .Produces<AssignOrderResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(PolicyOrdersProcess);

        // GET /admin/orders/{id}/pick-list
        group.MapGet("/orders/{id:guid}/pick-list", async (
            Guid id,
            OrderingService orderingService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await orderingService.GetPickListAsync(id, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("GetAdminOrderPickList")
        .WithSummary("Get grouped warehouse pick list for order")
        .Produces<PickListDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(PolicyOrdersRead);

        // GET /admin/orders/{id}/delivery-note
        group.MapGet("/orders/{id:guid}/delivery-note", async (
            Guid id,
            OrderingService orderingService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await orderingService.GetDeliveryNoteAsync(id, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("GetAdminOrderDeliveryNote")
        .WithSummary("Get shipping delivery note with COD amount and customer instructions")
        .Produces<DeliveryNoteDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(PolicyOrdersRead);

        // GET /admin/shipping/zones
        group.MapGet("/shipping/zones", async (
            OrderingService orderingService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await orderingService.GetShippingZonesAsync(ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("GetShippingZones")
        .WithSummary("Get all configured shipping zones with rates and province mappings")
        .Produces<IReadOnlyList<ShippingZoneDto>>(StatusCodes.Status200OK)
        .RequireAuthorization(PolicySettingsManage);

        // POST /admin/shipping/zones
        group.MapPost("/shipping/zones", async (
            [FromBody] CreateShippingZoneRequest request,
            IValidator<CreateShippingZoneRequest> validator,
            OrderingService orderingService,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return OrderingResults.Problem(context, DomainError.Validation(
                    ErrorCodes.ValidationFailed,
                    "Invalid shipping zone request.",
                    ToFieldErrors(validation)));
            }

            Guid? actorId = GetUserIdFromClaims(user);
            if (!actorId.HasValue)
            {
                return OrderingResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await orderingService.CreateShippingZoneAsync(request, actorId.Value, ct);
            return result.IsSuccess
                ? TypedResults.Created($"/api/v1/admin/shipping/zones/{result.Value.Id}", result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("CreateShippingZone")
        .WithSummary("Create new shipping zone and rates")
        .Produces<ShippingZoneDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(PolicySettingsManage);

        // PUT /admin/shipping/zones/{id}
        group.MapPut("/shipping/zones/{id:guid}", async (
            Guid id,
            [FromBody] UpdateShippingZoneRequest request,
            IValidator<UpdateShippingZoneRequest> validator,
            OrderingService orderingService,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return OrderingResults.Problem(context, DomainError.Validation(
                    ErrorCodes.ValidationFailed,
                    "Invalid shipping zone request.",
                    ToFieldErrors(validation)));
            }

            Guid? actorId = GetUserIdFromClaims(user);
            if (!actorId.HasValue)
            {
                return OrderingResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await orderingService.UpdateShippingZoneAsync(id, request, actorId.Value, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("UpdateShippingZone")
        .WithSummary("Update existing shipping zone and rates")
        .Produces<ShippingZoneDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(PolicySettingsManage);

        // DELETE /admin/shipping/zones/{id}
        group.MapDelete("/shipping/zones/{id:guid}", async (
            Guid id,
            OrderingService orderingService,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? actorId = GetUserIdFromClaims(user);
            if (!actorId.HasValue)
            {
                return OrderingResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await orderingService.DeleteShippingZoneAsync(id, actorId.Value, ct);
            return result.IsSuccess
                ? TypedResults.NoContent()
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("DeleteShippingZone")
        .WithSummary("Delete shipping zone")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(PolicySettingsManage);

        // GET /admin/settings
        group.MapGet("/settings", async (
            OrderingService orderingService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await orderingService.GetStoreSettingsAsync(ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("GetStoreSettings")
        .WithSummary("Get global store settings")
        .Produces<StoreSettingsDto>(StatusCodes.Status200OK)
        .RequireAuthorization(PolicySettingsManage);

        // PUT /admin/settings
        group.MapPut("/settings", async (
            [FromBody] StoreSettingsDto request,
            IValidator<StoreSettingsDto> validator,
            OrderingService orderingService,
            ClaimsPrincipal user,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return OrderingResults.Problem(context, DomainError.Validation(
                    ErrorCodes.ValidationFailed,
                    "Invalid store settings request.",
                    ToFieldErrors(validation)));
            }

            Guid? actorId = GetUserIdFromClaims(user);
            if (!actorId.HasValue)
            {
                return OrderingResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await orderingService.UpdateStoreSettingsAsync(request, actorId.Value, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : OrderingResults.Problem(context, result.Error);
        })
        .WithName("UpdateStoreSettings")
        .WithSummary("Update global store settings")
        .Produces<StoreSettingsDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(PolicySettingsManage);
    }

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        string? sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(sub, out Guid id) ? id : null;
    }

    private static HashSet<string> GetUserPermissions(ClaimsPrincipal user)
    {
        return user.Claims
            .Where(c => c.Type == "permission" || c.Type == "permissions")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string[]> ToFieldErrors(ValidationResult validation)
    {
        return validation.Errors
            .GroupBy(static e => e.PropertyName)
            .ToDictionary(
                static g => g.Key,
                static g => g.Select(static e => e.ErrorMessage).ToArray());
    }
}
