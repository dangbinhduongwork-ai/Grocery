using FluentValidation;
using GroceryHub.Modules.Inventory.Application.Services;
using GroceryHub.Modules.Inventory.Application.Validators;
using GroceryHub.Modules.Inventory.Contracts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryHub.Modules.Inventory.Api;

public static class InventoryModuleExtensions
{
    public static IServiceCollection AddInventoryModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<InventoryAdminService>();
        services.AddScoped<IInventoryModuleContract, InventoryModuleContract>();
        services.AddValidatorsFromAssemblyContaining<AdjustStockRequestValidator>();

        return services;
    }

    public static IEndpointRouteBuilder MapInventoryEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapInventoryApiEndpoints();

        return endpoints;
    }
}
