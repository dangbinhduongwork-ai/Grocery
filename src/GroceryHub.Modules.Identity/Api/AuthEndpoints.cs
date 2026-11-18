using System.Security.Claims;
using GroceryHub.Modules.Identity.Application;
using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GroceryHub.Modules.Identity.Api;

internal static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/auth")
            .WithTags("Authentication");

        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            AuthService authService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Result<RegisterResponse> result = await authService.RegisterAsync(request, ct);
            return result.IsSuccess
                ? TypedResults.Created($"/api/v1/auth/me", result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("Register")
        .WithSummary("Register a new customer account")
        .Produces<RegisterResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .AllowAnonymous()
        .RequireRateLimiting("auth");

        group.MapPost("/verify-email", async (
            [FromBody] VerifyEmailRequest request,
            AuthService authService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Result<VerifyEmailResponse> result = await authService.VerifyEmailAsync(request, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("VerifyEmail")
        .WithSummary("Verify customer email address")
        .Produces<VerifyEmailResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .AllowAnonymous();

        group.MapPost("/resend-verification", async (
            [FromBody] ResendVerificationRequest request,
            AuthService authService,
            CancellationToken ct) =>
        {
            AcceptedResponse result = await authService.ResendVerificationAsync(request, ct);
            return TypedResults.Accepted(string.Empty, result);
        })
        .WithName("ResendVerification")
        .WithSummary("Resend email verification link")
        .Produces<AcceptedResponse>(StatusCodes.Status202Accepted)
        .AllowAnonymous()
        .RequireRateLimiting("auth");

        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            AuthService authService,
            HttpContext context,
            CancellationToken ct) =>
        {
            string? ip = context.Connection.RemoteIpAddress?.ToString();
            string? userAgent = context.Request.Headers.UserAgent;
            string? guestCartToken = context.Request.Cookies["gh_cart"];

            Result<AuthTokenResponse> result = await authService.LoginAsync(
                request,
                context.Response,
                ip,
                userAgent,
                guestCartToken,
                ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("Login")
        .WithSummary("Authenticate user credentials")
        .Produces<AuthTokenResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(423)
        .AllowAnonymous()
        .RequireRateLimiting("auth");

        group.MapPost("/activate", async (
            [FromBody] LoginRequest request,
            AuthService authService,
            HttpContext context,
            CancellationToken ct) =>
        {
            string? ip = context.Connection.RemoteIpAddress?.ToString();
            string? userAgent = context.Request.Headers.UserAgent;
            string? guestCartToken = context.Request.Cookies["gh_cart"];

            Result<AuthTokenResponse> result = await authService.ActivateAndLoginAsync(
                request,
                context.Response,
                ip,
                userAgent,
                guestCartToken,
                ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("ActivateAndLogin")
        .WithSummary("Activate user account with credentials and return auth token")
        .Produces<AuthTokenResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(423)
        .AllowAnonymous()
        .RequireRateLimiting("auth");

        group.MapPost("/refresh", async (
            AuthService authService,
            HttpContext context,
            CancellationToken ct) =>
        {
            string? ip = context.Connection.RemoteIpAddress?.ToString();
            string? userAgent = context.Request.Headers.UserAgent;

            Result<AuthTokenResponse> result = await authService.RefreshAsync(
                context.Request,
                context.Response,
                ip,
                userAgent,
                ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("RefreshToken")
        .WithSummary("Rotate refresh token cookie and issue new access token")
        .Produces<AuthTokenResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .AllowAnonymous();

        group.MapPost("/logout", async (
            AuthService authService,
            HttpContext context,
            CancellationToken ct) =>
        {
            await authService.LogoutAsync(context.Request, context.Response, ct);
            return TypedResults.NoContent();
        })
        .WithName("Logout")
        .WithSummary("Revoke refresh token family and logout")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization("auth");

        group.MapPost("/forgot-password", async (
            [FromBody] ForgotPasswordRequest request,
            AuthService authService,
            CancellationToken ct) =>
        {
            AcceptedResponse response = await authService.ForgotPasswordAsync(request, ct);
            return TypedResults.Accepted(string.Empty, response);
        })
        .WithName("ForgotPassword")
        .WithSummary("Initiate password reset via email")
        .Produces<AcceptedResponse>(StatusCodes.Status202Accepted)
        .AllowAnonymous()
        .RequireRateLimiting("auth");

        group.MapPost("/reset-password", async (
            [FromBody] ResetPasswordRequest request,
            AuthService authService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Result<ResetPasswordResponse> result = await authService.ResetPasswordAsync(request, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("ResetPassword")
        .WithSummary("Reset account password using reset token")
        .Produces<ResetPasswordResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .AllowAnonymous()
        .RequireRateLimiting("auth");

        group.MapPost("/change-password", async (
            [FromBody] ChangePasswordRequest request,
            AuthService authService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(context.User);
            if (!userId.HasValue)
            {
                return IdentityResults.Problem(context, DomainError.Unauthorized());
            }

            Result<ChangePasswordResponse> result = await authService.ChangePasswordAsync(
                userId.Value,
                request,
                context.Request,
                ct);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("ChangePassword")
        .WithSummary("Change password for authenticated user")
        .Produces<ChangePasswordResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization("auth");

        group.MapGet("/me", async (
            AuthService authService,
            HttpContext context,
            CancellationToken ct) =>
        {
            Guid? userId = GetUserIdFromClaims(context.User);
            if (!userId.HasValue)
            {
                return IdentityResults.Problem(context, DomainError.Unauthorized());
            }

            Result<AuthUserDto> result = await authService.GetMeAsync(userId.Value, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : IdentityResults.Problem(context, result.Error);
        })
        .WithName("GetCurrentUser")
        .WithSummary("Retrieve current authenticated user profile, roles, and effective permissions")
        .Produces<AuthUserDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .RequireAuthorization("auth");

        return group;
    }

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        string? sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(sub, out Guid id) ? id : null;
    }
}
