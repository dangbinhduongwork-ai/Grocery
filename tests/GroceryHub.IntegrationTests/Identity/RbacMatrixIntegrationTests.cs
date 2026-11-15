using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GroceryHub.Modules.Identity.Contracts;
using Xunit;

namespace GroceryHub.IntegrationTests.Identity;

public enum TestPersona
{
    Anonymous,
    CustomerA,
    CustomerB,
    Staff,
    Manager,
    Admin
}

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class RbacMatrixIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
    private readonly SqlServerTestDatabaseFixture _dbFixture = dbFixture;
    private GroceryHubWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    private readonly Dictionary<TestPersona, string?> _tokens = new()
    {
        [TestPersona.Anonymous] = null
    };

    private Guid _staffUserId;
    private Guid _roleAdminId;
    private Guid _roleManagerId;
    private Guid _roleStaffId;

    public async Task InitializeAsync()
    {
        await _dbFixture.ResetDatabaseAsync();
        _factory = new GroceryHubWebApplicationFactory(_dbFixture.ConnectionString);
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        // 1. Admin setup
        LoginRequest adminLogin = new("admin@groceryhub.vn", "Admin@GroceryHub2026!");
        using HttpResponseMessage adminLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", adminLogin);
        AuthTokenResponse? adminAuth = await adminLoginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(adminAuth);
        _tokens[TestPersona.Admin] = adminAuth!.AccessToken;

        // Fetch system role IDs
        using HttpRequestMessage getRolesReq = new(HttpMethod.Get, "/api/v1/admin/roles");
        getRolesReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[TestPersona.Admin]);
        using HttpResponseMessage rolesRes = await _client.SendAsync(getRolesReq);
        List<RolePermissionMatrixDto>? roles = await rolesRes.Content.ReadFromJsonAsync<List<RolePermissionMatrixDto>>();
        Assert.NotNull(roles);
        _roleAdminId = roles!.First(r => r.Name == "Admin").RoleId;
        _roleManagerId = roles!.First(r => r.Name == "Manager").RoleId;
        _roleStaffId = roles!.First(r => r.Name == "Staff").RoleId;

        // 2. Manager setup (created by Admin)
        CreateStaffRequest managerReq = new(
            Email: "manager.matrix@groceryhub.vn",
            Password: "Password@123456",
            FullName: "Tran Manager Matrix",
            Phone: "0981112233",
            EmployeeCode: "MGR-MAT-01",
            Position: "Operations Manager",
            Role: "Manager",
            ExtraPermissions: null);

        using HttpRequestMessage createMgrReq = new(HttpMethod.Post, "/api/v1/admin/staff");
        createMgrReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[TestPersona.Admin]);
        createMgrReq.Content = JsonContent.Create(managerReq);
        using HttpResponseMessage createMgrRes = await _client.SendAsync(createMgrReq);
        Assert.Equal(HttpStatusCode.Created, createMgrRes.StatusCode);

        LoginRequest mgrLogin = new(managerReq.Email, managerReq.Password);
        using HttpResponseMessage mgrLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", mgrLogin);
        AuthTokenResponse? mgrAuth = await mgrLoginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(mgrAuth);
        _tokens[TestPersona.Manager] = mgrAuth!.AccessToken;

        // 3. Staff setup (created by Admin)
        CreateStaffRequest staffReq = new(
            Email: "staff.matrix@groceryhub.vn",
            Password: "Password@123456",
            FullName: "Le Staff Matrix",
            Phone: "0982223344",
            EmployeeCode: "STF-MAT-01",
            Position: "Warehouse Associate",
            Role: "Staff",
            ExtraPermissions: null);

        using HttpRequestMessage createStfReq = new(HttpMethod.Post, "/api/v1/admin/staff");
        createStfReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens[TestPersona.Admin]);
        createStfReq.Content = JsonContent.Create(staffReq);
        using HttpResponseMessage createStfRes = await _client.SendAsync(createStfReq);
        Assert.Equal(HttpStatusCode.Created, createStfRes.StatusCode);

        StaffAccountDto? staffAccount = await createStfRes.Content.ReadFromJsonAsync<StaffAccountDto>();
        _staffUserId = staffAccount!.UserId;

        LoginRequest stfLogin = new(staffReq.Email, staffReq.Password);
        using HttpResponseMessage stfLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", stfLogin);
        AuthTokenResponse? stfAuth = await stfLoginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(stfAuth);
        _tokens[TestPersona.Staff] = stfAuth!.AccessToken;

        // 4. Customer A setup
        _tokens[TestPersona.CustomerA] = await RegisterVerifyAndLoginCustomerAsync("customera.matrix@groceryhub.vn", "Nguyen Customer A", "0983334455");

        // 5. Customer B setup
        _tokens[TestPersona.CustomerB] = await RegisterVerifyAndLoginCustomerAsync("customerb.matrix@groceryhub.vn", "Hoang Customer B", "0984445566");
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<string> RegisterVerifyAndLoginCustomerAsync(string email, string fullName, string phone)
    {
        const string password = "Password@123456";
        RegisterRequest registerReq = new(email, password, fullName, phone);
        using HttpResponseMessage regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        Assert.Equal(HttpStatusCode.Created, regRes.StatusCode);
        RegisterResponse? regData = await regRes.Content.ReadFromJsonAsync<RegisterResponse>();

        TestEmailRecord emailRecord = _factory.EmailService.VerificationEmails.First(e => e.UserId == regData!.UserId);
        VerifyEmailRequest verifyReq = new(regData!.UserId, emailRecord.Token);
        using HttpResponseMessage verRes = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", verifyReq);
        Assert.Equal(HttpStatusCode.OK, verRes.StatusCode);

        LoginRequest loginReq = new(email, password);
        using HttpResponseMessage loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        AuthTokenResponse? auth = await loginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        return auth!.AccessToken;
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        TestPersona persona,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        using HttpRequestMessage request = new(method, url);
        string? token = _tokens[persona];
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (content is not null)
        {
            request.Content = content;
        }

        return await _client.SendAsync(request);
    }

    [Theory]
    [InlineData(TestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(TestPersona.CustomerA, HttpStatusCode.OK)]
    [InlineData(TestPersona.CustomerB, HttpStatusCode.OK)]
    [InlineData(TestPersona.Staff, HttpStatusCode.OK)]
    [InlineData(TestPersona.Manager, HttpStatusCode.OK)]
    [InlineData(TestPersona.Admin, HttpStatusCode.OK)]
    public async Task Auth_GetMe_PolicyMatrix(TestPersona persona, HttpStatusCode expectedStatus)
    {
        using HttpResponseMessage response = await SendRequestAsync(persona, HttpMethod.Get, "/api/v1/auth/me");
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(TestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(TestPersona.CustomerA, HttpStatusCode.NoContent)]
    [InlineData(TestPersona.CustomerB, HttpStatusCode.NoContent)]
    [InlineData(TestPersona.Staff, HttpStatusCode.NoContent)]
    [InlineData(TestPersona.Manager, HttpStatusCode.NoContent)]
    [InlineData(TestPersona.Admin, HttpStatusCode.NoContent)]
    public async Task Auth_Logout_PolicyMatrix(TestPersona persona, HttpStatusCode expectedStatus)
    {
        using HttpResponseMessage response = await SendRequestAsync(persona, HttpMethod.Post, "/api/v1/auth/logout");
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(TestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(TestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Staff, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Manager, HttpStatusCode.OK)]
    [InlineData(TestPersona.Admin, HttpStatusCode.OK)]
    public async Task Admin_GetStaffList_PolicyMatrix(TestPersona persona, HttpStatusCode expectedStatus)
    {
        using HttpResponseMessage response = await SendRequestAsync(persona, HttpMethod.Get, "/api/v1/admin/staff");
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(TestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(TestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Staff, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Manager, HttpStatusCode.OK)]
    [InlineData(TestPersona.Admin, HttpStatusCode.OK)]
    public async Task Admin_GetStaffById_PolicyMatrix(TestPersona persona, HttpStatusCode expectedStatus)
    {
        using HttpResponseMessage response = await SendRequestAsync(persona, HttpMethod.Get, $"/api/v1/admin/staff/{_staffUserId}");
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(TestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(TestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Staff, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Manager, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Admin, HttpStatusCode.Created)]
    public async Task Admin_CreateStaff_PolicyMatrix(TestPersona persona, HttpStatusCode expectedStatus)
    {
        CreateStaffRequest newStaff = new(
            Email: $"test.{Guid.NewGuid():N}@groceryhub.vn",
            Password: "Password@123456",
            FullName: "Test Staff Matrix",
            Phone: "0989998877",
            EmployeeCode: $"EMP-{Guid.NewGuid():N}"[..12],
            Position: "Test",
            Role: "Staff",
            ExtraPermissions: null);

        using HttpResponseMessage response = await SendRequestAsync(
            persona,
            HttpMethod.Post,
            "/api/v1/admin/staff",
            JsonContent.Create(newStaff));

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(TestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(TestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Staff, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Manager, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Admin, HttpStatusCode.OK)]
    public async Task Admin_UpdateStaffProfile_PolicyMatrix(TestPersona persona, HttpStatusCode expectedStatus)
    {
        UpdateStaffRequest updateReq = new(
            FullName: "Updated Name",
            Phone: "0982223344",
            Position: "Senior Associate",
            Status: "Active",
            Note: "Matrix update note");

        using HttpResponseMessage response = await SendRequestAsync(
            persona,
            HttpMethod.Put,
            $"/api/v1/admin/staff/{_staffUserId}",
            JsonContent.Create(updateReq));

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(TestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(TestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Staff, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Manager, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Admin, HttpStatusCode.OK)]
    public async Task Admin_UpdateStaffRoles_PolicyMatrix(TestPersona persona, HttpStatusCode expectedStatus)
    {
        UpdateStaffRolesRequest rolesReq = new(["Staff"]);

        using HttpResponseMessage response = await SendRequestAsync(
            persona,
            HttpMethod.Put,
            $"/api/v1/admin/staff/{_staffUserId}/roles",
            JsonContent.Create(rolesReq));

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(TestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(TestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Staff, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Manager, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Admin, HttpStatusCode.OK)]
    public async Task Admin_UpdateStaffPermissions_PolicyMatrix(TestPersona persona, HttpStatusCode expectedStatus)
    {
        UpdateStaffPermissionsRequest permsReq = new([Permissions.PurchasingReceive]);

        using HttpResponseMessage response = await SendRequestAsync(
            persona,
            HttpMethod.Put,
            $"/api/v1/admin/staff/{_staffUserId}/permissions",
            JsonContent.Create(permsReq));

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(TestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(TestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Staff, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Manager, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Admin, HttpStatusCode.OK)]
    public async Task Admin_GetRolesMatrix_PolicyMatrix(TestPersona persona, HttpStatusCode expectedStatus)
    {
        using HttpResponseMessage response = await SendRequestAsync(persona, HttpMethod.Get, "/api/v1/admin/roles");
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(TestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(TestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Staff, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Manager, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Admin, HttpStatusCode.OK)]
    public async Task Admin_GetPermissionsCatalog_PolicyMatrix(TestPersona persona, HttpStatusCode expectedStatus)
    {
        using HttpResponseMessage response = await SendRequestAsync(persona, HttpMethod.Get, "/api/v1/admin/permissions");
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(TestPersona.Anonymous, HttpStatusCode.Unauthorized)]
    [InlineData(TestPersona.CustomerA, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.CustomerB, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Staff, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Manager, HttpStatusCode.Forbidden)]
    [InlineData(TestPersona.Admin, HttpStatusCode.OK)]
    public async Task Admin_UpdateRolePermissions_PolicyMatrix(TestPersona persona, HttpStatusCode expectedStatus)
    {
        UpdateRolePermissionsRequest updateReq = new([
            Permissions.CatalogRead,
            Permissions.InventoryRead,
            Permissions.PurchasingRead,
            Permissions.OrdersRead,
            Permissions.OrdersProcess,
            Permissions.OrdersShip,
            Permissions.CustomersRead
        ]);

        using HttpResponseMessage response = await SendRequestAsync(
            persona,
            HttpMethod.Put,
            $"/api/v1/admin/roles/{_roleStaffId}/permissions",
            JsonContent.Create(updateReq));

        Assert.Equal(expectedStatus, response.StatusCode);
    }
}
