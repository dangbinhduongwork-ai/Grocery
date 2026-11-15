using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GroceryHub.Modules.Customers.Application.DTOs;
using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Xunit;

namespace GroceryHub.IntegrationTests;

public static class Phase07TestHelper
{
    public static async Task<(Guid UserId, string AccessToken)> RegisterVerifyAndLoginCustomerAsync(
        GroceryHubWebApplicationFactory factory,
        HttpClient client,
        string email,
        string fullName,
        string phone,
        string password = "Password@123456",
        string? guestCartToken = null)
    {
        RegisterRequest registerReq = new(email, password, fullName, phone);
        using HttpResponseMessage regRes = await client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        Assert.Equal(HttpStatusCode.Created, regRes.StatusCode);
        RegisterResponse? regData = await regRes.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(regData);

        TestEmailRecord? emailRecord = factory.EmailService.VerificationEmails.FirstOrDefault(e => e.UserId == regData!.UserId);
        Assert.NotNull(emailRecord);

        VerifyEmailRequest verifyReq = new(regData!.UserId, emailRecord!.Token);
        using HttpResponseMessage verRes = await client.PostAsJsonAsync("/api/v1/auth/verify-email", verifyReq);
        Assert.Equal(HttpStatusCode.OK, verRes.StatusCode);

        using HttpRequestMessage loginMsg = new(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest(email, password))
        };
        if (!string.IsNullOrWhiteSpace(guestCartToken))
        {
            loginMsg.Headers.Add("Cookie", $"gh_cart={guestCartToken}");
        }
        using HttpResponseMessage loginRes = await client.SendAsync(loginMsg);
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        AuthTokenResponse? auth = await loginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(auth);

        return (regData.UserId, auth!.AccessToken);
    }

    public static async Task<string> LoginAdminAsync(HttpClient client)
    {
        LoginRequest adminLogin = new("admin@groceryhub.vn", "Admin@GroceryHub2026!");
        using HttpResponseMessage adminLoginRes = await client.PostAsJsonAsync("/api/v1/auth/login", adminLogin);
        Assert.Equal(HttpStatusCode.OK, adminLoginRes.StatusCode);
        AuthTokenResponse? adminAuth = await adminLoginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(adminAuth);
        return adminAuth!.AccessToken;
    }

    public static async Task<(Guid UserId, string AccessToken)> CreateAndLoginStaffAsync(
        HttpClient client,
        string adminToken,
        string email,
        string fullName,
        string employeeCode,
        string role = "Staff",
        string[]? extraPermissions = null)
    {
        const string password = "Password@123456";
        CreateStaffRequest staffReq = new(
            Email: email,
            Password: password,
            FullName: fullName,
            Phone: "0981234567",
            EmployeeCode: employeeCode,
            Position: "Operations Staff",
            Role: role,
            ExtraPermissions: extraPermissions);

        using HttpRequestMessage createStaffReq = new(HttpMethod.Post, "/api/v1/admin/staff")
        {
            Content = JsonContent.Create(staffReq)
        };
        createStaffReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using HttpResponseMessage createStaffRes = await client.SendAsync(createStaffReq);
        Assert.Equal(HttpStatusCode.Created, createStaffRes.StatusCode);

        StaffAccountDto? createdStaff = await createStaffRes.Content.ReadFromJsonAsync<StaffAccountDto>();
        Assert.NotNull(createdStaff);

        LoginRequest staffLogin = new(email, password);
        using HttpResponseMessage staffLoginRes = await client.PostAsJsonAsync("/api/v1/auth/login", staffLogin);
        Assert.Equal(HttpStatusCode.OK, staffLoginRes.StatusCode);
        AuthTokenResponse? staffAuth = await staffLoginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(staffAuth);

        return (createdStaff!.UserId, staffAuth!.AccessToken);
    }

    public static async Task<(Guid CategoryId, Guid ProductId)> SeedCategoryAndProductAsync(
        GroceryHubDbContext db,
        string productName,
        string sku,
        long sellingPrice,
        long costPrice,
        int onHand,
        int reserved = 0)
    {
        Guid categoryId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        Category category = new()
        {
            Id = categoryId,
            Name = "Category " + Guid.NewGuid().ToString("N")[..6],
            Slug = "category-" + Guid.NewGuid().ToString("N")[..8],
            SortOrder = 1,
            IsActive = true
        };

        Product product = new()
        {
            Id = productId,
            Sku = sku,
            Name = productName,
            Slug = "prod-" + Guid.NewGuid().ToString("N")[..8],
            SearchText = VietnameseTextNormalizer.NormalizeForSearch(productName, sku),
            CategoryId = categoryId,
            Unit = "Gói",
            ListPrice = sellingPrice + 20_000,
            SellingPrice = sellingPrice,
            CostPrice = costPrice,
            Status = ProductStatus.Active
        };

        StockItem stockItem = new()
        {
            ProductId = productId,
            OnHand = onHand,
            Reserved = reserved,
            ReorderLevel = 2
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        db.StockItems.Add(stockItem);
        await db.SaveChangesAsync();

        return (categoryId, productId);
    }

    public static async Task<Guid> CreateAddressAsync(
        HttpClient client,
        string token,
        string recipientName = "Nguyen Van A",
        string phone = "0912345678",
        string provinceCode = "79",
        string wardCode = "79001",
        string street = "123 Le Loi, Ben Nghe, Quan 1",
        bool isDefault = true)
    {
        CreateAddressRequest req = new(recipientName, phone, provinceCode, wardCode, street, "Giao truoc cong", isDefault);
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/me/addresses")
        {
            Content = JsonContent.Create(req)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        AddressDto? created = await response.Content.ReadFromJsonAsync<AddressDto>();
        Assert.NotNull(created);
        return created!.Id;
    }
}
