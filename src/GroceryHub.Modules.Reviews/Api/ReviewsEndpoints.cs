using System.Security.Claims;
using FluentValidation;
using GroceryHub.Modules.Reviews.Application.Services;
using GroceryHub.Modules.Reviews.Contracts;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GroceryHub.Modules.Reviews.Api;

internal static class ReviewsEndpoints
{
    private const string ReviewsModeratePolicy = "reviews.moderate";

    public static IEndpointRouteBuilder MapReviewsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MapStorefrontReviewsEndpoints(endpoints);
        MapAdminReviewsEndpoints(endpoints);
        return endpoints;
    }

    private static void MapStorefrontReviewsEndpoints(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("")
            .WithTags("Storefront Reviews");

        group.MapGet("/products/{id:guid}/reviews", async (
            [FromRoute] Guid id,
            [FromQuery] int? rating,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ReviewsService reviewsService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await reviewsService.GetProductReviewsAsync(
                id,
                rating,
                page ?? 1,
                pageSize ?? 10,
                ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ReviewsResults.Problem(context, result.Error);
        })
        .WithName("GetProductReviews")
        .WithSummary("Get reviews and rating summary for a product")
        .Produces<ProductReviewsResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        group.MapPost("/products/{id:guid}/reviews", async (
            [FromRoute] Guid id,
            [FromBody] CreateReviewRequest request,
            IValidator<CreateReviewRequest> validator,
            ReviewsService reviewsService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                var errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return ReviewsResults.Problem(context, DomainError.ValidationFailed(errors));
            }

            if (!TryGetUserId(context.User, out Guid userId))
            {
                return ReviewsResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await reviewsService.CreateReviewAsync(id, userId, request, ct);

            return result.IsSuccess
                ? TypedResults.Created($"/api/v1/products/{id}/reviews", result.Value)
                : ReviewsResults.Problem(context, result.Error);
        })
        .WithName("CreateProductReview")
        .WithSummary("Create a verified-purchase product review")
        .Produces<CustomerReviewDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();

        group.MapPut("/me/reviews/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateReviewRequest request,
            IValidator<UpdateReviewRequest> validator,
            ReviewsService reviewsService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                var errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return ReviewsResults.Problem(context, DomainError.ValidationFailed(errors));
            }

            if (!TryGetUserId(context.User, out Guid userId))
            {
                return ReviewsResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await reviewsService.UpdateReviewAsync(id, userId, request, ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ReviewsResults.Problem(context, result.Error);
        })
        .WithName("UpdateCustomerReview")
        .WithSummary("Edit customer review within 7 days")
        .Produces<CustomerReviewDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();

        group.MapGet("/me/reviews", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ReviewsService reviewsService,
            HttpContext context,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(context.User, out Guid userId))
            {
                return ReviewsResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await reviewsService.GetCustomerReviewsAsync(
                userId,
                page ?? 1,
                pageSize ?? 10,
                ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ReviewsResults.Problem(context, result.Error);
        })
        .WithName("GetCustomerReviews")
        .WithSummary("Get current customer's reviews")
        .Produces<PagedResponse<CustomerReviewDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();
    }

    private static void MapAdminReviewsEndpoints(IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/admin/reviews")
            .WithTags("Admin Reviews");

        group.MapGet("", async (
            [FromQuery] string? status,
            [FromQuery] Guid? productId,
            [FromQuery] int? rating,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ReviewsService reviewsService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var result = await reviewsService.GetAdminReviewsAsync(
                status,
                productId,
                rating,
                page ?? 1,
                pageSize ?? 24,
                ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ReviewsResults.Problem(context, result.Error);
        })
        .WithName("GetAdminReviews")
        .WithSummary("Get reviews for moderation")
        .Produces<PagedResponse<AdminReviewDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization(ReviewsModeratePolicy);

        group.MapPost("/{id:guid}/approve", async (
            [FromRoute] Guid id,
            ReviewsService reviewsService,
            HttpContext context,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(context.User, out Guid userId))
            {
                return ReviewsResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await reviewsService.ApproveReviewAsync(id, userId, ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ReviewsResults.Problem(context, result.Error);
        })
        .WithName("ApproveAdminReview")
        .WithSummary("Approve a pending review")
        .Produces<AdminReviewDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(ReviewsModeratePolicy);

        group.MapPost("/{id:guid}/reject", async (
            [FromRoute] Guid id,
            [FromBody] RejectReviewRequest? request,
            IValidator<RejectReviewRequest> validator,
            ReviewsService reviewsService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var req = request ?? new RejectReviewRequest(null);
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
            {
                var errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return ReviewsResults.Problem(context, DomainError.ValidationFailed(errors));
            }

            if (!TryGetUserId(context.User, out Guid userId))
            {
                return ReviewsResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await reviewsService.RejectReviewAsync(id, userId, req.Reason, ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ReviewsResults.Problem(context, result.Error);
        })
        .WithName("RejectAdminReview")
        .WithSummary("Reject a review with optional reason")
        .Produces<AdminReviewDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(ReviewsModeratePolicy);

        group.MapPost("/{id:guid}/reply", async (
            [FromRoute] Guid id,
            [FromBody] ReplyReviewRequest request,
            IValidator<ReplyReviewRequest> validator,
            ReviewsService reviewsService,
            HttpContext context,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                var errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                return ReviewsResults.Problem(context, DomainError.ValidationFailed(errors));
            }

            if (!TryGetUserId(context.User, out Guid userId))
            {
                return ReviewsResults.Problem(context, DomainError.Unauthorized());
            }

            var result = await reviewsService.ReplyReviewAsync(id, userId, request.ReplyBody, ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : ReviewsResults.Problem(context, result.Error);
        })
        .WithName("ReplyAdminReview")
        .WithSummary("Post store reply to a review")
        .Produces<AdminReviewDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization(ReviewsModeratePolicy);
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        userId = Guid.Empty;
        string? claimVal = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;

        return Guid.TryParse(claimVal, out userId);
    }
}
