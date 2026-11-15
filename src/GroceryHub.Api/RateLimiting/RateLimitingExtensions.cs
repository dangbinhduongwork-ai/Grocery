using System.Globalization;
using System.Threading.RateLimiting;
using GroceryHub.Api.Configuration;
using GroceryHub.Api.ErrorHandling;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Mvc;

namespace GroceryHub.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddGroceryHubRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(timeProvider);

        RateLimitingOptions options = configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                int retryAfterSeconds = 60;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                }

                context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";

                DomainError error = DomainError.RateLimited();
                ProblemDetails problem = ResultProblemExtensions.CreateProblemDetails(context.HttpContext, error);
                await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
            };

            limiterOptions.AddPolicy(RateLimitingOptions.AuthPolicyName, httpContext =>
            {
                string ipKey = GetClientIpPartitionKey(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"auth:{ipKey}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.AuthPermitLimitPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            limiterOptions.AddPolicy(RateLimitingOptions.CheckoutPolicyName, httpContext =>
            {
                string userOrIpKey = httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.Identity?.Name
                    ?? GetClientIpPartitionKey(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"checkout:{userOrIpKey}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.CheckoutPermitLimitPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            limiterOptions.AddPolicy(RateLimitingOptions.PublicReadPolicyName, httpContext =>
            {
                string ipKey = GetClientIpPartitionKey(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"public-read:{ipKey}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.PublicReadPermitLimitPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    private static string GetClientIpPartitionKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
