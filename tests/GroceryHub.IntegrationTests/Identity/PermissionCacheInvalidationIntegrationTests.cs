using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GroceryHub.Modules.Identity.Contracts;
using Xunit;

namespace GroceryHub.IntegrationTests.Identity;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class PermissionCacheInvalidationIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
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
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task UpdatingStaffPermissionsAndRoles_InvalidatesCacheImmediately_WithoutWaitingForTtl()
    {
        // 1. Log in as bootstrap Admin
        LoginRequest adminLoginReq = new("admin@groceryhub.vn", "Admin@GroceryHub2026!");
        using HttpResponseMessage adminLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", adminLoginReq);
        Assert.Equal(HttpStatusCode.OK, adminLoginRes.StatusCode);

        AuthTokenResponse? adminTokens = await adminLoginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(adminTokens);
        string adminAccessToken = adminTokens!.AccessToken;

        // 2. Admin creates a Staff member
        const string staffEmail = "staff.permcache@groceryhub.vn";
        const string staffPassword = "Password@123456";
        CreateStaffRequest createStaffReq = new(
            Email: staffEmail,
            Password: staffPassword,
            FullName: "Nguyen Van Staff Cache",
            Phone: "0971234567",
            EmployeeCode: "EMP-CACHE-01",
            Position: "Warehouse Operator",
            Role: "Staff",
            ExtraPermissions: null);

        using HttpRequestMessage createStaffHttpReq = new(HttpMethod.Post, "/api/v1/admin/staff");
        createStaffHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAccessToken);
        createStaffHttpReq.Content = JsonContent.Create(createStaffReq);

        using HttpResponseMessage createStaffRes = await _client.SendAsync(createStaffHttpReq);
        Assert.Equal(HttpStatusCode.Created, createStaffRes.StatusCode);

        StaffAccountDto? staffAccount = await createStaffRes.Content.ReadFromJsonAsync<StaffAccountDto>();
        Assert.NotNull(staffAccount);
        Guid staffUserId = staffAccount!.UserId;

        // 3. Staff logs in -> receives staff access token
        LoginRequest staffLoginReq = new(staffEmail, staffPassword);
        using HttpResponseMessage staffLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", staffLoginReq);
        Assert.Equal(HttpStatusCode.OK, staffLoginRes.StatusCode);

        AuthTokenResponse? staffTokens = await staffLoginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(staffTokens);
        string staffAccessToken = staffTokens!.AccessToken;

        // 4. Staff checks GET /api/v1/auth/me -> 200 OK (effective permissions are cached)
        using HttpRequestMessage staffMeReq1 = new(HttpMethod.Get, "/api/v1/auth/me");
        staffMeReq1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffAccessToken);
        using HttpResponseMessage staffMeRes1 = await _client.SendAsync(staffMeReq1);
        Assert.Equal(HttpStatusCode.OK, staffMeRes1.StatusCode);
        AuthUserDto? meUser1 = await staffMeRes1.Content.ReadFromJsonAsync<AuthUserDto>();
        Assert.NotNull(meUser1);
        Assert.DoesNotContain(Permissions.StaffCreate, meUser1!.Permissions);

        // 5. Staff attempts POST /api/v1/admin/staff (requires staff.create.or.manage) -> 403 Forbidden
        CreateStaffRequest dummyCreateReq = new(
            Email: "dummy@groceryhub.vn",
            Password: "Password@123456",
            FullName: "Dummy",
            Phone: "0971112233",
            EmployeeCode: "EMP-DUMMY-99",
            Position: "Dummy",
            Role: "Staff",
            ExtraPermissions: null);

        using HttpRequestMessage staffCreateForbiddenReq = new(HttpMethod.Post, "/api/v1/admin/staff");
        staffCreateForbiddenReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffAccessToken);
        staffCreateForbiddenReq.Content = JsonContent.Create(dummyCreateReq);

        using HttpResponseMessage staffCreateForbiddenRes = await _client.SendAsync(staffCreateForbiddenReq);
        Assert.Equal(HttpStatusCode.Forbidden, staffCreateForbiddenRes.StatusCode);

        // 6. Admin grants extra permission "staff.create" via PUT /api/v1/admin/staff/{id}/permissions
        UpdateStaffPermissionsRequest grantPermsReq = new([Permissions.StaffCreate]);
        using HttpRequestMessage grantPermsHttpReq = new(HttpMethod.Put, $"/api/v1/admin/staff/{staffUserId}/permissions");
        grantPermsHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAccessToken);
        grantPermsHttpReq.Content = JsonContent.Create(grantPermsReq);

        using HttpResponseMessage grantPermsRes = await _client.SendAsync(grantPermsHttpReq);
        Assert.Equal(HttpStatusCode.OK, grantPermsRes.StatusCode);

        // 7. Staff IMMEDIATELY repeats POST /api/v1/admin/staff -> must succeed with 201 Created!
        // Instantaneous cache invalidation proved!
        using HttpRequestMessage staffCreateAllowedReq = new(HttpMethod.Post, "/api/v1/admin/staff");
        staffCreateAllowedReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffAccessToken);
        staffCreateAllowedReq.Content = JsonContent.Create(dummyCreateReq);

        using HttpResponseMessage staffCreateAllowedRes = await _client.SendAsync(staffCreateAllowedReq);
        Assert.Equal(HttpStatusCode.Created, staffCreateAllowedRes.StatusCode);

        // 8. Admin revokes extra permission "staff.create"
        UpdateStaffPermissionsRequest revokePermsReq = new([]);
        using HttpRequestMessage revokePermsHttpReq = new(HttpMethod.Put, $"/api/v1/admin/staff/{staffUserId}/permissions");
        revokePermsHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAccessToken);
        revokePermsHttpReq.Content = JsonContent.Create(revokePermsReq);

        using HttpResponseMessage revokePermsRes = await _client.SendAsync(revokePermsHttpReq);
        Assert.Equal(HttpStatusCode.OK, revokePermsRes.StatusCode);

        // 9. Staff IMMEDIATELY repeats POST /api/v1/admin/staff -> immediately 403 Forbidden again
        CreateStaffRequest dummyCreateReq2 = new(
            Email: "dummy2@groceryhub.vn",
            Password: "Password@123456",
            FullName: "Dummy 2",
            Phone: "0972223344",
            EmployeeCode: "EMP-DUMMY-98",
            Position: "Dummy",
            Role: "Staff",
            ExtraPermissions: null);

        using HttpRequestMessage staffCreateRevokedReq = new(HttpMethod.Post, "/api/v1/admin/staff");
        staffCreateRevokedReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffAccessToken);
        staffCreateRevokedReq.Content = JsonContent.Create(dummyCreateReq2);

        using HttpResponseMessage staffCreateRevokedRes = await _client.SendAsync(staffCreateRevokedReq);
        Assert.Equal(HttpStatusCode.Forbidden, staffCreateRevokedRes.StatusCode);

        // 10. Staff attempts GET /api/v1/admin/staff (requires staff.read; Staff role lacks staff.read) -> 403 Forbidden
        using HttpRequestMessage staffReadReq1 = new(HttpMethod.Get, "/api/v1/admin/staff");
        staffReadReq1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffAccessToken);
        using HttpResponseMessage staffReadRes1 = await _client.SendAsync(staffReadReq1);
        Assert.Equal(HttpStatusCode.Forbidden, staffReadRes1.StatusCode);

        // 11. Admin promotes staff to "Manager" role via PUT /api/v1/admin/staff/{id}/roles
        UpdateStaffRolesRequest promoteToManagerReq = new(["Manager"]);
        using HttpRequestMessage promoteHttpReq = new(HttpMethod.Put, $"/api/v1/admin/staff/{staffUserId}/roles");
        promoteHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAccessToken);
        promoteHttpReq.Content = JsonContent.Create(promoteToManagerReq);

        using HttpResponseMessage promoteRes = await _client.SendAsync(promoteHttpReq);
        Assert.Equal(HttpStatusCode.OK, promoteRes.StatusCode);

        // 12. Staff IMMEDIATELY calls GET /api/v1/admin/staff -> now returns 200 OK because Manager has staff.read!
        using HttpRequestMessage staffReadReq2 = new(HttpMethod.Get, "/api/v1/admin/staff");
        staffReadReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffAccessToken);
        using HttpResponseMessage staffReadRes2 = await _client.SendAsync(staffReadReq2);
        Assert.Equal(HttpStatusCode.OK, staffReadRes2.StatusCode);
    }
}
