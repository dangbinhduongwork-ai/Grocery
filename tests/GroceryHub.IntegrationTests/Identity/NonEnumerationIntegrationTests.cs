using System.Net;
using System.Net.Http.Json;
using GroceryHub.Modules.Identity.Contracts;
using Xunit;

namespace GroceryHub.IntegrationTests.Identity;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class NonEnumerationIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
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
    public async Task ForgotPassword_ForExistingAndNonExistingEmail_BothReturn202AcceptedWithIdenticalPayload()
    {
        const string existingEmail = "existing.user@groceryhub.vn";
        const string nonExistingEmail = "does.not.exist.98765@groceryhub.vn";

        // 1. Register and verify an existing user
        RegisterRequest registerReq = new(existingEmail, "Password@123456", "Existing User", "0945678901");
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

        _factory.EmailService.Reset();

        // 2. Forgot password for existing email -> 202 Accepted { accepted: true }
        ForgotPasswordRequest existingReq = new(existingEmail);
        using HttpResponseMessage existingRes = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", existingReq);
        Assert.Equal(HttpStatusCode.Accepted, existingRes.StatusCode);

        AcceptedResponse? existingPayload = await existingRes.Content.ReadFromJsonAsync<AcceptedResponse>();
        Assert.NotNull(existingPayload);
        Assert.True(existingPayload!.Accepted);

        // Verify that password reset email was dispatched for existing user
        Assert.Contains(_factory.EmailService.ResetPasswordEmails, e => e.Email == existingEmail);

        // 3. Forgot password for non-existing email -> 202 Accepted { accepted: true }
        ForgotPasswordRequest nonExistingReq = new(nonExistingEmail);
        using HttpResponseMessage nonExistingRes = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", nonExistingReq);
        Assert.Equal(HttpStatusCode.Accepted, nonExistingRes.StatusCode);

        AcceptedResponse? nonExistingPayload = await nonExistingRes.Content.ReadFromJsonAsync<AcceptedResponse>();
        Assert.NotNull(nonExistingPayload);
        Assert.True(nonExistingPayload!.Accepted);

        // Verify NO password reset email was dispatched for non-existing email
        Assert.DoesNotContain(_factory.EmailService.ResetPasswordEmails, e => e.Email == nonExistingEmail);
    }

    [Fact]
    public async Task ResendVerification_ForExistingAndNonExistingEmail_BothReturn202AcceptedWithIdenticalPayload()
    {
        const string unverifiedEmail = "unverified.user@groceryhub.vn";
        const string nonExistingEmail = "does.not.exist.54321@groceryhub.vn";

        // 1. Register an account (leave it unverified)
        RegisterRequest registerReq = new(unverifiedEmail, "Password@123456", "Unverified User", "0956789012");
        using HttpResponseMessage registerRes = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        Assert.Equal(HttpStatusCode.Created, registerRes.StatusCode);

        _factory.EmailService.Reset();

        // 2. Resend verification for existing unverified email -> 202 Accepted { accepted: true }
        ResendVerificationRequest existingReq = new(unverifiedEmail);
        using HttpResponseMessage existingRes = await _client.PostAsJsonAsync("/api/v1/auth/resend-verification", existingReq);
        Assert.Equal(HttpStatusCode.Accepted, existingRes.StatusCode);

        AcceptedResponse? existingPayload = await existingRes.Content.ReadFromJsonAsync<AcceptedResponse>();
        Assert.NotNull(existingPayload);
        Assert.True(existingPayload!.Accepted);

        // Verification email dispatched
        Assert.Contains(_factory.EmailService.VerificationEmails, e => e.Email == unverifiedEmail);

        // 3. Resend verification for non-existing email -> 202 Accepted { accepted: true }
        ResendVerificationRequest nonExistingReq = new(nonExistingEmail);
        using HttpResponseMessage nonExistingRes = await _client.PostAsJsonAsync("/api/v1/auth/resend-verification", nonExistingReq);
        Assert.Equal(HttpStatusCode.Accepted, nonExistingRes.StatusCode);

        AcceptedResponse? nonExistingPayload = await nonExistingRes.Content.ReadFromJsonAsync<AcceptedResponse>();
        Assert.NotNull(nonExistingPayload);
        Assert.True(nonExistingPayload!.Accepted);

        // NO verification email dispatched for non-existing email
        Assert.DoesNotContain(_factory.EmailService.VerificationEmails, e => e.Email == nonExistingEmail);
    }
}
