using FluentValidation;
using GroceryHub.Modules.Promotions.Application.Services;
using GroceryHub.Modules.Promotions.Application.Validators;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryHub.Modules.Promotions.Api;

public static class PromotionsModuleExtensions
{
    public static IServiceCollection AddPromotionsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddValidatorsFromAssemblyContaining<CreateCouponValidator>();
        services.AddScoped<PromotionsService>();

        return services;
    }

    public static IEndpointRouteBuilder MapPromotionsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        PromotionsEndpoints.MapPromotionsEndpoints(endpoints);
        return endpoints;
    }
}
