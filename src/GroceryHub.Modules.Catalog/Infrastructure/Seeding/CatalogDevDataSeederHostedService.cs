using GroceryHub.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GroceryHub.Modules.Catalog.Infrastructure.Seeding;

public sealed class CatalogDevDataSeederHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CatalogDevDataSeederHostedService> _logger;

    public CatalogDevDataSeederHostedService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment hostEnvironment,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<CatalogDevDataSeederHostedService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // STRICT GUARD: Must NEVER run in Production or Staging (DEC-008)
        if (!_hostEnvironment.IsDevelopment())
        {
            _logger.LogInformation("Non-development environment ({Environment}). Catalog sample data seeder is disabled.", _hostEnvironment.EnvironmentName);
            return;
        }

        bool autoSeed = _configuration.GetValue<bool>("Database:AutoSeed", true);
        if (!autoSeed)
        {
            _logger.LogInformation("Database:AutoSeed is disabled. Skipping CatalogDevDataSeeder.");
            return;
        }

        try
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            GroceryHubDbContext db = scope.ServiceProvider.GetRequiredService<GroceryHubDbContext>();

            await CatalogDevDataSeeder.SeedSampleProductsAsync(db, _timeProvider, _logger, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run CatalogDevDataSeeder.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
