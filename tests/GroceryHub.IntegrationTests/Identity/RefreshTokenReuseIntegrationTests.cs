using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GroceryHub.Modules.Identity.Contracts;
using Xunit;

namespace GroceryHub.IntegrationTests.Identity;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class RefreshTokenReuseIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
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
    public async Task ReplayingOldRefreshToken_TriggersReuseDetection_RevokesEntireFamily_AndInvalidatesActiveToken()
    {
        const string email = "customer.tokenreuse@groceryhub.vn";
        const string password = "Password@123456";
        const string fullName = "Nguyen Van Reuse";
        const string phone = "0934567890";

        // 1. Register & verify email
        RegisterRequest registerReq = new(email, password, fullName, phone);
        using HttpResponseMessage registerRes = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        Assert.Equal(HttpStatusCode.Created, registerRes.StatusCode);

        RegisterResponse? registerData = await registerRes.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(registerData);

        TestEmailRecord? emailRecord = _factory.EmailService.VerificationEmails
            .FirstOrDefault(e => e.UserId == registerData!.UserId);
        Assert.NotNull(emailRecord);

        VerifyEmailRequest verifyReq = new(registerData!.UserId, emailRecord!.Token);
        using HttpResponseMessage verifyRes = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", verifyReq);
        Assert.Equal(HttpStatusCode.OK, verifyRes.StatusCode);

        // 2. Login -> obtain refresh token T1
        LoginRequest loginReq = new(email, password);
        using HttpResponseMessage loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);

        string? t1CookieHeader = loginRes.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(c => c.StartsWith("gh_refresh=", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(t1CookieHeader);
        string tokenT1 = ExtractCookieValue(t1CookieHeader!, "gh_refresh");
        Assert.False(string.IsNullOrWhiteSpace(tokenT1));

        // 3. Refresh with T1 -> obtain replacement refresh token T2
        using HttpRequestMessage refreshReq1 = new(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq1.Headers.Add("X-Requested-With", "XMLHttpRequest");
        refreshReq1.Headers.Add("Cookie", $"gh_refresh={tokenT1}");

        using HttpResponseMessage refreshRes1 = await _client.SendAsync(refreshReq1);
        Assert.Equal(HttpStatusCode.OK, refreshRes1.StatusCode);

        string? t2CookieHeader = refreshRes1.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(c => c.StartsWith("gh_refresh=", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(t2CookieHeader);
        string tokenT2 = ExtractCookieValue(t2CookieHeader!, "gh_refresh");
        Assert.False(string.IsNullOrWhiteSpace(tokenT2));
        Assert.NotEqual(tokenT1, tokenT2);

        // 4. Token Theft Simulation: Replay token T1
        using HttpRequestMessage replayReq = new(HttpMethod.Post, "/api/v1/auth/refresh");
        replayReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
        replayReq.Headers.Add("Cookie", $"gh_refresh={tokenT1}");

        using HttpResponseMessage replayRes = await _client.SendAsync(replayReq);
        Assert.Equal(HttpStatusCode.Unauthorized, replayRes.StatusCode);
        Assert.Equal("application/problem+json", replayRes.Content.Headers.ContentType?.MediaType);

        string replayBody = await replayRes.Content.ReadAsStringAsync();
        using JsonDocument replayDoc = JsonDocument.Parse(replayBody);
        Assert.Equal("REFRESH_TOKEN_INVALID", replayDoc.RootElement.GetProperty("code").GetString());

        // 5. Verify that token T2 (which belonged to the same FamilyId) was also revoked and now fails
        using HttpRequestMessage t2Req = new(HttpMethod.Post, "/api/v1/auth/refresh");
        t2Req.Headers.Add("X-Requested-With", "XMLHttpRequest");
        t2Req.Headers.Add("Cookie", $"gh_refresh={tokenT2}");

        using HttpResponseMessage t2Res = await _client.SendAsync(t2Req);
        Assert.Equal(HttpStatusCode.Unauthorized, t2Res.StatusCode);

        string t2Body = await t2Res.Content.ReadAsStringAsync();
        using JsonDocument t2Doc = JsonDocument.Parse(t2Body);
        Assert.Equal("REFRESH_TOKEN_INVALID", t2Doc.RootElement.GetProperty("code").GetString());
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
