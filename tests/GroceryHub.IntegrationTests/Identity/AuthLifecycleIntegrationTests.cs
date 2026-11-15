using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GroceryHub.Modules.Identity.Contracts;
using Xunit;

namespace GroceryHub.IntegrationTests.Identity;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class AuthLifecycleIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
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
    public async Task CompleteAuthLifecycle_Register_Verify_Login_Refresh_GetMe_Logout_Succeeds()
    {
        const string email = "customer.lifecycle@groceryhub.vn";
        const string password = "Password@123456";
        const string fullName = "Nguyen Van Lifecycle";
        const string phone = "0912345678";

        // 1. Register Customer -> 201 Created
        RegisterRequest registerReq = new(email, password, fullName, phone);
        using HttpResponseMessage registerRes = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        Assert.Equal(HttpStatusCode.Created, registerRes.StatusCode);

        RegisterResponse? registerData = await registerRes.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(registerData);
        Assert.Equal(email, registerData!.Email);
        Assert.True(registerData.VerificationRequired);

        // Retrieve verification token recorded by TestEmailService
        TestEmailRecord? emailRecord = _factory.EmailService.VerificationEmails
            .FirstOrDefault(e => e.UserId == registerData.UserId);
        Assert.NotNull(emailRecord);
        string verificationToken = emailRecord!.Token;

        // 2. Verify Email -> 200 OK
        VerifyEmailRequest verifyReq = new(registerData.UserId, verificationToken);
        using HttpResponseMessage verifyRes = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", verifyReq);
        Assert.Equal(HttpStatusCode.OK, verifyRes.StatusCode);

        VerifyEmailResponse? verifyData = await verifyRes.Content.ReadFromJsonAsync<VerifyEmailResponse>();
        Assert.NotNull(verifyData);
        Assert.True(verifyData!.Verified);

        // 3. Login -> 200 OK + JWT + gh_refresh cookie
        LoginRequest loginReq = new(email, password);
        using HttpResponseMessage loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);

        AuthTokenResponse? loginTokens = await loginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(loginTokens);
        Assert.False(string.IsNullOrWhiteSpace(loginTokens!.AccessToken));
        Assert.Equal(900, loginTokens.ExpiresIn);
        Assert.Equal(fullName, loginTokens.User.FullName);
        Assert.Contains("Customer", loginTokens.User.Roles);

        // Verify gh_refresh cookie attributes
        Assert.True(loginRes.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieHeaders));
        string? refreshCookieHeader = cookieHeaders!.FirstOrDefault(c => c.StartsWith("gh_refresh=", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(refreshCookieHeader);

        AssertCookieAttributes(refreshCookieHeader!);
        string refreshToken1 = ExtractCookieValue(refreshCookieHeader!, "gh_refresh");
        Assert.False(string.IsNullOrWhiteSpace(refreshToken1));

        // 4. Refresh token with gh_refresh + X-Requested-With: XMLHttpRequest -> rotated token + new cookie
        using HttpRequestMessage refreshReq = new(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
        refreshReq.Headers.Add("Cookie", $"gh_refresh={refreshToken1}");

        using HttpResponseMessage refreshRes = await _client.SendAsync(refreshReq);
        Assert.Equal(HttpStatusCode.OK, refreshRes.StatusCode);

        AuthTokenResponse? rotatedTokens = await refreshRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(rotatedTokens);
        Assert.False(string.IsNullOrWhiteSpace(rotatedTokens!.AccessToken));
        Assert.NotEqual(loginTokens.AccessToken, rotatedTokens.AccessToken);

        Assert.True(refreshRes.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? rotatedCookieHeaders));
        string? rotatedCookieHeader = rotatedCookieHeaders!.FirstOrDefault(c => c.StartsWith("gh_refresh=", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(rotatedCookieHeader);

        AssertCookieAttributes(rotatedCookieHeader!);
        string refreshToken2 = ExtractCookieValue(rotatedCookieHeader!, "gh_refresh");
        Assert.False(string.IsNullOrWhiteSpace(refreshToken2));
        Assert.NotEqual(refreshToken1, refreshToken2);

        // 5. Call GET /api/v1/auth/me with Bearer token -> 200 OK
        using HttpRequestMessage meReq = new(HttpMethod.Get, "/api/v1/auth/me");
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rotatedTokens.AccessToken);

        using HttpResponseMessage meRes = await _client.SendAsync(meReq);
        Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);

        AuthUserDto? meUser = await meRes.Content.ReadFromJsonAsync<AuthUserDto>();
        Assert.NotNull(meUser);
        Assert.Equal(registerData.UserId, meUser!.Id);
        Assert.Equal(email, meUser.Email);
        Assert.Contains("Customer", meUser.Roles);

        // 6. Logout -> 204 NoContent, revokes family, clears cookie
        using HttpRequestMessage logoutReq = new(HttpMethod.Post, "/api/v1/auth/logout");
        logoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rotatedTokens.AccessToken);
        logoutReq.Headers.Add("Cookie", $"gh_refresh={refreshToken2}");

        using HttpResponseMessage logoutRes = await _client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.NoContent, logoutRes.StatusCode);

        Assert.True(logoutRes.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? logoutCookieHeaders));
        string? clearedCookieHeader = logoutCookieHeaders!.FirstOrDefault(c => c.StartsWith("gh_refresh=", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(clearedCookieHeader);
        Assert.Contains("expires=thu, 01 jan 1970", clearedCookieHeader!.ToLowerInvariant());

        // 7. Subsequent refresh with revoked token fails with 401 Unauthorized
        using HttpRequestMessage postLogoutRefreshReq = new(HttpMethod.Post, "/api/v1/auth/refresh");
        postLogoutRefreshReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
        postLogoutRefreshReq.Headers.Add("Cookie", $"gh_refresh={refreshToken2}");

        using HttpResponseMessage postLogoutRefreshRes = await _client.SendAsync(postLogoutRefreshReq);
        Assert.Equal(HttpStatusCode.Unauthorized, postLogoutRefreshRes.StatusCode);
    }

    [Fact]
    public async Task Refresh_Without_XRequestedWithHeader_Returns400BadRequest()
    {
        // Missing X-Requested-With header
        using HttpRequestMessage refreshReq = new(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq.Headers.Add("Cookie", "gh_refresh=some_random_token");

        using HttpResponseMessage refreshRes = await _client.SendAsync(refreshReq);
        Assert.Equal(HttpStatusCode.BadRequest, refreshRes.StatusCode);
    }

    [Fact]
    public async Task Refresh_Without_RefreshCookie_Returns401Unauthorized()
    {
        using HttpRequestMessage refreshReq = new(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq.Headers.Add("X-Requested-With", "XMLHttpRequest");

        using HttpResponseMessage refreshRes = await _client.SendAsync(refreshReq);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshRes.StatusCode);
    }

    private static void AssertCookieAttributes(string cookieHeader)
    {
        string lower = cookieHeader.ToLowerInvariant();
        Assert.Contains("httponly", lower);
        Assert.Contains("secure", lower);
        Assert.Contains("samesite=strict", lower);
        Assert.Contains("path=/api/v1/auth", lower);
    }

    private static string ExtractCookieValue(string cookieHeader, string cookieName)
    {
        string[] parts = cookieHeader.Split(';');
        string prefix = $"{cookieName}=";
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed[prefix.Length..];
            }
        }

        return string.Empty;
    }
}
