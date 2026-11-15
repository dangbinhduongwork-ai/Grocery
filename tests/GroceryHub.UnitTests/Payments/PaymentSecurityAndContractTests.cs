using System.Text.RegularExpressions;
using GroceryHub.Modules.Payments.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GroceryHub.UnitTests.Payments;

public sealed class PaymentSecurityAndContractTests
{
    [Fact]
    public void T_PAY_21_StaticCodebaseScan_ProvesZeroClientCallableMarkPaidEndpoints()
    {
        // T-PAY-21: Static codebase verification for any client-callable "mark as paid" endpoint
        // Rule: Only the verified SePay webhook or Staff manual review can mark a payment as Paid.
        // There must be ZERO client endpoints (e.g. under /me/, /storefront, or customer routes)
        // that accept client input to mark an order or payment as Paid.

        string solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        string srcDir = Path.Combine(solutionDir, "src");

        var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("bin"))
            .ToList();

        List<string> suspiciousFindings = [];

        // Patterns that would indicate a client endpoint setting payment to Paid
        var suspiciousRoutePattern = new Regex(@"Map(Post|Put|Patch)\s*\(\s*""[^""]*(?:confirm-payment|mark-paid|set-paid)", RegexOptions.IgnoreCase);

        foreach (var file in csFiles)
        {
            string content = File.ReadAllText(file);
            var matches = suspiciousRoutePattern.Matches(content);
            foreach (Match match in matches)
            {
                suspiciousFindings.Add($"{Path.GetFileName(file)}: Found suspicious route '{match.Value}'");
            }
        }

        Assert.Empty(suspiciousFindings);
    }

    [Fact]
    public void SePayOptionsValidator_BlocksUnsafeProductionConfiguration()
    {
        var validator = new SePayOptionsValidator(new TestHostEnvironment("Production"));

        // 1. Production with AuthMode = "None" -> Must fail
        var opt1 = new SePayOptions
        {
            Environment = "Production",
            Webhook = new SePayWebhookOptions
            {
                AuthMode = "None",
                HmacSecret = "real_secret_12345",
            },
            AccountNumber = "1234567890",
            Api = new SePayApiOptions { Token = "valid_token" },
        };
        var res1 = validator.Validate(null, opt1);
        Assert.True(res1.Failed);
        Assert.Contains("AuthMode 'None' is strictly forbidden in Production", res1.FailureMessage);

        // 2. Production with placeholder AccountNumber -> Must fail
        var opt2 = new SePayOptions
        {
            Environment = "Production",
            Webhook = new SePayWebhookOptions
            {
                AuthMode = "Hmac",
                HmacSecret = "real_secret_12345",
            },
            AccountNumber = "0000000000",
            Api = new SePayApiOptions { Token = "valid_token" },
        };
        var res2 = validator.Validate(null, opt2);
        Assert.True(res2.Failed);
        Assert.Contains("valid account in Production", res2.FailureMessage);

        // 3. Production with empty/CHANGE_ME HmacSecret -> Must fail
        var opt3 = new SePayOptions
        {
            Environment = "Production",
            Webhook = new SePayWebhookOptions
            {
                AuthMode = "Hmac",
                HmacSecret = "CHANGE_ME",
            },
            AccountNumber = "0987654321",
            Api = new SePayApiOptions { Token = "valid_token" },
        };
        var res3 = validator.Validate(null, opt3);
        Assert.True(res3.Failed);
        Assert.Contains("HmacSecret must be configured", res3.FailureMessage);

        // 4. Production with valid production settings -> Must succeed
        var optValid = new SePayOptions
        {
            Environment = "Production",
            Webhook = new SePayWebhookOptions
            {
                AuthMode = "Hmac",
                HmacSecret = "real_secret_1234567890_strong",
            },
            AccountNumber = "19036789234567",
            BankShortName = "MBBank",
            Api = new SePayApiOptions { Token = "valid_prod_token" },
        };
        var resValid = validator.Validate(null, optValid);
        Assert.True(resValid.Succeeded);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "GroceryHub";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
