using FluentValidation;
using GroceryHub.Modules.Ordering.Application.Services;
using GroceryHub.Modules.Ordering.Application.Validators;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryHub.Modules.Ordering.Api;

public static class OrderingModuleExtensions
{
    public static IServiceCollection AddOrderingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddValidatorsFromAssemblyContaining<ShippingQuoteValidator>();
        services.AddScoped<OrderingService>();

        return services;
    }

    public static IEndpointRouteBuilder MapOrderingEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        OrderingEndpoints.MapOrderingEndpoints(endpoints);
        return endpoints;
    }
}
