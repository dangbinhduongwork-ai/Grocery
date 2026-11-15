using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.Modules.Purchasing.Application.DTOs;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.Persistence.Services;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GroceryHub.IntegrationTests.Purchasing;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class MovingAverageCostIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
    private readonly SqlServerTestDatabaseFixture _dbFixture = dbFixture;
    private GroceryHubWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private string _adminToken = null!;

    public async Task InitializeAsync()
    {
        await _dbFixture.ResetDatabaseAsync();
        _factory = new GroceryHubWebApplicationFactory(_dbFixture.ConnectionString);
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        LoginRequest adminLogin = new("admin@groceryhub.vn", "Admin@GroceryHub2026!");
        using HttpResponseMessage adminLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", adminLogin);
        AuthTokenResponse? adminAuth = await adminLoginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(adminAuth);
        _adminToken = adminAuth!.AccessToken;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task MovingAverageCost_ThreeSequentialReceiptScenarios_CalculatesExactIntegerVndMatchingBR11()
    {
        Guid categoryId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        Guid receiverId = Guid.NewGuid();

        // Arrange product with OnHand = 0, CostPrice = 0
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Category category = new()
            {
                Id = categoryId,
                Name = "Gia vị & Nước chấm",
                Slug = $"gia-vi-nuoc-cham-{Guid.NewGuid():N}",
                SortOrder = 1,
                IsActive = true
            };

            Product product = new()
            {
                Id = productId,
                Sku = $"SKU-MAC-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                Name = "Dầu ăn Tường An 1L",
                Slug = $"dau-an-tuong-an-{Guid.NewGuid():N}",
                SearchText = "dau an tuong an 1l",
                CategoryId = categoryId,
                Unit = "Chai",
                ListPrice = 75000,
                SellingPrice = 70000,
                CostPrice = 0L,
                Status = ProductStatus.Active
            };

            StockItem stockItem = new()
            {
                ProductId = productId,
                OnHand = 0,
                Reserved = 0,
                ReorderLevel = 5
            };

            db.Categories.Add(category);
            db.Products.Add(product);
            db.StockItems.Add(stockItem);
            await db.SaveChangesAsync();
        }

        using IServiceScope scope = _factory.Services.CreateScope();
        IPurchasingPersistenceService purchasingService = scope.ServiceProvider.GetRequiredService<IPurchasingPersistenceService>();

        // Scenario 1: Initial OnHand = 0. Receive 10 units @ 50.000đ -> Product CostPrice = 50.000đ
        GoodsReceiptIntakeCommand cmd1 = new(
            ReceiptNumber: $"GR-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            ReceivedBy: receiverId,
            Items: [new GoodsReceiptLineInput(productId, 10, 50000L, null)],
            PurchaseOrderId: null,
            Note: "Scenario 1 intake: 10 @ 50.000đ");

        Result<GoodsReceiptIntakeResult> res1 = await purchasingService.ExecuteGoodsReceiptIntakeAsync(cmd1);
        Assert.True(res1.IsSuccess);

        await using (GroceryHubDbContext db1 = _dbFixture.CreateDbContext())
        {
            Product? p1 = await db1.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
            StockItem? s1 = await db1.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.ProductId == productId);
            Assert.NotNull(p1);
            Assert.NotNull(s1);

            Assert.Equal(50000L, p1!.CostPrice);
            Assert.Equal(10, s1!.OnHand);
            Assert.Equal(0, s1.Reserved);

            StockMovement? m1 = await db1.StockMovements
                .AsNoTracking()
                .OrderByDescending(m => m.At)
                .FirstOrDefaultAsync(m => m.ProductId == productId);

            Assert.NotNull(m1);
            Assert.Equal(StockMovementType.PurchaseReceipt, m1!.Type);
            Assert.Equal(10, m1.Quantity);
            Assert.Equal(0, m1.OnHandBefore);
            Assert.Equal(10, m1.OnHandAfter);
            Assert.Equal(50000L, m1.UnitCost);
        }

        // Scenario 2: Current OnHand = 10 @ 50.000đ. Receive 10 units @ 70.000đ -> Product CostPrice = 60.000đ
        // Calculation: ((10 * 50.000) + (10 * 70.000)) / (10 + 10) = (500.000 + 700.000) / 20 = 1.200.000 / 20 = 60.000đ
        GoodsReceiptIntakeCommand cmd2 = new(
            ReceiptNumber: $"GR-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            ReceivedBy: receiverId,
            Items: [new GoodsReceiptLineInput(productId, 10, 70000L, null)],
            PurchaseOrderId: null,
            Note: "Scenario 2 intake: 10 @ 70.000đ");

        Result<GoodsReceiptIntakeResult> res2 = await purchasingService.ExecuteGoodsReceiptIntakeAsync(cmd2);
        Assert.True(res2.IsSuccess);

        await using (GroceryHubDbContext db2 = _dbFixture.CreateDbContext())
        {
            Product? p2 = await db2.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
            StockItem? s2 = await db2.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.ProductId == productId);
            Assert.NotNull(p2);
            Assert.NotNull(s2);

            Assert.Equal(60000L, p2!.CostPrice);
            Assert.Equal(20, s2!.OnHand);

            StockMovement? m2 = await db2.StockMovements
                .AsNoTracking()
                .OrderByDescending(m => m.At)
                .FirstOrDefaultAsync(m => m.ProductId == productId);

            Assert.NotNull(m2);
            Assert.Equal(StockMovementType.PurchaseReceipt, m2!.Type);
            Assert.Equal(10, m2.Quantity);
            Assert.Equal(10, m2.OnHandBefore);
            Assert.Equal(20, m2.OnHandAfter);
            Assert.Equal(70000L, m2.UnitCost);
        }

        // Scenario 3: Current OnHand = 20 @ 60.000đ. Receive 5 units @ 65.000đ -> Product CostPrice = 61.000đ
        // Calculation: ((20 * 60.000) + (5 * 65.000)) / (20 + 5) = (1.200.000 + 325.000) / 25 = 1.525.000 / 25 = 61.000đ
        GoodsReceiptIntakeCommand cmd3 = new(
            ReceiptNumber: $"GR-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            ReceivedBy: receiverId,
            Items: [new GoodsReceiptLineInput(productId, 5, 65000L, null)],
            PurchaseOrderId: null,
            Note: "Scenario 3 intake: 5 @ 65.000đ");

        Result<GoodsReceiptIntakeResult> res3 = await purchasingService.ExecuteGoodsReceiptIntakeAsync(cmd3);
        Assert.True(res3.IsSuccess);

        await using (GroceryHubDbContext db3 = _dbFixture.CreateDbContext())
        {
            Product? p3 = await db3.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
            StockItem? s3 = await db3.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.ProductId == productId);
            Assert.NotNull(p3);
            Assert.NotNull(s3);

            Assert.Equal(61000L, p3!.CostPrice);
            Assert.Equal(25, s3!.OnHand);

            StockMovement? m3 = await db3.StockMovements
                .AsNoTracking()
                .OrderByDescending(m => m.At)
                .FirstOrDefaultAsync(m => m.ProductId == productId);

            Assert.NotNull(m3);
            Assert.Equal(StockMovementType.PurchaseReceipt, m3!.Type);
            Assert.Equal(5, m3.Quantity);
            Assert.Equal(20, m3.OnHandBefore);
            Assert.Equal(25, m3.OnHandAfter);
            Assert.Equal(65000L, m3.UnitCost);
        }
    }

    [Fact]
    public async Task EndToEnd_PurchaseOrderIntakeViaApi_UpdatesProductCostAndStock()
    {
        // 1. Arrange: Create Supplier, Category, and Product
        Guid categoryId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        Guid supplierId = Guid.NewGuid();

        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Supplier supplier = new()
            {
                Id = supplierId,
                Code = $"SUP-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                Name = "Vinamilk Logistics Supplier",
                ContactPerson = "Nguyen Van Supplier",
                Email = "supplier@vinamilk.vn",
                Phone = "0901234567",
                IsActive = true
            };

            Category category = new()
            {
                Id = categoryId,
                Name = "Sữa và Các sản phẩm từ sữa",
                Slug = $"sua-san-pham-tu-sua-{Guid.NewGuid():N}",
                SortOrder = 1,
                IsActive = true
            };

            Product product = new()
            {
                Id = productId,
                Sku = $"SKU-PO-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                Name = "Sữa tiệt trùng Vinamilk 100% 1L",
                Slug = $"sua-tiet-trung-vinamilk-100-1l-{Guid.NewGuid():N}",
                SearchText = "sua tiet trung vinamilk 100 1l",
                CategoryId = categoryId,
                Unit = "Hộp",
                ListPrice = 38000,
                SellingPrice = 35000,
                CostPrice = 25000,
                Status = ProductStatus.Active
            };

            StockItem stockItem = new()
            {
                ProductId = productId,
                OnHand = 10,
                Reserved = 0,
                ReorderLevel = 5
            };

            db.Suppliers.Add(supplier);
            db.Categories.Add(category);
            db.Products.Add(product);
            db.StockItems.Add(stockItem);
            await db.SaveChangesAsync();
        }

        // 2. Admin creates PO for 20 units @ 30.000đ
        CreatePurchaseOrderRequest poReq = new(
            SupplierId: supplierId,
            Note: "End to end purchasing intake test",
            Items: [new PurchaseOrderItemRequest(productId, 20, 30000L)]);

        using HttpRequestMessage createPoMsg = new(HttpMethod.Post, "/api/v1/admin/purchasing/orders")
        {
            Content = JsonContent.Create(poReq)
        };
        createPoMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        using HttpResponseMessage createPoRes = await _client.SendAsync(createPoMsg);
        Assert.Equal(HttpStatusCode.Created, createPoRes.StatusCode);

        PurchaseOrderDetailDto? po = await createPoRes.Content.ReadFromJsonAsync<PurchaseOrderDetailDto>();
        Assert.NotNull(po);
        Guid poId = po!.Id;
        Guid poItemId = po.Items.First().Id;

        // 3. Admin submits PO (Draft -> Submitted)
        using HttpRequestMessage submitPoMsg = new(HttpMethod.Post, $"/api/v1/admin/purchasing/orders/{poId}/submit");
        submitPoMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        using HttpResponseMessage submitPoRes = await _client.SendAsync(submitPoMsg);
        Assert.Equal(HttpStatusCode.OK, submitPoRes.StatusCode);

        // 4. Admin receives PO (intake 10 units @ 30.000đ)
        // Current: OnHand = 10 @ 25.000đ. Intake: 10 @ 30.000đ.
        // Expected Moving-Average Cost: ((10 * 25.000) + (10 * 30.000)) / 20 = 550.000 / 20 = 27.500đ
        ReceivePurchaseOrderRequest receiveReq = new(
            ReceiptNumber: $"REC-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            Note: "Partial intake 10 units",
            Items: [new ReceivePurchaseOrderItemRequest(poItemId, productId, 10, 30000L)]);

        using HttpRequestMessage receiveMsg = new(HttpMethod.Post, $"/api/v1/admin/purchasing/orders/{poId}/receive")
        {
            Content = JsonContent.Create(receiveReq)
        };
        receiveMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        using HttpResponseMessage receiveRes = await _client.SendAsync(receiveMsg);
        Assert.Equal(HttpStatusCode.OK, receiveRes.StatusCode);

        GoodsReceiptIntakeResult? intakeResult = await receiveRes.Content.ReadFromJsonAsync<GoodsReceiptIntakeResult>();
        Assert.NotNull(intakeResult);
        Assert.Equal("PartiallyReceived", intakeResult!.PurchaseOrderStatus);

        // 5. Verify database: CostPrice = 27.500đ, OnHand = 20
        await using (GroceryHubDbContext verifyDb = _dbFixture.CreateDbContext())
        {
            Product? verifyProduct = await verifyDb.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
            StockItem? verifyStock = await verifyDb.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.ProductId == productId);

            Assert.NotNull(verifyProduct);
            Assert.NotNull(verifyStock);
            Assert.Equal(27500L, verifyProduct!.CostPrice);
            Assert.Equal(20, verifyStock!.OnHand);
        }
    }
}
