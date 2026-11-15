using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GroceryHub.Modules.Payments.Configuration;

public sealed class SePayOptions
{
    public const string SectionName = "SePay";

    public string Environment { get; set; } = "Sandbox";

    public string AccountNumber { get; set; } = "0000000000";

    public string? SubAccount { get; set; }

    public string BankShortName { get; set; } = "MBBank";

    public string AccountName { get; set; } = "CONG TY TNHH GROCERYHUB";

    public string PaymentCodePrefix { get; set; } = "GH";

    public string? TransferContentPrefix { get; set; }

    public string QrBaseUrl { get; set; } = "https://qr.sepay.vn/img";

    public SePayWebhookOptions Webhook { get; set; } = new();

    public SePayApiOptions Api { get; set; } = new();

    public int QrExpiryMinutes { get; set; } = 30;

    public int ExpiryGraceMinutes { get; set; } = 5;

    public int MaxAttemptsPerOrder { get; set; } = 5;

    public SePayReconciliationOptions Reconciliation { get; set; } = new();
}

public sealed class SePayWebhookOptions
{
    public string AuthMode { get; set; } = "Hmac";

    public string? HmacSecret { get; set; }

    public string? HmacSecretPrevious { get; set; }

    public string? ApiKey { get; set; }

    public int TimestampToleranceSeconds { get; set; } = 300;

    public int MaxBodyBytes { get; set; } = 65536;

    public string[] AllowedIps { get; set; } =
    [
        "172.236.138.20",
        "172.233.83.68",
        "171.244.35.2",
        "151.158.108.68",
        "151.158.109.79",
        "103.255.238.139",
        "45.57.137.67",
        "2400:8905::2000:8cff:fe98:45cd",
        "2600:3c15::2000:8aff:fedd:874b"
    ];
}

public sealed class SePayApiOptions
{
    public string BaseUrl { get; set; } = "https://userapi-sandbox.sepay.vn/v2";

    public string? Token { get; set; }
}

public sealed class SePayReconciliationOptions
{
    public int IntervalMinutes { get; set; } = 15;

    public int LookbackHours { get; set; } = 24;
}

public sealed class SePayOptionsValidator : IValidateOptions<SePayOptions>
{
    private readonly IHostEnvironment _environment;

    public SePayOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, SePayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.BankShortName))
        {
            failures.Add("SePay:BankShortName is required.");
        }

        if (options.QrExpiryMinutes <= 0)
        {
            failures.Add("SePay:QrExpiryMinutes must be positive.");
        }

        if (options.ExpiryGraceMinutes < 0)
        {
            failures.Add("SePay:ExpiryGraceMinutes cannot be negative.");
        }

        if (_environment.IsProduction())
        {
            if (!string.Equals(options.Environment, "Production", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("SePay:Environment must be 'Production' when running in Production environment.");
            }

            if (string.Equals(options.Webhook.AuthMode, "None", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("SePay:Webhook:AuthMode 'None' is strictly forbidden in Production.");
            }

            if (string.IsNullOrWhiteSpace(options.AccountNumber) || options.AccountNumber == "0000000000")
            {
                failures.Add("SePay:AccountNumber must be configured with a valid account in Production.");
            }

            if (string.Equals(options.Webhook.AuthMode, "Hmac", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(options.Webhook.HmacSecret) ||
                    options.Webhook.HmacSecret.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add("SePay:Webhook:HmacSecret must be configured with a real secret in Production.");
                }
            }
            else if (string.Equals(options.Webhook.AuthMode, "ApiKey", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(options.Webhook.ApiKey) ||
                    options.Webhook.ApiKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add("SePay:Webhook:ApiKey must be configured with a real key in Production.");
                }
            }

            if (string.IsNullOrWhiteSpace(options.Api.Token) ||
                options.Api.Token.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("SePay:Api:Token must be configured with a real API token in Production.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
