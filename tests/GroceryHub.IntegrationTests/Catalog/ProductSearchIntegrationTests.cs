using System.Net;
using System.Net.Http.Json;
using GroceryHub.Modules.Catalog.Application.DTOs;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Xunit;

namespace GroceryHub.IntegrationTests.Catalog;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class ProductSearchIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
    private readonly SqlServerTestDatabaseFixture _dbFixture = dbFixture;
    private GroceryHubWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _dbFixture.ResetDatabaseAsync();
        _factory = new GroceryHubWebApplicationFactory(_dbFixture.ConnectionString);
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        // Seed Vietnamese grocery products
        Guid categoryId = Guid.NewGuid();

        await using GroceryHubDbContext db = _dbFixture.CreateDbContext();

        Category category = new()
        {
            Id = categoryId,
            Name = "Bách Hóa Tổng Hợp",
            Slug = $"bach-hoa-tong-hop-{Guid.NewGuid():N}",
            SortOrder = 1,
            IsActive = true
        };
        db.Categories.Add(category);

        List<Product> products =
        [
            new Product
            {
                Id = Guid.NewGuid(),
                Sku = "SKU-SUA-TUOI-01",
                Name = "Sữa tươi tiệt trùng Vinamilk ít đường 1L",
                Slug = "sua-tuoi-tiet-trung-vinamilk-it-duong-1l",
                SearchText = VietnameseTextNormalizer.NormalizeForSearch("Sữa tươi tiệt trùng Vinamilk ít đường 1L SKU-SUA-TUOI-01"),
                CategoryId = categoryId,
                Unit = "Hộp",
                ListPrice = 38000L,
                SellingPrice = 35000L,
                CostPrice = 28000L,
                Status = ProductStatus.Active
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Sku = "SKU-NUOC-MAM-01",
                Name = "Nước mắm Nam Ngư Đệ Nhị chai 900ml",
                Slug = "nuoc-mam-nam-ngu-de-nhi-chai-900ml",
                SearchText = VietnameseTextNormalizer.NormalizeForSearch("Nước mắm Nam Ngư Đệ Nhị chai 900ml SKU-NUOC-MAM-01"),
                CategoryId = categoryId,
                Unit = "Chai",
                ListPrice = 32000L,
                SellingPrice = 29000L,
                CostPrice = 22000L,
                Status = ProductStatus.Active
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Sku = "SKU-RAU-CU-01",
                Name = "Rau củ hỗn hợp đông lạnh Đà Lạt 500g",
                Slug = "rau-cu-hon-hop-dong-lanh-da-lat-500g",
                SearchText = VietnameseTextNormalizer.NormalizeForSearch("Rau củ hỗn hợp đông lạnh Đà Lạt 500g SKU-RAU-CU-01"),
                CategoryId = categoryId,
                Unit = "Gói",
                ListPrice = 45000L,
                SellingPrice = 42000L,
                CostPrice = 30000L,
                Status = ProductStatus.Active
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Sku = "SKU-GAO-ST25-01",
                Name = "Gạo ST25 Ông Cua túi 5kg chuẩn Sóc Trăng",
                Slug = "gao-st25-ong-cua-tui-5kg",
                SearchText = VietnameseTextNormalizer.NormalizeForSearch("Gạo ST25 Ông Cua túi 5kg chuẩn Sóc Trăng SKU-GAO-ST25-01"),
                CategoryId = categoryId,
                Unit = "Túi",
                ListPrice = 200000L,
                SellingPrice = 185000L,
                CostPrice = 150000L,
                Status = ProductStatus.Active
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Sku = "SKU-DRAFT-ONLY-99",
                Name = "Sản Phẩm Bản Thảo Chưa Xuất Bản DraftOnlyUniq99",
                Slug = "san-pham-ban-thao-chua-xuat-ban",
                SearchText = VietnameseTextNormalizer.NormalizeForSearch("Sản Phẩm Bản Thảo Chưa Xuất Bản DraftOnlyUniq99 SKU-DRAFT-ONLY-99"),
                CategoryId = categoryId,
                Unit = "Lốc",
                ListPrice = 28000L,
                SellingPrice = 25000L,
                CostPrice = 18000L,
                Status = ProductStatus.Draft // Must NOT appear in storefront search
            }
        ];

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Theory]
    [InlineData("sua tuoi", "Sữa tươi tiệt trùng Vinamilk ít đường 1L")]
    [InlineData("SUA TUOI", "Sữa tươi tiệt trùng Vinamilk ít đường 1L")]
    [InlineData("Sữa tươi", "Sữa tươi tiệt trùng Vinamilk ít đường 1L")]
    [InlineData("sUa tUoi", "Sữa tươi tiệt trùng Vinamilk ít đường 1L")]
    [InlineData("nuoc mam", "Nước mắm Nam Ngư Đệ Nhị chai 900ml")]
    [InlineData("NUOC MAM", "Nước mắm Nam Ngư Đệ Nhị chai 900ml")]
    [InlineData("Nước mắm", "Nước mắm Nam Ngư Đệ Nhị chai 900ml")]
    [InlineData("rau cu", "Rau củ hỗn hợp đông lạnh Đà Lạt 500g")]
    [InlineData("RAU CU", "Rau củ hỗn hợp đông lạnh Đà Lạt 500g")]
    [InlineData("Rau củ", "Rau củ hỗn hợp đông lạnh Đà Lạt 500g")]
    [InlineData("da lat", "Rau củ hỗn hợp đông lạnh Đà Lạt 500g")]
    [InlineData("gao st25", "Gạo ST25 Ông Cua túi 5kg chuẩn Sóc Trăng")]
    [InlineData("GAO ST25", "Gạo ST25 Ông Cua túi 5kg chuẩn Sóc Trăng")]
    [InlineData("soc trang", "Gạo ST25 Ông Cua túi 5kg chuẩn Sóc Trăng")]
    public async Task SearchProducts_AccentAndCaseInsensitive_ReturnsExpectedProduct(string query, string expectedName)
    {
        using HttpResponseMessage response = await _client.GetAsync($"/api/v1/products?q={Uri.EscapeDataString(query)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PagedResponse<StorefrontProductCardDto>? result = await response.Content.ReadFromJsonAsync<PagedResponse<StorefrontProductCardDto>>();
        Assert.NotNull(result);
        Assert.NotEmpty(result!.Items);
        Assert.Contains(result.Items, p => p.Name == expectedName);
    }

    [Fact]
    public async Task SearchProducts_DraftProductsAreNeverReturnedToPublicStorefront()
    {
        using HttpResponseMessage response = await _client.GetAsync("/api/v1/products?q=DraftOnlyUniq99");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PagedResponse<StorefrontProductCardDto>? result = await response.Content.ReadFromJsonAsync<PagedResponse<StorefrontProductCardDto>>();
        Assert.NotNull(result);
        Assert.Empty(result!.Items);
    }
}
