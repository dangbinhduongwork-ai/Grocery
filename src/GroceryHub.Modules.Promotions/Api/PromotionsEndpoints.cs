using System.Security.Claims;
using FluentValidation;
using GroceryHub.Modules.Promotions.Application.DTOs;
using GroceryHub.Modules.Promotions.Application.Services;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GroceryHub.Modules.Promotions.Api;

internal static class PromotionsEndpoints
{
    public static RouteGroupBuilder MapPromotionsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/admin/coupons")
            .WithTags("Admin Promotions & Coupons")
            .RequireAuthorization("promotions.manage");

        group.MapGet("/", async (
            [FromQuery] string? q,
            [FromQuery] bool? isActive,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 24,
            PromotionsService promotionsService = default!,
            HttpContext context = default!,
            CancellationToken ct = default) =>
        {
            Result<PagedResponse<CouponDto>> result = await promotionsService.GetCouponsAsync(q, isActive, page, pageSize, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : PromotionsResults.Problem(context, result.Error);
        })
        .WithName("GetAdminCoupons")
        .WithSummary("List all promotional coupons with optional filtering and pagination")
        .Produces<PagedResponse<CouponDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/", async (
            [FromBody] CreateCouponRequest request,
            IValidator<CreateCouponRequest> validator,
            ClaimsPrincipal user,
            PromotionsService promotionsService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return PromotionsResults.Problem(context, DomainError.ValidationFailed(
                    ToFieldErrors(validation),
                    "Coupon validation failed."));
            }

            Guid actorId = GetUserIdFromClaims(user) ?? Guid.Empty;
            Result<CouponDto> result = await promotionsService.CreateCouponAsync(request, actorId, ct);
            return result.IsSuccess
                ? TypedResults.Created($"/api/v1/admin/coupons/{result.Value.Id}", result.Value)
                : PromotionsResults.Problem(context, result.Error);
        })
        .WithName("CreateAdminCoupon")
        .WithSummary("Create a new coupon code")
        .Produces<CouponDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{id:guid}", async (
            Guid id,
            PromotionsService promotionsService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Result<CouponDto> result = await promotionsService.GetCouponByIdAsync(id, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : PromotionsResults.Problem(context, result.Error);
        })
        .WithName("GetAdminCouponById")
        .WithSummary("Get coupon details by ID")
        .Produces<CouponDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCouponRequest request,
            IValidator<UpdateCouponRequest> validator,
            [FromHeader(Name = "If-Match")] string? ifMatch,
            ClaimsPrincipal user,
            PromotionsService promotionsService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                return PromotionsResults.Problem(context, DomainError.ValidationFailed(
                    ToFieldErrors(validation),
                    "Coupon validation failed."));
            }

            Guid actorId = GetUserIdFromClaims(user) ?? Guid.Empty;
            Result<CouponDto> result = await promotionsService.UpdateCouponAsync(id, request, ifMatch, actorId, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : PromotionsResults.Problem(context, result.Error);
        })
        .WithName("UpdateAdminCoupon")
        .WithSummary("Update an existing coupon")
        .Produces<CouponDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status412PreconditionFailed)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            PromotionsService promotionsService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid actorId = GetUserIdFromClaims(user) ?? Guid.Empty;
            Result result = await promotionsService.DeleteCouponAsync(id, actorId, ct);
            return result.IsSuccess
                ? TypedResults.NoContent()
                : PromotionsResults.Problem(context, result.Error);
        })
        .WithName("DeleteAdminCoupon")
        .WithSummary("Delete or deactivate an existing coupon")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/redemptions", async (
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 24,
            PromotionsService promotionsService = default!,
            HttpContext context = default!,
            CancellationToken ct = default) =>
        {
            Result<PagedResponse<CouponRedemptionDto>> result = await promotionsService.GetCouponRedemptionsAsync(id, page, pageSize, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : PromotionsResults.Problem(context, result.Error);
        })
        .WithName("GetAdminCouponRedemptions")
        .WithSummary("Get list of orders that redeemed this coupon")
        .Produces<PagedResponse<CouponRedemptionDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/usage", async (
            Guid id,
            PromotionsService promotionsService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Result<CouponUsageSummaryDto> result = await promotionsService.GetCouponUsageAsync(id, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : PromotionsResults.Problem(context, result.Error);
        })
        .WithName("GetAdminCouponUsage")
        .WithSummary("Get coupon redemption statistics and summary")
        .Produces<CouponUsageSummaryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
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
