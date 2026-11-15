using FluentValidation;
using GroceryHub.Modules.Purchasing.Application.Services;
using GroceryHub.Modules.Purchasing.Application.Validators;
using GroceryHub.Modules.Purchasing.Contracts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryHub.Modules.Purchasing.Api;

public static class PurchasingModuleExtensions
{
    public static IServiceCollection AddPurchasingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<PurchasingAdminService>();
        services.AddScoped<IPurchasingModuleContract, PurchasingModuleContract>();
        services.AddValidatorsFromAssemblyContaining<CreateSupplierRequestValidator>();

        return services;
    }

    public static IEndpointRouteBuilder MapPurchasingEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPurchasingApiEndpoints();

        return endpoints;
    }
}
