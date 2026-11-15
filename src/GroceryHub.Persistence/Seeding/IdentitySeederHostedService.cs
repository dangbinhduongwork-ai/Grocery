using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GroceryHub.Persistence.Seeding;

public sealed class IdentitySeederHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<IdentitySeederHostedService> logger,
    IHostEnvironment? hostEnvironment = null) : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ILogger<IdentitySeederHostedService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IHostEnvironment? _hostEnvironment = hostEnvironment;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        bool isDevelopmentOrTesting = IsDevelopmentOrTesting();

        bool autoMigrate = _configuration.GetValue<bool>("Database:AutoMigrate", isDevelopmentOrTesting);
        bool autoSeed = _configuration.GetValue<bool>("Database:AutoSeed", isDevelopmentOrTesting);

        if (!autoMigrate && !autoSeed)
        {
            return;
        }

        try
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            GroceryHubDbContext db = scope.ServiceProvider.GetRequiredService<GroceryHubDbContext>();

            if (autoMigrate && !IsProduction())
            {
                _logger.LogInformation("Applying EF Core migrations at startup for non-production environment...");
                await db.Database.MigrateAsync(cancellationToken);
            }

            if (autoSeed)
            {
                _logger.LogInformation("Running idempotent reference-data seeding...");
                await IdentitySeeder.SeedIdentityAsync(db, _configuration, _timeProvider, _logger, cancellationToken);
                await AdminUnitSeeder.SeedAdminUnitsAsync(db, _timeProvider, _logger, cancellationToken);
                await ShippingSeeder.SeedShippingAsync(db, _timeProvider, _logger, cancellationToken);
                await PromotionsSeeder.SeedPromotionsAsync(db, _timeProvider, _logger, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database migration or identity seeding failed at startup. This is expected if the database is intentionally unreachable or during health-check probes.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private bool IsDevelopmentOrTesting()
    {
        if (_hostEnvironment is not null)
        {
            return _hostEnvironment.IsDevelopment() ||
                   string.Equals(_hostEnvironment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);
        }

        string? envName = _configuration["ASPNETCORE_ENVIRONMENT"] ??
                          _configuration["DOTNET_ENVIRONMENT"] ??
                          _configuration["GroceryHub:EnvironmentName"];

        return string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(envName, "Testing", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsProduction()
    {
        if (_hostEnvironment is not null)
        {
            return _hostEnvironment.IsProduction();
        }

        string? envName = _configuration["ASPNETCORE_ENVIRONMENT"] ??
                          _configuration["DOTNET_ENVIRONMENT"] ??
                          _configuration["GroceryHub:EnvironmentName"];

        return string.Equals(envName, "Production", StringComparison.OrdinalIgnoreCase);
    }
}
