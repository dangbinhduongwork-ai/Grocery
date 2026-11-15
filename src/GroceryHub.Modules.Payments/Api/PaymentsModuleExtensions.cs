using GroceryHub.Modules.Payments.Application;
using GroceryHub.Modules.Payments.Configuration;
using GroceryHub.Modules.Payments.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GroceryHub.Modules.Payments.Api;

public static class PaymentsModuleExtensions
{
    public static IServiceCollection AddPaymentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SePayOptions>(configuration.GetSection(SePayOptions.SectionName));
        services.AddSingleton<IValidateOptions<SePayOptions>, SePayOptionsValidator>();

        services.AddHttpClient("SePayApi", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<IVietQrUrlBuilder, VietQrUrlBuilder>();
        services.AddSingleton<ISePaySignatureValidator, SePaySignatureValidator>();
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddHostedService<PaymentExpiryService>();
        services.AddHostedService<ReconciliationService>();

        return services;
    }

    public static IEndpointRouteBuilder MapPaymentsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPaymentsApiEndpoints();

        return endpoints;
    }
}
