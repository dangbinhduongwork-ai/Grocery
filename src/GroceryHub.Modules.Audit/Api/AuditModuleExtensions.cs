using GroceryHub.Modules.Audit.Application;
using GroceryHub.Modules.Audit.Contracts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryHub.Modules.Audit.Api;

public static class AuditModuleExtensions
{
    public static IServiceCollection AddAuditModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<AuditLogService>();
        services.AddScoped<IAuditModuleLogPort>(sp => sp.GetRequiredService<AuditLogService>());
        services.AddScoped<GroceryHub.SharedKernel.IAuditLogPort>(sp => sp.GetRequiredService<AuditLogService>());

        return services;
    }

    public static IEndpointRouteBuilder MapAuditEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        AuditEndpoints.MapAuditApiEndpoints(endpoints);
        return endpoints;
    }
}
