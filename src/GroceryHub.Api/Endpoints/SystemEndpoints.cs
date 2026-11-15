using GroceryHub.Api.Configuration;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GroceryHub.Api.Endpoints;

public sealed record HealthStatusResponse(string Status);

public sealed record SystemInfoResponse(
    string Service,
    string Version,
    string Environment,
    DateTimeOffset TimestampUtc);

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemAndHealthEndpoints(
        this IEndpointRouteBuilder app,
        RouteGroupBuilder apiV1Group)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(apiV1Group);

        // Liveness endpoint: anonymous container probe
        app.MapGet("/health/live", static () => TypedResults.Ok(new HealthStatusResponse("Healthy")))
            .AllowAnonymous()
            .WithName("GetHealthLive")
            .WithTags("Health")
            .Produces<HealthStatusResponse>(StatusCodes.Status200OK);

        // Readiness endpoint: anonymous readiness check running "ready"-tagged health checks
        app.MapGet("/health/ready", static async Task<Results<Ok<HealthStatusResponse>, JsonHttpResult<HealthStatusResponse>>> (
                HealthCheckService healthCheckService,
                CancellationToken ct) =>
            {
                HealthReport report = await healthCheckService.CheckHealthAsync(
                    static registration => registration.Tags.Contains("ready"),
                    ct);

                HealthStatusResponse payload = new(report.Status.ToString());
                return report.Status == HealthStatus.Healthy
                    ? TypedResults.Ok(payload)
                    : TypedResults.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
            })
            .AllowAnonymous()
            .WithName("GetHealthReady")
            .WithTags("Health")
            .Produces<HealthStatusResponse>(StatusCodes.Status200OK)
            .Produces<HealthStatusResponse>(StatusCodes.Status503ServiceUnavailable);

        // Public system info endpoint for API contract & smoke check
        apiV1Group.MapGet("/system/info", static (
                IClock clock,
                IOptions<SystemOptions> systemOptions,
                IHostEnvironment hostEnvironment) =>
            {
                SystemOptions opts = systemOptions.Value;
                string environment = string.IsNullOrWhiteSpace(opts.EnvironmentName)
                    ? hostEnvironment.EnvironmentName
                    : opts.EnvironmentName;

                SystemInfoResponse response = new(
                    Service: string.IsNullOrWhiteSpace(opts.ServiceName) ? "GroceryHub.Api" : opts.ServiceName,
                    Version: string.IsNullOrWhiteSpace(opts.Version) ? "1.0.0" : opts.Version,
                    Environment: environment,
                    TimestampUtc: clock.UtcNow);

                return TypedResults.Ok(response);
            })
            .AllowAnonymous()
            .WithName("GetSystemInfo")
            .WithTags("System")
            .Produces<SystemInfoResponse>(StatusCodes.Status200OK);

        return app;
    }
}
