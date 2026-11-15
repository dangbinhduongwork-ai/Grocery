using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GroceryHub.Modules.Identity.Contracts;
using Xunit;

namespace GroceryHub.IntegrationTests.Identity;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class LockoutIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
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
    public async Task FiveFailedLogins_LocksAccount_AndSubsequentAttemptWithCorrectPassword_IsRejectedWith423()
    {
        const string email = "customer.lockout@groceryhub.vn";
        const string correctPassword = "Password@123456";
        const string wrongPassword = "WrongPassword@999";
        const string fullName = "Nguyen Van Lockout";
        const string phone = "0923456789";

        // 1. Register & verify email
        RegisterRequest registerReq = new(email, correctPassword, fullName, phone);
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

        // 2. Perform 5 failed login attempts with wrong password
        for (int i = 1; i <= 5; i++)
        {
            LoginRequest failedReq = new(email, wrongPassword);
            using HttpResponseMessage failedRes = await _client.PostAsJsonAsync("/api/v1/auth/login", failedReq);

            // Attempts 1-5 fail password verification, triggering AccessFailedCount increment
            Assert.Equal(HttpStatusCode.Unauthorized, failedRes.StatusCode);
        }

        // 3. 6th attempt with CORRECT password must be rejected with 423 Locked / ACCOUNT_LOCKED
        LoginRequest correctReq = new(email, correctPassword);
        using HttpResponseMessage lockedRes = await _client.PostAsJsonAsync("/api/v1/auth/login", correctReq);

        Assert.Equal((HttpStatusCode)423, lockedRes.StatusCode);
        Assert.Equal("application/problem+json", lockedRes.Content.Headers.ContentType?.MediaType);

        string responseContent = await lockedRes.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(responseContent);
        JsonElement root = doc.RootElement;

        Assert.Equal(423, root.GetProperty("status").GetInt32());
        Assert.Equal("ACCOUNT_LOCKED", root.GetProperty("code").GetString());
        Assert.Equal("https://groceryhub.vn/errors/ACCOUNT_LOCKED", root.GetProperty("type").GetString());
        Assert.Contains("locked", root.GetProperty("detail").GetString(), StringComparison.OrdinalIgnoreCase);

        // 4. Attempt with wrong password during lockout window is also rejected with 423
        LoginRequest anotherWrongReq = new(email, wrongPassword);
        using HttpResponseMessage lockedAgainRes = await _client.PostAsJsonAsync("/api/v1/auth/login", anotherWrongReq);

        Assert.Equal((HttpStatusCode)423, lockedAgainRes.StatusCode);
    }
}
