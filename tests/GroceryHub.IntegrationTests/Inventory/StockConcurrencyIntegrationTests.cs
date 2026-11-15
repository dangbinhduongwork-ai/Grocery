using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.Persistence.Services;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GroceryHub.IntegrationTests.Inventory;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class StockConcurrencyIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
    private readonly SqlServerTestDatabaseFixture _dbFixture = dbFixture;
    private GroceryHubWebApplicationFactory _factory = null!;

    public async Task InitializeAsync()
    {
        await _dbFixture.ResetDatabaseAsync();
        _factory = new GroceryHubWebApplicationFactory(_dbFixture.ConnectionString);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task ReserveStock_50ConcurrentRequestsForStockOf10_Exactly10Succeed_40Fail_NoOverselling()
    {
        // 1. Arrange: Seed a product with OnHand = 10, Reserved = 0
        Guid categoryId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Category category = new()
            {
                Id = categoryId,
                Name = "Gạo & Nông sản Concurrency",
                Slug = $"gao-nong-san-concurrency-{Guid.NewGuid():N}",
                SortOrder = 1,
                IsActive = true
            };

            Product product = new()
            {
                Id = productId,
                Sku = $"SKU-CONC-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                Name = "Gạo ST25 Concurrency Test 5kg",
                Slug = $"gao-st25-concurrency-{Guid.NewGuid():N}",
                SearchText = "gao st25 concurrency test 5kg",
                CategoryId = categoryId,
                Unit = "Túi",
                ListPrice = 180000,
                SellingPrice = 160000,
                CostPrice = 130000,
                Status = ProductStatus.Active
            };

            StockItem stockItem = new()
            {
                ProductId = productId,
                OnHand = 10,
                Reserved = 0,
                ReorderLevel = 2
            };

            db.Categories.Add(category);
            db.Products.Add(product);
            db.StockItems.Add(stockItem);
            await db.SaveChangesAsync();
        }

        // 2. Act: Fire 50 concurrent requests, each attempting to reserve 1 unit
        const int concurrentRequests = 50;
        List<Guid> orderIds = Enumerable.Range(0, concurrentRequests).Select(_ => Guid.NewGuid()).ToList();
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        Task<Result>[] reservationTasks = new Task<Result>[concurrentRequests];
        for (int i = 0; i < concurrentRequests; i++)
        {
            Guid orderId = orderIds[i];
            reservationTasks[i] = Task.Run(async () =>
            {
                using IServiceScope scope = _factory.Services.CreateScope();
                IStockPersistenceService stockService = scope.ServiceProvider.GetRequiredService<IStockPersistenceService>();
                return await stockService.ReserveStockAsync(orderId, [(productId, 1)], expiresAt);
            });
        }

        Result[] results = await Task.WhenAll(reservationTasks);

        // 3. Assert: Exactly 10 succeed, 40 fail with InsufficientStock
        int successes = results.Count(r => r.IsSuccess);
        int failures = results.Count(r => r.IsFailure);

        Assert.Equal(10, successes);
        Assert.Equal(40, failures);

        var failedResults = results.Where(r => r.IsFailure).ToList();
        Assert.All(failedResults, f =>
        {
            Assert.Equal(ErrorCodes.InsufficientStock, f.Error.Code);
            Assert.Contains(productId.ToString(), f.Error.Message);
        });

        // 4. Assert: In database, OnHand = 10, Reserved = 10, Available = 0
        await using (GroceryHubDbContext verifyDb = _dbFixture.CreateDbContext())
        {
            StockItem? finalStock = await verifyDb.StockItems
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ProductId == productId);

            Assert.NotNull(finalStock);
            Assert.Equal(10, finalStock!.OnHand);
            Assert.Equal(10, finalStock.Reserved);
            Assert.Equal(0, finalStock.OnHand - finalStock.Reserved);

            int activeReservations = await verifyDb.StockReservations
                .CountAsync(r => r.ProductId == productId && r.Status == StockReservationStatus.Active);
            Assert.Equal(10, activeReservations);
        }
    }

    [Fact]
    public async Task ReleaseAndCommitStockReservation_UpdatesStockAndLedgerAccurately()
    {
        // 1. Arrange: Seed a product with OnHand = 10, Reserved = 0
        Guid categoryId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        Guid orderIdA = Guid.NewGuid();
        Guid orderIdB = Guid.NewGuid();

        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Category category = new()
            {
                Id = categoryId,
                Name = "Thực phẩm Tươi sống",
                Slug = $"thuc-pham-tuoi-song-{Guid.NewGuid():N}",
                SortOrder = 1,
                IsActive = true
            };

            Product product = new()
            {
                Id = productId,
                Sku = $"SKU-RELCOM-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                Name = "Thịt Bò Concurrency 500g",
                Slug = $"thit-bo-concurrency-{Guid.NewGuid():N}",
                SearchText = "thit bo concurrency 500g",
                CategoryId = categoryId,
                Unit = "Khay",
                ListPrice = 150000,
                SellingPrice = 140000,
                CostPrice = 100000,
                Status = ProductStatus.Active
            };

            StockItem stockItem = new()
            {
                ProductId = productId,
                OnHand = 10,
                Reserved = 0,
                ReorderLevel = 2
            };

            db.Categories.Add(category);
            db.Products.Add(product);
            db.StockItems.Add(stockItem);
            await db.SaveChangesAsync();
        }

        // 2. Reserve 6 units for order A and 4 units for order B -> All 10 reserved
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IStockPersistenceService stockService = scope.ServiceProvider.GetRequiredService<IStockPersistenceService>();
            Result resA = await stockService.ReserveStockAsync(orderIdA, [(productId, 6)], DateTimeOffset.UtcNow.AddMinutes(15));
            Result resB = await stockService.ReserveStockAsync(orderIdB, [(productId, 4)], DateTimeOffset.UtcNow.AddMinutes(15));

            Assert.True(resA.IsSuccess);
            Assert.True(resB.IsSuccess);
        }

        // Available is now 0. Further reservation must fail
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IStockPersistenceService stockService = scope.ServiceProvider.GetRequiredService<IStockPersistenceService>();
            Result extraRes = await stockService.ReserveStockAsync(Guid.NewGuid(), [(productId, 1)], DateTimeOffset.UtcNow.AddMinutes(15));
            Assert.True(extraRes.IsFailure);
            Assert.Equal(ErrorCodes.InsufficientStock, extraRes.Error.Code);
        }

        // 3. Release order B (4 units) -> Reserved becomes 6, Available becomes 4
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IStockPersistenceService stockService = scope.ServiceProvider.GetRequiredService<IStockPersistenceService>();
            Result releaseRes = await stockService.ReleaseStockReservationAsync(orderIdB);
            Assert.True(releaseRes.IsSuccess);
        }

        await using (GroceryHubDbContext verifyDb = _dbFixture.CreateDbContext())
        {
            StockItem? stockAfterRelease = await verifyDb.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.ProductId == productId);
            Assert.NotNull(stockAfterRelease);
            Assert.Equal(10, stockAfterRelease!.OnHand);
            Assert.Equal(6, stockAfterRelease.Reserved);
            Assert.Equal(4, stockAfterRelease.OnHand - stockAfterRelease.Reserved);
        }

        // 4. Commit order A (6 units) -> OnHand becomes 4, Reserved becomes 0
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IStockPersistenceService stockService = scope.ServiceProvider.GetRequiredService<IStockPersistenceService>();
            Result commitRes = await stockService.CommitStockReservationAsync(orderIdA);
            Assert.True(commitRes.IsSuccess);
        }

        await using (GroceryHubDbContext verifyDb = _dbFixture.CreateDbContext())
        {
            StockItem? finalStock = await verifyDb.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.ProductId == productId);
            Assert.NotNull(finalStock);
            Assert.Equal(4, finalStock!.OnHand);
            Assert.Equal(0, finalStock.Reserved);

            StockMovement? movement = await verifyDb.StockMovements
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ProductId == productId && m.ReferenceId == orderIdA);

            Assert.NotNull(movement);
            Assert.Equal(StockMovementType.Sale, movement!.Type);
            Assert.Equal(-6, movement.Quantity);
            Assert.Equal(10, movement.OnHandBefore);
            Assert.Equal(4, movement.OnHandAfter);
        }
    }
}
