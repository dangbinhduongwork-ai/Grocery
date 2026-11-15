using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using GroceryHub.Modules.Payments.Application;
using GroceryHub.Modules.Payments.Configuration;
using GroceryHub.Modules.Payments.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GroceryHub.Modules.Payments.Infrastructure;

public sealed class ReconciliationService : BackgroundService
{
    private static readonly Regex CodeRegex = new(@"\bGH\d{10}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ReconciliationService> _logger;
    private readonly TimeProvider _timeProvider;

    public ReconciliationService(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<ReconciliationService> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReconciliationService started.");

        using var scope = _serviceProvider.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<SePayOptions>>().Value;
        int intervalMinutes = options.Reconciliation.IntervalMinutes > 0 ? options.Reconciliation.IntervalMinutes : 15;

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes), _timeProvider);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ReconcileAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred during SePay reconciliation cycle.");
            }
        }
    }

    public async Task<(int LocalRetried, int RemoteChecked, int Recovered)> ReconcileAsync(CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GroceryHubDbContext>();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<SePayOptions>>().Value;

        DateTimeOffset now = _timeProvider.GetUtcNow();
        int localRetried = 0;
        int remoteChecked = 0;
        int recovered = 0;

        // 1. Retry failed local webhook events older than 10 minutes (up to 5 delivery attempts)
        DateTimeOffset tenMinutesAgo = now.AddMinutes(-10);
        var failedEvents = await db.WebhookEvents
            .Where(we => we.ProcessingStatus == WebhookProcessingStatusConstants.Failed &&
                         we.DeliveryCount < 5 &&
                         we.ReceivedAt <= tenMinutesAgo)
            .Take(20)
            .ToListAsync(ct);

        foreach (var ev in failedEvents)
        {
            try
            {
                var result = await paymentService.ProcessWebhookAsync(
                    ev.RawBody,
                    now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                    null,
                    options.Webhook.ApiKey != null ? $"Apikey {options.Webhook.ApiKey}" : null,
                    "127.0.0.1",
                    ct);

                if (result.Success)
                {
                    localRetried++;
                    _logger.LogInformation("Reconciliation recovered local failed webhook event {EventId}", ev.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Retry of failed webhook event {EventId} failed again", ev.Id);
            }
        }

        // 2. Poll remote SePay API if credentials exist
        string? token = options.Api.Token;
        if (string.IsNullOrWhiteSpace(token) || token.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("SePay:Api:Token is not configured or placeholder; skipping remote API polling.");
            return (localRetried, remoteChecked, recovered);
        }

        try
        {
            string baseUrl = string.IsNullOrWhiteSpace(options.Api.BaseUrl)
                ? "https://userapi-sandbox.sepay.vn/v2"
                : options.Api.BaseUrl.TrimEnd('/');

            int lookbackHours = options.Reconciliation.LookbackHours > 0 ? options.Reconciliation.LookbackHours : 24;
            DateTimeOffset fromTime = now.AddHours(-lookbackHours);
            string dateFrom = fromTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            using var httpClient = _httpClientFactory.CreateClient("SePayApi");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());

            string url = $"{baseUrl}/transactions?transaction_date_from={Uri.EscapeDataString(dateFrom)}&transfer_type=in&per_page=100";
            var response = await httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SePay API returned status {StatusCode} during reconciliation.", response.StatusCode);
                return (localRetried, remoteChecked, recovered);
            }

            string jsonContent = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(jsonContent);

            JsonElement dataElement;
            if (doc.RootElement.TryGetProperty("data", out var dEl) && dEl.ValueKind == JsonValueKind.Array)
            {
                dataElement = dEl;
            }
            else if (doc.RootElement.TryGetProperty("transactions", out var tEl) && tEl.ValueKind == JsonValueKind.Array)
            {
                dataElement = tEl;
            }
            else
            {
                return (localRetried, remoteChecked, recovered);
            }

            foreach (var item in dataElement.EnumerateArray())
            {
                remoteChecked++;

                string txId = item.TryGetProperty("id", out var idProp) ? idProp.ToString() : string.Empty;
                string accountNumber = item.TryGetProperty("account_number", out var accProp) ? accProp.GetString() ?? string.Empty : string.Empty;
                string? refNumber = item.TryGetProperty("reference_number", out var refProp) ? refProp.GetString() : null;
                string content = item.TryGetProperty("transaction_content", out var cProp) ? cProp.GetString() ?? string.Empty : string.Empty;
                string? code = item.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : null;
                string gateway = item.TryGetProperty("bank_brand_name", out var gProp) ? gProp.GetString() ?? options.BankShortName : options.BankShortName;
                string dateStr = item.TryGetProperty("transaction_date", out var dProp) ? dProp.GetString() ?? now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                long amount = 0;
                if (item.TryGetProperty("amount_in", out var amtInProp))
                {
                    if (amtInProp.ValueKind == JsonValueKind.Number) amount = amtInProp.GetInt64();
                    else if (long.TryParse(amtInProp.GetString(), out var aVal)) amount = aVal;
                }

                if (amount <= 0) continue;

                // Deduplication check
                bool alreadyInWebhook = await db.WebhookEvents
                    .AnyAsync(we => we.Provider == "SEPAY" && we.ProviderTransactionId == txId, ct);

                if (alreadyInWebhook) continue;

                if (!string.IsNullOrWhiteSpace(refNumber))
                {
                    bool alreadyInTransactions = await db.PaymentTransactions
                        .AnyAsync(pt => pt.ReferenceCode == refNumber && pt.Amount == amount && pt.AccountNumber == accountNumber, ct);

                    if (alreadyInTransactions) continue;
                }

                string? paymentCode = !string.IsNullOrWhiteSpace(code) ? code.Trim().ToUpperInvariant() : null;
                if (string.IsNullOrWhiteSpace(paymentCode))
                {
                    var match = CodeRegex.Match(content);
                    if (match.Success) paymentCode = match.Value.ToUpperInvariant();
                }

                if (!string.IsNullOrWhiteSpace(paymentCode))
                {
                    bool isPaid = await db.PaymentAttempts
                        .AnyAsync(pa => pa.PaymentCode == paymentCode && pa.Status == PaymentAttemptStatusConstants.Paid && pa.Amount == amount, ct);

                    if (isPaid) continue;
                }

                // Genuinely missing transaction recovered from SePay API
                long numericId = Math.Abs(txId.GetHashCode());
                var syntheticPayload = new SePayWebhookPayloadDto(
                    numericId,
                    gateway,
                    dateStr,
                    accountNumber,
                    null,
                    paymentCode,
                    content,
                    "in",
                    content,
                    amount,
                    0L,
                    refNumber
                );

                string syntheticJson = JsonSerializer.Serialize(syntheticPayload);

                var processResult = await paymentService.ProcessWebhookAsync(
                    syntheticJson,
                    now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                    null,
                    options.Webhook.ApiKey != null ? $"Apikey {options.Webhook.ApiKey}" : null,
                    "127.0.0.1",
                    ct);

                if (processResult.Success)
                {
                    recovered++;
                    _logger.LogInformation("Reconciliation recovered missing transaction {TxId} for code {Code}, amount {Amount}",
                        txId, paymentCode, amount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed while polling remote SePay transactions API.");
        }

        return (localRetried, remoteChecked, recovered);
    }
}
