using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GroceryHub.Modules.Catalog.Application.DTOs;
using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GroceryHub.IntegrationTests.Catalog;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class ProductPriceHistoryIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
    private readonly SqlServerTestDatabaseFixture _dbFixture = dbFixture;
    private GroceryHubWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private string _adminToken = null!;
    private Guid _adminUserId;

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

        await using GroceryHubDbContext db = _dbFixture.CreateDbContext();
        User adminUser = await db.Users.FirstAsync(u => u.Email == "admin@groceryhub.vn");
        _adminUserId = adminUser.Id;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task UpdatePrice_ValidInput_UpdatesProductPrice_AppendsPriceHistory_AndLogsToAudit()
    {
        // 1. Arrange product with initial prices
        Guid categoryId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Category category = new()
            {
                Id = categoryId,
                Name = "Thực phẩm Khô",
                Slug = $"thuc-pham-kho-{Guid.NewGuid():N}",
                SortOrder = 1,
                IsActive = true
            };

            Product product = new()
            {
                Id = productId,
                Sku = $"SKU-PRICE-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                Name = "Mì Hảo Hảo Tôm Chua Cay Thùng 30 Gói",
                Slug = $"mi-hao-hao-thung-30-goi-{Guid.NewGuid():N}",
                SearchText = "mi hao hao tom chua cay thung 30 goi",
                CategoryId = categoryId,
                Unit = "Thùng",
                ListPrice = 120000L,
                SellingPrice = 110000L,
                CostPrice = 95000L,
                Status = ProductStatus.Active
            };

            db.Categories.Add(category);
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        // 2. Act: Admin calls PUT /api/v1/admin/products/{id}/price
        UpdateProductPriceRequest updateReq = new(
            ListPrice: 135000L,
            SellingPrice: 125000L,
            Reason: "Dieu chinh gia theo nha cung cap Acecook Q4");

        using HttpRequestMessage request = new(HttpMethod.Put, $"/api/v1/admin/products/{productId}/price")
        {
            Content = JsonContent.Create(updateReq)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        using HttpResponseMessage response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ProductPriceResponse? priceRes = await response.Content.ReadFromJsonAsync<ProductPriceResponse>();
        Assert.NotNull(priceRes);
        Assert.Equal(productId, priceRes!.Id);
        Assert.Equal(135000L, priceRes.ListPrice);
        Assert.Equal(125000L, priceRes.SellingPrice);

        // 3. Assert database: Product price updated
        await using (GroceryHubDbContext verifyDb = _dbFixture.CreateDbContext())
        {
            Product? updatedProduct = await verifyDb.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
            Assert.NotNull(updatedProduct);
            Assert.Equal(135000L, updatedProduct!.ListPrice);
            Assert.Equal(125000L, updatedProduct.SellingPrice);

            // 4. Assert database: ProductPriceHistories records old and new prices
            ProductPriceHistory? history = await verifyDb.ProductPriceHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.ProductId == productId);

            Assert.NotNull(history);
            Assert.Equal(120000L, history!.OldListPrice);
            Assert.Equal(135000L, history.NewListPrice);
            Assert.Equal(110000L, history.OldSellingPrice);
            Assert.Equal(125000L, history.NewSellingPrice);
            Assert.Equal("Dieu chinh gia theo nha cung cap Acecook Q4", history.Reason);
            Assert.Equal(_adminUserId, history.ChangedBy);

            // 5. Assert database: audit.AuditLogs records the action
            AuditLog? auditLog = await verifyDb.AuditLogs
                .AsNoTracking()
                .OrderByDescending(a => a.At)
                .FirstOrDefaultAsync(a => a.EntityType == "Product" && a.EntityId == productId.ToString());

            Assert.NotNull(auditLog);
            Assert.Equal("UpdateProductPrice", auditLog!.Action);
            Assert.Equal(_adminUserId, auditLog.ActorUserId);
            Assert.Equal("Admin", auditLog.ActorRole);
            Assert.Contains("110000", auditLog.BeforeJson ?? string.Empty);
            Assert.Contains("125000", auditLog.AfterJson ?? string.Empty);
            Assert.Contains("Dieu chinh gia theo nha cung cap Acecook Q4", auditLog.AfterJson ?? string.Empty);
        }
    }

    [Fact]
    public async Task UpdatePrice_SellingPriceExceedsListPrice_Returns422UnprocessableEntity()
    {
        Guid categoryId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Category category = new()
            {
                Id = categoryId,
                Name = "Danh Muc Kiem Tra Gia",
                Slug = $"danh-muc-kiem-tra-gia-{Guid.NewGuid():N}",
                SortOrder = 1,
                IsActive = true
            };

            Product product = new()
            {
                Id = productId,
                Sku = $"SKU-INVPRICE-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                Name = "San Pham Kiem Tra Gia",
                Slug = $"san-pham-kiem-tra-gia-{Guid.NewGuid():N}",
                SearchText = "san pham kiem tra gia",
                CategoryId = categoryId,
                Unit = "Hop",
                ListPrice = 100000L,
                SellingPrice = 90000L,
                CostPrice = 70000L,
                Status = ProductStatus.Active
            };

            db.Categories.Add(category);
            db.Products.Add(product);
            await db.SaveChangesAsync();
        }

        // SellingPrice = 150.000 > ListPrice = 100.000 (Violates BR-01)
        UpdateProductPriceRequest invalidReq = new(
            ListPrice: 100000L,
            SellingPrice: 150000L,
            Reason: "Gia khong hop le");

        using HttpRequestMessage request = new(HttpMethod.Put, $"/api/v1/admin/products/{productId}/price")
        {
            Content = JsonContent.Create(invalidReq)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        using HttpResponseMessage response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        // Verify database: product price unchanged, no history added
        await using (GroceryHubDbContext verifyDb = _dbFixture.CreateDbContext())
        {
            Product? product = await verifyDb.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
            Assert.NotNull(product);
            Assert.Equal(100000L, product!.ListPrice);
            Assert.Equal(90000L, product.SellingPrice);

            bool hasHistory = await verifyDb.ProductPriceHistories.AnyAsync(h => h.ProductId == productId);
            Assert.False(hasHistory);
        }
    }
}
