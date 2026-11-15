using GroceryHub.Modules.Reporting.Application;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryHub.Modules.Reporting.Api;

public static class ReportingModuleExtensions
{
    public static IServiceCollection AddReportingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<ReportingService>();

        return services;
    }

    public static IEndpointRouteBuilder MapReportingEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        ReportingEndpoints.MapReportingApiEndpoints(endpoints);

        return endpoints;
    }
}
