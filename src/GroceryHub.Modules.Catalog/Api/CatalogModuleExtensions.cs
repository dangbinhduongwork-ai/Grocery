using FluentValidation;
using GroceryHub.Modules.Catalog.Application.Services;
using GroceryHub.Modules.Catalog.Application.Storage;
using GroceryHub.Modules.Catalog.Application.Validators;
using GroceryHub.Modules.Catalog.Contracts;
using GroceryHub.Modules.Catalog.Infrastructure.Seeding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryHub.Modules.Catalog.Api;

public static class CatalogModuleExtensions
{
    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<FileStorageOptions>(
            configuration.GetSection(FileStorageOptions.SectionName));

        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IImageUploadService, ImageUploadService>();

        services.AddScoped<CatalogQueryService>();
        services.AddScoped<CatalogAdminService>();
        services.AddScoped<ICatalogModuleContract, CatalogModuleContract>();
        services.AddScoped<ICatalogReviewSummaryPort, CatalogReviewSummaryService>();

        services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();

        services.AddHostedService<CatalogDevDataSeederHostedService>();

        return services;
    }

    public static IEndpointRouteBuilder MapCatalogEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapCatalogStorefrontEndpoints();
        endpoints.MapCatalogAdminEndpoints();

        return endpoints;
    }
}
