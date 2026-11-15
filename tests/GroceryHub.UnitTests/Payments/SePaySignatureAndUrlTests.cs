using System.Security.Cryptography;
using System.Text;
using GroceryHub.Modules.Payments.Application;
using GroceryHub.Modules.Payments.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace GroceryHub.UnitTests.Payments;

public sealed class SePaySignatureAndUrlTests
{
    private const string TestSecret = "sepay_test_secret_key_1234567890_abcdef";
    private const string TestApiKey = "sepay_api_key_test_9876543210";

    [Fact]
    public void VietQrUrlBuilder_BuildsExpectedUrlWithUrlEncoding()
    {
        // Arrange
        var builder = new VietQrUrlBuilder(Options.Create(new SePayOptions()));

        // Act
        string url = builder.BuildQrUrl(
            accountNumber: "0123456789",
            bankShortName: "MBBank",
            amount: 250000,
            transferContent: "GH0000000001 DON HANG");

        // Assert
        Assert.StartsWith("https://qr.sepay.vn/img?", url);
        Assert.Contains("acc=0123456789", url);
        Assert.Contains("bank=MBBank", url);
        Assert.Contains("amount=250000", url);
        Assert.Contains("des=GH0000000001%20DON%20HANG", url);
        Assert.Contains("template=compact", url);
        Assert.Contains("download=false", url);
    }

    [Fact]
    public void T_PAY_04_TamperedRawJsonBody_SignatureValidationFails()
    {
        // Arrange
        var options = Options.Create(new SePayOptions
        {
            Webhook = new SePayWebhookOptions
            {
                AuthMode = "Hmac",
                HmacSecret = TestSecret,
            }
        });

        var validator = new SePaySignatureValidator(options, new TestHostEnvironment("Development"));

        string originalBody = """{"id":12345,"gateway":"MBBank","transferAmount":250000,"content":"GH0000000001"}""";
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string signature = ComputeHmac(timestamp, originalBody, TestSecret);

        // Tamper the body (amount changed from 250000 to 250001)
        string tamperedBody = """{"id":12345,"gateway":"MBBank","transferAmount":250001,"content":"GH0000000001"}""";

        // Act
        bool isValid = validator.ValidateWebhook(
            rawBody: tamperedBody,
            timestampHeader: timestamp.ToString(),
            signatureHeader: $"sha256={signature}",
            authHeader: null,
            now: DateTimeOffset.UtcNow);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void T_PAY_05_WrongSecretOrMissingHeaders_Fails()
    {
        // Arrange
        var options = Options.Create(new SePayOptions
        {
            Webhook = new SePayWebhookOptions
            {
                AuthMode = "Hmac",
                HmacSecret = TestSecret,
            }
        });

        var validator = new SePaySignatureValidator(options, new TestHostEnvironment("Development"));

        string body = """{"id":12345,"transferAmount":250000}""";
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string signatureWithWrongSecret = ComputeHmac(timestamp, body, "completely_wrong_secret");

        // Act & Assert - Wrong Secret
        bool wrongSecretResult = validator.ValidateWebhook(
            body, timestamp.ToString(), $"sha256={signatureWithWrongSecret}", null, DateTimeOffset.UtcNow);
        Assert.False(wrongSecretResult);

        // Act & Assert - Missing Signature
        bool missingSigResult = validator.ValidateWebhook(
            body, timestamp.ToString(), null, null, DateTimeOffset.UtcNow);
        Assert.False(missingSigResult);

        // Act & Assert - Missing Timestamp
        string validSig = ComputeHmac(timestamp, body, TestSecret);
        bool missingTsResult = validator.ValidateWebhook(
            body, null, $"sha256={validSig}", null, DateTimeOffset.UtcNow);
        Assert.False(missingTsResult);
    }

    [Fact]
    public void T_PAY_06_TimestampExpiredBeyondTolerance_Fails()
    {
        // Arrange
        var options = Options.Create(new SePayOptions
        {
            Webhook = new SePayWebhookOptions
            {
                AuthMode = "Hmac",
                HmacSecret = TestSecret,
                TimestampToleranceSeconds = 300,
            }
        });

        var validator = new SePaySignatureValidator(options, new TestHostEnvironment("Development"));

        string body = """{"id":12345,"transferAmount":250000}""";
        // Timestamp 600s (10 minutes) in the past
        long expiredTimestamp = DateTimeOffset.UtcNow.AddSeconds(-600).ToUnixTimeSeconds();
        string signature = ComputeHmac(expiredTimestamp, body, TestSecret);

        // Act
        bool isValid = validator.ValidateWebhook(
            body, expiredTimestamp.ToString(), $"sha256={signature}", null, DateTimeOffset.UtcNow);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void T_PAY_07_ApiKeyMode_ValidVsInvalidKey()
    {
        // Arrange
        var options = Options.Create(new SePayOptions
        {
            Webhook = new SePayWebhookOptions
            {
                AuthMode = "ApiKey",
                ApiKey = TestApiKey,
            }
        });

        var validator = new SePaySignatureValidator(options, new TestHostEnvironment("Development"));
        string body = """{"id":12345,"transferAmount":250000}""";

        // Act - Valid Key
        bool validResult = validator.ValidateWebhook(
            rawBody: body,
            timestampHeader: null,
            signatureHeader: null,
            authHeader: $"Apikey {TestApiKey}",
            now: DateTimeOffset.UtcNow);

        // Assert - Valid
        Assert.True(validResult);

        // Act - Invalid Key
        bool invalidResult = validator.ValidateWebhook(
            rawBody: body,
            timestampHeader: null,
            signatureHeader: null,
            authHeader: "Apikey wrong_api_key_12345",
            now: DateTimeOffset.UtcNow);

        // Assert - Invalid
        Assert.False(invalidResult);
    }

    [Fact]
    public void WebhookSecretPrevious_AcceptedDuringRotation()
    {
        // Arrange: primary secret is new, previous secret is old
        const string oldSecret = "old_previous_secret_12345";
        const string newSecret = "new_primary_secret_67890";

        var options = Options.Create(new SePayOptions
        {
            Webhook = new SePayWebhookOptions
            {
                AuthMode = "Hmac",
                HmacSecret = newSecret,
                HmacSecretPrevious = oldSecret,
            }
        });

        var validator = new SePaySignatureValidator(options, new TestHostEnvironment("Development"));

        string body = """{"id":12345,"transferAmount":250000}""";
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string oldSignature = ComputeHmac(timestamp, body, oldSecret);

        // Act
        bool isValid = validator.ValidateWebhook(
            body, timestamp.ToString(), $"sha256={oldSignature}", null, DateTimeOffset.UtcNow);

        // Assert: Old secret is accepted gracefully during rotation
        Assert.True(isValid);
    }

    private static string ComputeHmac(long timestamp, string rawBody, string secret)
    {
        string message = $"{timestamp}.{rawBody}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "GroceryHub";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
