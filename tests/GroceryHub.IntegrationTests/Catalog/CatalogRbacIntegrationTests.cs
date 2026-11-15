using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GroceryHub.Modules.Catalog.Application.DTOs;
using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.Modules.Inventory.Application.DTOs;
using GroceryHub.Modules.Purchasing.Application.DTOs;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using Xunit;

namespace GroceryHub.IntegrationTests.Catalog;

public enum CatalogTestPersona
{
    Anonymous,
    CustomerA,
    CustomerB,
    Staff,
    Manager,
    Admin
}

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class CatalogRbacIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
    private readonly SqlServerTestDatabaseFixture _dbFixture = dbFixture;
    private GroceryHubWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    private readonly Dictionary<CatalogTestPersona, string?> _tokens = new()
    {
        [CatalogTestPersona.Anonymous] = null
    };

    private Guid _categoryId;
    private Guid _productId;
    private string _productSlug = null!;
    private Guid _supplierId;
    private Guid _purchaseOrderId;

    public async Task InitializeAsync()
    {
        await _dbFixture.ResetDatabaseAsync();
        _factory = new GroceryHubWebApplicationFactory(_dbFixture.ConnectionString);
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        // 1. Admin login
        LoginRequest adminLogin = new("admin@groceryhub.vn", "Admin@GroceryHub2026!");
        using HttpResponseMessage adminLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", adminLogin);
        AuthTokenResponse? adminAuth = await adminLoginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(adminAuth);
        _tokens[CatalogTestPersona.Admin] = adminAuth!.AccessToken;

        // 2. Create and login Manager
        CreateStaffRequest managerReq = new(
            Email: "manager.catalog@groceryhub.vn",
            Password: "Password@123456",
            FullName: "Hoang Manager Catalog",
            Phone: "0981119988",
            EmployeeCode: "MGR-CAT-01",
            Position: "Store Manager",
            Role: "Manager",
            ExtraPermissions: null);

        using HttpRequestMessage createMgrReq = new(HttpMethod.Post, "/api/v1/admin/staff")
        {
            Content = JsonContent.Create(managerReq)
        };
        createMgrReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[CatalogTestPersona.Admin]);
        using HttpResponseMessage createMgrRes = await _client.SendAsync(createMgrReq);
        Assert.Equal(HttpStatusCode.Created, createMgrRes.StatusCode);

        LoginRequest mgrLogin = new(managerReq.Email, managerReq.Password);
        using HttpResponseMessage mgrLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", mgrLogin);
        AuthTokenResponse? mgrAuth = await mgrLoginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(mgrAuth);
        _tokens[CatalogTestPersona.Manager] = mgrAuth!.AccessToken;

        // 3. Create and login Staff
        CreateStaffRequest staffReq = new(
            Email: "staff.catalog@groceryhub.vn",
            Password: "Password@123456",
            FullName: "Vu Staff Catalog",
            Phone: "0982229977",
            EmployeeCode: "STF-CAT-01",
            Position: "Inventory Associate",
            Role: "Staff",
            ExtraPermissions: null);

        using HttpRequestMessage createStfReq = new(HttpMethod.Post, "/api/v1/admin/staff")
        {
            Content = JsonContent.Create(staffReq)
        };
        createStfReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[CatalogTestPersona.Admin]);
        using HttpResponseMessage createStfRes = await _client.SendAsync(createStfReq);
        Assert.Equal(HttpStatusCode.Created, createStfRes.StatusCode);

        LoginRequest stfLogin = new(staffReq.Email, staffReq.Password);
        using HttpResponseMessage stfLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", stfLogin);
        AuthTokenResponse? stfAuth = await stfLoginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(stfAuth);
        _tokens[CatalogTestPersona.Staff] = stfAuth!.AccessToken;

        // 4. Register and login Customer A
        RegisterRequest regReqA = new("customerA.catalog@groceryhub.vn", "Password@123456", "Dang Customer A", "0983339966");
        using HttpResponseMessage regResA = await _client.PostAsJsonAsync("/api/v1/auth/register", regReqA);
        Assert.Equal(HttpStatusCode.Created, regResA.StatusCode);
        RegisterResponse? regDataA = await regResA.Content.ReadFromJsonAsync<RegisterResponse>();

        TestEmailRecord emailRecordA = _factory.EmailService.VerificationEmails.First(e => e.UserId == regDataA!.UserId);
        VerifyEmailRequest verifyReqA = new(regDataA!.UserId, emailRecordA.Token);
        using HttpResponseMessage verResA = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", verifyReqA);
        Assert.Equal(HttpStatusCode.OK, verResA.StatusCode);

        LoginRequest custLoginA = new(regReqA.Email, regReqA.Password);
        using HttpResponseMessage custLoginResA = await _client.PostAsJsonAsync("/api/v1/auth/login", custLoginA);
        AuthTokenResponse? custAuthA = await custLoginResA.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(custAuthA);
        _tokens[CatalogTestPersona.CustomerA] = custAuthA!.AccessToken;

        // 5. Register and login Customer B
        RegisterRequest regReqB = new("customerB.catalog@groceryhub.vn", "Password@123456", "Dang Customer B", "0983339955");
        using HttpResponseMessage regResB = await _client.PostAsJsonAsync("/api/v1/auth/register", regReqB);
        Assert.Equal(HttpStatusCode.Created, regResB.StatusCode);
        RegisterResponse? regDataB = await regResB.Content.ReadFromJsonAsync<RegisterResponse>();

        TestEmailRecord emailRecordB = _factory.EmailService.VerificationEmails.First(e => e.UserId == regDataB!.UserId);
        VerifyEmailRequest verifyReqB = new(regDataB!.UserId, emailRecordB.Token);
        using HttpResponseMessage verResB = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", verifyReqB);
        Assert.Equal(HttpStatusCode.OK, verResB.StatusCode);

        LoginRequest custLoginB = new(regReqB.Email, regReqB.Password);
        using HttpResponseMessage custLoginResB = await _client.PostAsJsonAsync("/api/v1/auth/login", custLoginB);
        AuthTokenResponse? custAuthB = await custLoginResB.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(custAuthB);
        _tokens[CatalogTestPersona.CustomerB] = custAuthB!.AccessToken;

        // 5. Seed Category, Product, Stock, Supplier, and PO
        _categoryId = Guid.NewGuid();
        _productId = Guid.NewGuid();
        _productSlug = $"tra-xanh-o-long-teaplus-{Guid.NewGuid():N}";
        _supplierId = Guid.NewGuid();
        _purchaseOrderId = Guid.NewGuid();

        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Category category = new()
            {
                Id = _categoryId,
                Name = "Đồ uống giải khát",
                Slug = $"do-uong-giai-khat-{Guid.NewGuid():N}",
                SortOrder = 1,
                IsActive = true
            };

            Product product = new()
            {
                Id = _productId,
                Sku = $"SKU-RBAC-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                Name = "Trà Xanh Ô Long TeaPlus Chai 455ml",
                Slug = _productSlug,
                SearchText = "tra xanh o long teaplus chai 455ml",
                CategoryId = _categoryId,
                Unit = "Chai",
                ListPrice = 12000L,
                SellingPrice = 10000L,
                CostPrice = 7500L,
                Status = ProductStatus.Active
            };

            StockItem stockItem = new()
            {
                ProductId = _productId,
                OnHand = 50,
                Reserved = 0,
                ReorderLevel = 10
            };

            Supplier supplier = new()
            {
                Id = _supplierId,
                Code = $"SUP-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                Name = "Suntory Pepsico Vietnam",
                ContactPerson = "Pham Purchasing Manager",
                Email = "sales@pepsico.vn",
                Phone = "0909123456",
                IsActive = true
            };

            PurchaseOrder po = new()
            {
                Id = _purchaseOrderId,
                PoNumber = $"PO-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                SupplierId = _supplierId,
                Status = PurchaseOrderStatus.Draft,
                TotalAmount = 75000L,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = Guid.NewGuid()
            };

            PurchaseOrderItem poItem = new()
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = _purchaseOrderId,
                ProductId = _productId,
                OrderedQuantity = 10,
                ReceivedQuantity = 0,
                UnitCost = 7500L,
                Subtotal = 75000L,
                CreatedAt = DateTimeOffset.UtcNow
            };
            po.Items.Add(poItem);

            db.Categories.Add(category);
            db.Products.Add(product);
            db.StockItems.Add(stockItem);
            db.Suppliers.Add(supplier);
            db.PurchaseOrders.Add(po);
            await db.SaveChangesAsync();
        }
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CostPriceVisibility_AdminAndManagerCanReadCost_StaffAndCustomerCannot()
    {
        // 1. Admin GET /api/v1/admin/products/{id} -> 200 OK, CostPrice == 7500
        using HttpRequestMessage adminReq = new(HttpMethod.Get, $"/api/v1/admin/products/{_productId}");
        adminReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[CatalogTestPersona.Admin]);
        using HttpResponseMessage adminRes = await _client.SendAsync(adminReq);
        Assert.Equal(HttpStatusCode.OK, adminRes.StatusCode);
        AdminProductDetailDto? adminProduct = await adminRes.Content.ReadFromJsonAsync<AdminProductDetailDto>();
        Assert.NotNull(adminProduct);
        Assert.Equal(7500L, adminProduct!.CostPrice);

        // 2. Manager GET /api/v1/admin/products/{id} -> 200 OK, CostPrice == 7500
        using HttpRequestMessage mgrReq = new(HttpMethod.Get, $"/api/v1/admin/products/{_productId}");
        mgrReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[CatalogTestPersona.Manager]);
        using HttpResponseMessage mgrRes = await _client.SendAsync(mgrReq);
        Assert.Equal(HttpStatusCode.OK, mgrRes.StatusCode);
        AdminProductDetailDto? mgrProduct = await mgrRes.Content.ReadFromJsonAsync<AdminProductDetailDto>();
        Assert.NotNull(mgrProduct);
        Assert.Equal(7500L, mgrProduct!.CostPrice);

        // 3. Staff GET /api/v1/admin/products/{id} -> 200 OK, CostPrice == null (masked due to missing catalog.cost.read)
        using HttpRequestMessage staffReq = new(HttpMethod.Get, $"/api/v1/admin/products/{_productId}");
        staffReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[CatalogTestPersona.Staff]);
        using HttpResponseMessage staffRes = await _client.SendAsync(staffReq);
        Assert.Equal(HttpStatusCode.OK, staffRes.StatusCode);
        AdminProductDetailDto? staffProduct = await staffRes.Content.ReadFromJsonAsync<AdminProductDetailDto>();
        Assert.NotNull(staffProduct);
        Assert.Null(staffProduct!.CostPrice);

        // 4. Customer GET /api/v1/admin/products/{id} -> 403 Forbidden (no catalog.read permission)
        using HttpRequestMessage custAdminReq = new(HttpMethod.Get, $"/api/v1/admin/products/{_productId}");
        custAdminReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[CatalogTestPersona.CustomerA]);
        using HttpResponseMessage custAdminRes = await _client.SendAsync(custAdminReq);
        Assert.Equal(HttpStatusCode.Forbidden, custAdminRes.StatusCode);

        // 5. Customer GET /api/v1/products/{slug} (Storefront) -> 200 OK, DTO does not even have CostPrice field
        using HttpResponseMessage storefrontRes = await _client.GetAsync($"/api/v1/products/{_productSlug}");
        Assert.Equal(HttpStatusCode.OK, storefrontRes.StatusCode);
        StorefrontProductDetailDto? storefrontProduct = await storefrontRes.Content.ReadFromJsonAsync<StorefrontProductDetailDto>();
        Assert.NotNull(storefrontProduct);
        Assert.Equal(10000L, storefrontProduct!.SellingPrice);
        Assert.Equal(12000L, storefrontProduct.ListPrice);
    }

    [Fact]
    public async Task InventoryAdjust_StaffDenied_ManagerAndAdminAllowed()
    {
        AdminAdjustStockRequest adjustReq = new(
            ProductId: _productId,
            Delta: 5,
            Reason: "Kiem ke kho dinh ky bu ton");

        // 1. Staff POST /api/v1/admin/inventory/adjust -> 403 Forbidden (missing inventory.adjust)
        using HttpRequestMessage staffReq = new(HttpMethod.Post, "/api/v1/admin/inventory/adjust")
        {
            Content = JsonContent.Create(adjustReq)
        };
        staffReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[CatalogTestPersona.Staff]);
        using HttpResponseMessage staffRes = await _client.SendAsync(staffReq);
        Assert.Equal(HttpStatusCode.Forbidden, staffRes.StatusCode);

        // 2. Manager POST /api/v1/admin/inventory/adjust -> 200 OK
        using HttpRequestMessage mgrReq = new(HttpMethod.Post, "/api/v1/admin/inventory/adjust")
        {
            Content = JsonContent.Create(adjustReq)
        };
        mgrReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[CatalogTestPersona.Manager]);
        using HttpResponseMessage mgrRes = await _client.SendAsync(mgrReq);
        Assert.Equal(HttpStatusCode.OK, mgrRes.StatusCode);

        // 3. Admin POST /api/v1/admin/inventory/adjust -> 200 OK
        using HttpRequestMessage adminReq = new(HttpMethod.Post, "/api/v1/admin/inventory/adjust")
        {
            Content = JsonContent.Create(adjustReq)
        };
        adminReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[CatalogTestPersona.Admin]);
        using HttpResponseMessage adminRes = await _client.SendAsync(adminReq);
        Assert.Equal(HttpStatusCode.OK, adminRes.StatusCode);
    }

    [Fact]
    public async Task PurchasingWrite_StaffDenied_ManagerAndAdminAllowed()
    {
        // 1. Staff attempts to submit PO -> 403 Forbidden (missing purchasing.write)
        using HttpRequestMessage staffSubmitReq = new(HttpMethod.Post, $"/api/v1/admin/purchasing/orders/{_purchaseOrderId}/submit");
        staffSubmitReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[CatalogTestPersona.Staff]);
        using HttpResponseMessage staffSubmitRes = await _client.SendAsync(staffSubmitReq);
        Assert.Equal(HttpStatusCode.Forbidden, staffSubmitRes.StatusCode);

        // 2. Staff attempts to cancel PO -> 403 Forbidden (missing purchasing.write)
        using HttpRequestMessage staffCancelReq = new(HttpMethod.Post, $"/api/v1/admin/purchasing/orders/{_purchaseOrderId}/cancel")
        {
            Content = JsonContent.Create(new CancelPurchaseOrderRequest("Staff test cancellation"))
        };
        staffCancelReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[CatalogTestPersona.Staff]);
        using HttpResponseMessage staffCancelRes = await _client.SendAsync(staffCancelReq);
        Assert.Equal(HttpStatusCode.Forbidden, staffCancelRes.StatusCode);

        // 3. Manager submits PO -> 200 OK (has purchasing.write)
        using HttpRequestMessage mgrSubmitReq = new(HttpMethod.Post, $"/api/v1/admin/purchasing/orders/{_purchaseOrderId}/submit");
        mgrSubmitReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[CatalogTestPersona.Manager]);
        using HttpResponseMessage mgrSubmitRes = await _client.SendAsync(mgrSubmitReq);
        Assert.Equal(HttpStatusCode.OK, mgrSubmitRes.StatusCode);
    }

    [Theory]
    [InlineData(CatalogTestPersona.Anonymous, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.CustomerA, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.CustomerB, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.Staff, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.Manager, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.Admin, HttpStatusCode.OK)]
    public async Task Storefront_SearchProducts_AllowAnonymous_AllPersonas200(CatalogTestPersona persona, HttpStatusCode expectedStatus)
    {
        using HttpResponseMessage response = await SendRequestAsync(persona, HttpMethod.Get, "/api/v1/products");
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(CatalogTestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(CatalogTestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.Staff, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.Manager, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.Admin, HttpStatusCode.OK)]
    public async Task Admin_GetProducts_PolicyMatrix(CatalogTestPersona persona, HttpStatusCode expectedStatus)
    {
        using HttpResponseMessage response = await SendRequestAsync(persona, HttpMethod.Get, "/api/v1/admin/products");
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(CatalogTestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(CatalogTestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.Staff, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.Manager, HttpStatusCode.Created)]
    [InlineData(CatalogTestPersona.Admin, HttpStatusCode.Created)]
    public async Task Admin_CreateProduct_PolicyMatrix(CatalogTestPersona persona, HttpStatusCode expectedStatus)
    {
        CreateProductRequest req = new(
            Sku: $"SKU-M-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            Barcode: null,
            Name: $"Matrix Product {persona}",
            Slug: $"matrix-product-{Guid.NewGuid():N}",
            CategoryId: _categoryId,
            BrandId: null,
            Unit: "Hộp",
            PackSize: null,
            ListPrice: 50000,
            SellingPrice: 45000,
            InitialCostPrice: 30000,
            Status: "Draft",
            ShortDescription: "Matrix test short desc",
            Description: "Matrix test desc",
            SpecsJson: null,
            IsFeatured: false,
            ExpiryTracked: false,
            InitialReorderLevel: 5);

        using HttpResponseMessage response = await SendRequestAsync(
            persona,
            HttpMethod.Post,
            "/api/v1/admin/products",
            JsonContent.Create(req));

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(CatalogTestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(CatalogTestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.Staff, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.Manager, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.Admin, HttpStatusCode.OK)]
    public async Task Admin_UpdateProductPrice_PolicyMatrix(CatalogTestPersona persona, HttpStatusCode expectedStatus)
    {
        UpdateProductPriceRequest req = new(
            ListPrice: 60000,
            SellingPrice: 50000,
            Reason: "Matrix price test");

        using HttpResponseMessage response = await SendRequestAsync(
            persona,
            HttpMethod.Put,
            $"/api/v1/admin/products/{_productId}/price",
            JsonContent.Create(req));

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(CatalogTestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(CatalogTestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.Staff, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.Manager, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.Admin, HttpStatusCode.OK)]
    public async Task Admin_GetInventory_PolicyMatrix(CatalogTestPersona persona, HttpStatusCode expectedStatus)
    {
        using HttpResponseMessage response = await SendRequestAsync(persona, HttpMethod.Get, "/api/v1/admin/inventory");
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(CatalogTestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(CatalogTestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.Staff, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.Manager, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.Admin, HttpStatusCode.OK)]
    public async Task Admin_AdjustStock_PolicyMatrix(CatalogTestPersona persona, HttpStatusCode expectedStatus)
    {
        AdminAdjustStockRequest req = new(
            ProductId: _productId,
            Delta: 1,
            Reason: "Matrix stock adjust");

        using HttpResponseMessage response = await SendRequestAsync(
            persona,
            HttpMethod.Post,
            "/api/v1/admin/inventory/adjust",
            JsonContent.Create(req));

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(CatalogTestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(CatalogTestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.Staff, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.Manager, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.Admin, HttpStatusCode.OK)]
    public async Task Admin_GetSuppliers_PolicyMatrix(CatalogTestPersona persona, HttpStatusCode expectedStatus)
    {
        using HttpResponseMessage response = await SendRequestAsync(persona, HttpMethod.Get, "/api/v1/admin/suppliers");
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(CatalogTestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(CatalogTestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.Staff, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.Manager, HttpStatusCode.Created)]
    [InlineData(CatalogTestPersona.Admin, HttpStatusCode.Created)]
    public async Task Admin_CreateSupplier_PolicyMatrix(CatalogTestPersona persona, HttpStatusCode expectedStatus)
    {
        CreateSupplierRequest req = new(
            Code: $"SUP-M-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
            Name: $"Matrix Supplier {persona}",
            ContactPerson: "Contact Person",
            Phone: "0901234567",
            Email: $"supplier.{Guid.NewGuid():N}@matrix.vn",
            Address: "123 Matrix St",
            TaxCode: "0101234567",
            Note: "Matrix test supplier note");

        using HttpResponseMessage response = await SendRequestAsync(
            persona,
            HttpMethod.Post,
            "/api/v1/admin/suppliers",
            JsonContent.Create(req));

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(CatalogTestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(CatalogTestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(CatalogTestPersona.Staff, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.Manager, HttpStatusCode.OK)]
    [InlineData(CatalogTestPersona.Admin, HttpStatusCode.OK)]
    public async Task Admin_GetPurchaseOrders_PolicyMatrix(CatalogTestPersona persona, HttpStatusCode expectedStatus)
    {
        using HttpResponseMessage response = await SendRequestAsync(persona, HttpMethod.Get, "/api/v1/admin/purchase-orders");
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        CatalogTestPersona persona,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        HttpRequestMessage request = new(method, url)
        {
            Content = content
        };

        string? token = _tokens[persona];
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await _client.SendAsync(request);
    }
}
