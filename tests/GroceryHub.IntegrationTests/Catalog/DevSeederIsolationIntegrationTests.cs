using GroceryHub.Modules.Catalog.Infrastructure.Seeding;
using GroceryHub.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GroceryHub.IntegrationTests.Catalog;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class DevSeederIsolationIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
    private readonly SqlServerTestDatabaseFixture _dbFixture = dbFixture;
    private ServiceProvider _serviceProvider = null!;

    public async Task InitializeAsync()
    {
        await _dbFixture.ResetDatabaseAsync();

        ServiceCollection services = new();
        services.AddDbContext<GroceryHubDbContext>(options =>
            options.UseSqlServer(_dbFixture.ConnectionString));
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task ProductionEnvironment_CatalogDevSeederDoesNotExecute()
    {
        // 1. Arrange: Ensure catalog is empty
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            int initialCount = await db.Products.CountAsync();
            Assert.Equal(0, initialCount);
        }

        // 2. Instantiate seeder with Production environment
        TestHostEnvironment prodEnvironment = new("Production");
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:AutoSeed"] = "true"
            })
            .Build();

        IServiceScopeFactory scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        CatalogDevDataSeederHostedService seeder = new(
            scopeFactory,
            prodEnvironment,
            config,
            TimeProvider.System,
            NullLogger<CatalogDevDataSeederHostedService>.Instance);

        // 3. Act
        await seeder.StartAsync(CancellationToken.None);

        // 4. Assert: Zero products seeded in Production
        await using (GroceryHubDbContext verifyDb = _dbFixture.CreateDbContext())
        {
            int productCount = await verifyDb.Products.CountAsync();
            int categoryCount = await verifyDb.Categories.CountAsync();
            int brandCount = await verifyDb.Brands.CountAsync();

            Assert.Equal(0, productCount);
            Assert.Equal(0, categoryCount);
            Assert.Equal(0, brandCount);
        }
    }

    [Fact]
    public async Task StagingEnvironment_CatalogDevSeederDoesNotExecute()
    {
        TestHostEnvironment stagingEnvironment = new("Staging");
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:AutoSeed"] = "true"
            })
            .Build();

        IServiceScopeFactory scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        CatalogDevDataSeederHostedService seeder = new(
            scopeFactory,
            stagingEnvironment,
            config,
            TimeProvider.System,
            NullLogger<CatalogDevDataSeederHostedService>.Instance);

        await seeder.StartAsync(CancellationToken.None);

        await using GroceryHubDbContext verifyDb = _dbFixture.CreateDbContext();
        int productCount = await verifyDb.Products.CountAsync();
        Assert.Equal(0, productCount);
    }

    [Fact]
    public async Task DevelopmentEnvironment_WhenAutoSeedTrue_CatalogDevSeederPopulatesSampleCatalog()
    {
        TestHostEnvironment devEnvironment = new("Development");
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:AutoSeed"] = "true"
            })
            .Build();

        IServiceScopeFactory scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        CatalogDevDataSeederHostedService seeder = new(
            scopeFactory,
            devEnvironment,
            config,
            TimeProvider.System,
            NullLogger<CatalogDevDataSeederHostedService>.Instance);

        await seeder.StartAsync(CancellationToken.None);

        await using GroceryHubDbContext verifyDb = _dbFixture.CreateDbContext();
        int productCount = await verifyDb.Products.CountAsync();
        int categoryCount = await verifyDb.Categories.CountAsync();
        int brandCount = await verifyDb.Brands.CountAsync();

        Assert.True(productCount >= 20, $"Expected >= 20 products seeded in Development, found: {productCount}");
        Assert.True(categoryCount >= 5, $"Expected >= 5 categories seeded in Development, found: {categoryCount}");
        Assert.True(brandCount >= 5, $"Expected >= 5 brands seeded in Development, found: {brandCount}");
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "GroceryHub.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
