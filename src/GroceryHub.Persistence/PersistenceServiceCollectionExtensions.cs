using GroceryHub.Persistence.Outbox;
using GroceryHub.Persistence.Seeding;
using GroceryHub.Persistence.Services;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace GroceryHub.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public const string DefaultConnectionStringName = "DefaultConnection";
    public const string MigrationsHistoryTableName = HistoryRepository.DefaultTableName;

    public static IServiceCollection AddGroceryHubPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<GroceryHubPersistenceOptions>? configurePersistence = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IClock, SystemClock>();

        GroceryHubPersistenceOptions persistenceOptions = new();
        configurePersistence?.Invoke(persistenceOptions);
        services.TryAddSingleton(persistenceOptions);

        services.TryAddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<GroceryHubDbContext>((sp, options) =>
        {
            IConfiguration activeConfiguration = sp.GetService<IConfiguration>() ?? configuration;
            string connectionString = activeConfiguration.GetConnectionString(DefaultConnectionStringName)
                ?? activeConfiguration[$"ConnectionStrings:{DefaultConnectionStringName}"]
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=GroceryHub;Trusted_Connection=True;TrustServerCertificate=True;";

            options.UseSqlServer(
                    connectionString,
                    sql => sql.MigrationsHistoryTable(MigrationsHistoryTableName, DatabaseSchemas.Shared))
                .AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.TryAddScoped<IStockPersistenceService, StockPersistenceService>();
        services.TryAddScoped<IPurchasingPersistenceService, PurchasingPersistenceService>();

        services.TryAddScoped<IOutboxPublisher, EfOutboxPublisher>();
        services.TryAddScoped<OutboxDispatcherProcessor>(sp =>
            new OutboxDispatcherProcessor(
                sp.GetRequiredService<GroceryHubDbContext>(),
                sp,
                sp.GetRequiredService<TimeProvider>(),
                sp.GetService<ILogger<OutboxDispatcherProcessor>>()));

        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<IdentitySeederHostedService>();

        services.AddHealthChecks()
            .AddDbContextCheck<GroceryHubDbContext>(
                name: "sqlserver",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"]);

        return services;
    }

    public static IServiceCollection AddOutboxEventHandler<TEvent, THandler>(
        this IServiceCollection services)
        where TEvent : class, IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        ArgumentNullException.ThrowIfNull(services);

        OutboxEventTypeRegistry.Register<TEvent>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IIntegrationEventHandler<TEvent>, THandler>());
        return services;
    }
}
