using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GroceryHub.Modules.Identity.Contracts;
using Xunit;

namespace GroceryHub.IntegrationTests.Identity;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class EmailVerificationRequirementIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
    private readonly SqlServerTestDatabaseFixture _dbFixture = dbFixture;
    private GroceryHubWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _dbFixture.ResetDatabaseAsync();
        _factory = new GroceryHubWebApplicationFactory(_dbFixture.ConnectionString);
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task UnverifiedEmail_LoginAttempt_IsRejectedWith403Forbidden_AndEmailNotVerifiedCode()
    {
        const string email = "customer.unverified@groceryhub.vn";
        const string password = "Password@123456";
        const string fullName = "Nguyen Van Unverified";
        const string phone = "0967890123";

        // 1. Register account
        RegisterRequest registerReq = new(email, password, fullName, phone);
        using HttpResponseMessage registerRes = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        Assert.Equal(HttpStatusCode.Created, registerRes.StatusCode);

        RegisterResponse? registerData = await registerRes.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(registerData);
        Assert.True(registerData!.VerificationRequired);

        // 2. Attempt login BEFORE email verification
        LoginRequest loginReq = new(email, password);
        using HttpResponseMessage loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);

        Assert.Equal(HttpStatusCode.Forbidden, loginRes.StatusCode);
        Assert.Equal("application/problem+json", loginRes.Content.Headers.ContentType?.MediaType);

        string body = await loginRes.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(body);
        JsonElement root = doc.RootElement;

        Assert.Equal(403, root.GetProperty("status").GetInt32());
        Assert.Equal("EMAIL_NOT_VERIFIED", root.GetProperty("code").GetString());
        Assert.Equal("https://groceryhub.vn/errors/EMAIL_NOT_VERIFIED", root.GetProperty("type").GetString());
        Assert.Contains("verified", root.GetProperty("detail").GetString(), StringComparison.OrdinalIgnoreCase);
    }
}
