using FluentValidation;
using GroceryHub.Modules.Cart.Application.Services;
using GroceryHub.Modules.Cart.Application.Validators;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryHub.Modules.Cart.Api;

public static class CartModuleExtensions
{
    public static IServiceCollection AddCartModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMemoryCache();
        services.AddValidatorsFromAssemblyContaining<AddCartItemValidator>();
        services.AddScoped<CartService>();
        services.AddScoped<ICartService>(sp => sp.GetRequiredService<CartService>());
        services.AddScoped<ICartMergeService>(sp => sp.GetRequiredService<CartService>());

        return services;
    }

    public static IEndpointRouteBuilder MapCartEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        CartEndpoints.MapCartEndpoints(endpoints);
        return endpoints;
    }
}
