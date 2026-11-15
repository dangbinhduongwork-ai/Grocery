using FluentValidation;
using GroceryHub.Modules.Customers.Application.Services;
using GroceryHub.Modules.Customers.Application.Validators;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryHub.Modules.Customers.Api;

public static class CustomersModuleExtensions
{
    public static IServiceCollection AddCustomersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddValidatorsFromAssemblyContaining<CreateAddressValidator>();
        services.AddScoped<CustomersService>();

        return services;
    }

    public static IEndpointRouteBuilder MapCustomersEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        CustomersEndpoints.MapCustomersEndpoints(endpoints);
        return endpoints;
    }
}
