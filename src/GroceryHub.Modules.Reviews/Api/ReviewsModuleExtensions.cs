using FluentValidation;
using GroceryHub.Modules.Reviews.Application.Services;
using GroceryHub.Modules.Reviews.Application.Validators;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryHub.Modules.Reviews.Api;

public static class ReviewsModuleExtensions
{
    public static IServiceCollection AddReviewsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddValidatorsFromAssemblyContaining<CreateReviewValidator>();
        services.AddScoped<ReviewsService>();

        return services;
    }

    public static IEndpointRouteBuilder MapReviewsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ReviewsEndpoints.MapReviewsEndpoints(endpoints);
        return endpoints;
    }
}
