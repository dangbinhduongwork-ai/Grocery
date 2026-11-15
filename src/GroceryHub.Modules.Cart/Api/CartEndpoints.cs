using System.Security.Claims;
using FluentValidation;
using GroceryHub.Modules.Cart.Application.Services;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GroceryHub.Modules.Cart.Api;

internal static class CartEndpoints
{
    public static RouteGroupBuilder MapCartEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/cart")
            .WithTags("Cart");

        group.MapGet("/", async (
            ClaimsPrincipal user,
            CartService cartService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            string? guestToken = context.Request.Cookies["gh_cart"];

            Result<(CartViewDto CartView, string? NewGuestToken)> result =
                await cartService.GetOrCreateCartViewAsync(userId, guestToken, ct);

            if (!result.IsSuccess)
            {
                return CartResults.Problem(context, result.Error);
            }

            if (!userId.HasValue && !string.IsNullOrWhiteSpace(result.Value.NewGuestToken))
            {
                SetGuestCartCookie(context, result.Value.NewGuestToken);
            }

            return TypedResults.Ok(result.Value.CartView);
        })
        .WithName("GetCart")
        .WithSummary("Get active cart view with live prices and stock status")
        .Produces<CartViewDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .AllowAnonymous();

        group.MapPost("/items", async (
            [FromBody] AddCartItemRequest request,
            IValidator<AddCartItemRequest> validator,
            ClaimsPrincipal user,
            CartService cartService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return CartResults.Problem(context, DomainError.ValidationFailed(
                    ToFieldErrors(validation),
                    "Add to cart validation failed."));
            }

            Guid? userId = GetUserIdFromClaims(user);
            string? guestToken = context.Request.Cookies["gh_cart"];

            Result<(CartViewDto CartView, string? NewGuestToken)> result =
                await cartService.AddItemAsync(userId, guestToken, request, ct);

            if (!result.IsSuccess)
            {
                return CartResults.Problem(context, result.Error);
            }

            if (!userId.HasValue && !string.IsNullOrWhiteSpace(result.Value.NewGuestToken))
            {
                SetGuestCartCookie(context, result.Value.NewGuestToken);
            }

            return TypedResults.Ok(result.Value.CartView);
        })
        .WithName("AddCartItem")
        .WithSummary("Add a product item to the shopping cart (1..20 per line)")
        .Produces<CartViewDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .AllowAnonymous();

        group.MapPatch("/items/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCartItemRequest request,
            IValidator<UpdateCartItemRequest> validator,
            ClaimsPrincipal user,
            CartService cartService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return CartResults.Problem(context, DomainError.ValidationFailed(
                    ToFieldErrors(validation),
                    "Update cart item validation failed."));
            }

            Guid? userId = GetUserIdFromClaims(user);
            string? guestToken = context.Request.Cookies["gh_cart"];

            Result<CartViewDto> result =
                await cartService.UpdateItemQuantityAsync(userId, guestToken, id, request.Quantity, ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : CartResults.Problem(context, result.Error);
        })
        .WithName("UpdateCartItemQuantityPatch")
        .WithSummary("Update item quantity in cart (0 removes item, 1..20 updates)")
        .Produces<CartViewDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .AllowAnonymous();

        group.MapPut("/items/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCartItemRequest request,
            IValidator<UpdateCartItemRequest> validator,
            ClaimsPrincipal user,
            CartService cartService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return CartResults.Problem(context, DomainError.ValidationFailed(
                    ToFieldErrors(validation),
                    "Update cart item validation failed."));
            }

            Guid? userId = GetUserIdFromClaims(user);
            string? guestToken = context.Request.Cookies["gh_cart"];

            Result<CartViewDto> result =
                await cartService.UpdateItemQuantityAsync(userId, guestToken, id, request.Quantity, ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : CartResults.Problem(context, result.Error);
        })
        .WithName("UpdateCartItemQuantityPut")
        .WithSummary("Update item quantity in cart")
        .Produces<CartViewDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .AllowAnonymous();

        group.MapDelete("/items/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            CartService cartService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            string? guestToken = context.Request.Cookies["gh_cart"];

            Result<CartViewDto> result =
                await cartService.RemoveItemAsync(userId, guestToken, id, ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : CartResults.Problem(context, result.Error);
        })
        .WithName("RemoveCartItem")
        .WithSummary("Remove an item line from the cart")
        .Produces<CartViewDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        group.MapDelete("/", async (
            ClaimsPrincipal user,
            CartService cartService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            string? guestToken = context.Request.Cookies["gh_cart"];

            Result result = await cartService.ClearCartAsync(userId, guestToken, ct);

            return result.IsSuccess
                ? TypedResults.NoContent()
                : CartResults.Problem(context, result.Error);
        })
        .WithName("ClearCart")
        .WithSummary("Clear all items from the shopping cart")
        .Produces(StatusCodes.Status204NoContent)
        .AllowAnonymous();

        group.MapPost("/coupon", async (
            [FromBody] ApplyCouponRequest request,
            IValidator<ApplyCouponRequest> validator,
            ClaimsPrincipal user,
            CartService cartService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return CartResults.Problem(context, DomainError.Unauthorized());
            }

            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return CartResults.Problem(context, DomainError.ValidationFailed(
                    ToFieldErrors(validation),
                    "Coupon validation failed."));
            }

            string? guestToken = context.Request.Cookies["gh_cart"];
            Result<CartViewDto> result = await cartService.ApplyCouponAsync(userId.Value, guestToken, request.Code, ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : CartResults.Problem(context, result.Error);
        })
        .WithName("ApplyCartCoupon")
        .WithSummary("Apply a promotional coupon code to the authenticated customer's cart")
        .Produces<CartViewDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization("auth");

        group.MapDelete("/coupon", async (
            ClaimsPrincipal user,
            CartService cartService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(user);
            if (!userId.HasValue)
            {
                return CartResults.Problem(context, DomainError.Unauthorized());
            }

            string? guestToken = context.Request.Cookies["gh_cart"];
            Result<CartViewDto> result = await cartService.RemoveCouponAsync(userId.Value, guestToken, ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : CartResults.Problem(context, result.Error);
        })
        .WithName("RemoveCartCoupon")
        .WithSummary("Remove applied coupon from cart")
        .Produces<CartViewDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization("auth");

        return group;
    }

    private static void SetGuestCartCookie(HttpContext context, string guestToken)
    {
        context.Response.Cookies.Append("gh_cart", guestToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !context.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase),
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1",
            MaxAge = TimeSpan.FromDays(30)
        });
    }

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        string? sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(sub, out Guid parsed) ? parsed : null;
    }

    private static Dictionary<string, string[]> ToFieldErrors(FluentValidation.Results.ValidationResult validation)
    {
        return validation.Errors
            .GroupBy(static e => e.PropertyName)
            .ToDictionary(
                static g => g.Key,
                static g => g.Select(static e => e.ErrorMessage).ToArray());
    }
}
