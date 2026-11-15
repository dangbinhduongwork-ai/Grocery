using System.Security.Cryptography;
using System.Text;
using GroceryHub.Modules.Payments.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GroceryHub.Modules.Payments.Application;

public interface ISePaySignatureValidator
{
    bool ValidateWebhook(
        string rawBody,
        string? timestampHeader,
        string? signatureHeader,
        string? authHeader,
        DateTimeOffset now);
}

public sealed class SePaySignatureValidator : ISePaySignatureValidator
{
    private readonly SePayOptions _options;
    private readonly IHostEnvironment _environment;

    public SePaySignatureValidator(
        IOptions<SePayOptions> options,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        _options = options.Value;
        _environment = environment;
    }

    public bool ValidateWebhook(
        string rawBody,
        string? timestampHeader,
        string? signatureHeader,
        string? authHeader,
        DateTimeOffset now)
    {
        string mode = _options.Webhook.AuthMode ?? "Hmac";

        if (string.Equals(mode, "None", StringComparison.OrdinalIgnoreCase))
        {
            return !_environment.IsProduction();
        }

        if (string.Equals(mode, "ApiKey", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateApiKey(authHeader);
        }

        // Default: Hmac mode
        return ValidateHmac(rawBody, timestampHeader, signatureHeader, now);
    }

    private bool ValidateHmac(
        string rawBody,
        string? timestampHeader,
        string? signatureHeader,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(timestampHeader) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        if (!long.TryParse(timestampHeader, out long timestamp))
        {
            return false;
        }

        long nowSeconds = now.ToUnixTimeSeconds();
        int tolerance = _options.Webhook.TimestampToleranceSeconds > 0
            ? _options.Webhook.TimestampToleranceSeconds
            : 300;

        if (Math.Abs(nowSeconds - timestamp) > tolerance)
        {
            return false;
        }

        const string prefix = "sha256=";
        if (!signatureHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string providedHex = signatureHeader[prefix.Length..].Trim().ToLowerInvariant();
        if (providedHex.Length != 64)
        {
            return false;
        }

        byte[] signedPayloadBytes = Encoding.UTF8.GetBytes($"{timestamp}.{rawBody}");

        // 1. Try primary secret
        if (!string.IsNullOrWhiteSpace(_options.Webhook.HmacSecret))
        {
            if (CheckHmacSecret(_options.Webhook.HmacSecret, signedPayloadBytes, providedHex))
            {
                return true;
            }
        }

        // 2. Try previous secret during key rotation overlap
        if (!string.IsNullOrWhiteSpace(_options.Webhook.HmacSecretPrevious))
        {
            if (CheckHmacSecret(_options.Webhook.HmacSecretPrevious, signedPayloadBytes, providedHex))
            {
                return true;
            }
        }

        return false;
    }

    private static bool CheckHmacSecret(string secret, byte[] signedPayloadBytes, string providedHex)
    {
        byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
        byte[] computedHash = HMACSHA256.HashData(secretBytes, signedPayloadBytes);
        string computedHex = Convert.ToHexStringLower(computedHash);

        byte[] computedBytes = Encoding.UTF8.GetBytes(computedHex);
        byte[] providedBytes = Encoding.UTF8.GetBytes(providedHex);

        return CryptographicOperations.FixedTimeEquals(computedBytes, providedBytes);
    }

    private bool ValidateApiKey(string? authHeader)
    {
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            return false;
        }

        string expectedApiKey = _options.Webhook.ApiKey ?? string.Empty;
        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            return false;
        }

        const string apikeyPrefix = "Apikey ";
        if (!authHeader.StartsWith(apikeyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string providedApiKey = authHeader[apikeyPrefix.Length..].Trim();

        byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);
        byte[] providedBytes = Encoding.UTF8.GetBytes(providedApiKey);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
