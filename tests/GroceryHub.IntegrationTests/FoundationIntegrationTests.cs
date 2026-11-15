using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GroceryHub.Api.Endpoints;
using GroceryHub.Api.Middleware;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.Persistence.Outbox;
using GroceryHub.SharedKernel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GroceryHub.IntegrationTests;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class FoundationIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
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
    public async Task GetHealthLive_Returns200Healthy()
    {
        using HttpResponseMessage response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        HealthStatusResponse? payload = await response.Content.ReadFromJsonAsync<HealthStatusResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Healthy", payload!.Status);
    }

    [Fact]
    public async Task GetHealthReady_WhenDatabaseIsReachable_Returns200Healthy()
    {
        using HttpResponseMessage response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        HealthStatusResponse? payload = await response.Content.ReadFromJsonAsync<HealthStatusResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Healthy", payload!.Status);

        // Verify all 12 module schemas and 4 database sequences exist in the migrated SQL Server database.
        await using SqlConnection connection = new(_dbFixture.ConnectionString);
        await connection.OpenAsync();

        List<string> existingSchemas = [];
        await using (SqlCommand schemaCommand = connection.CreateCommand())
        {
            schemaCommand.CommandText = "SELECT name FROM sys.schemas;";
            await using SqlDataReader reader = await schemaCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                existingSchemas.Add(reader.GetString(0));
            }
        }

        foreach (string expectedSchema in DatabaseSchemas.All)
        {
            Assert.Contains(expectedSchema, existingSchemas, StringComparer.OrdinalIgnoreCase);
        }

        var existingSequences = new Dictionary<string, (string Schema, long StartValue)>(StringComparer.OrdinalIgnoreCase);
        await using (SqlCommand seqCommand = connection.CreateCommand())
        {
            seqCommand.CommandText = """
                SELECT s.name AS SequenceName, sc.name AS SchemaName, CAST(s.start_value AS bigint) AS StartValue
                FROM sys.sequences s
                INNER JOIN sys.schemas sc ON s.schema_id = sc.schema_id;
                """;
            await using SqlDataReader reader = await seqCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                existingSequences[reader.GetString(0)] = (reader.GetString(1), reader.GetInt64(2));
            }
        }

        Assert.Equal(4, existingSequences.Count);
        Assert.Equal(("ordering", 1L), existingSequences[DatabaseSequences.OrderNumberSeq]);
        Assert.Equal(("payments", DatabaseSequences.PaymentCodeStartValue), existingSequences[DatabaseSequences.PaymentCodeSeq]);
        Assert.Equal(("purchasing", 1L), existingSequences[DatabaseSequences.PurchaseOrderSeq]);
        Assert.Equal(("purchasing", 1L), existingSequences[DatabaseSequences.GoodsReceiptSeq]);
    }

    [Fact]
    public async Task GetHealthReady_WhenDatabaseIsUnreachable_Returns503ServiceUnavailable()
    {
        SqlConnectionStringBuilder unreachableBuilder = new()
        {
            DataSource = "127.0.0.1,65431",
            InitialCatalog = "UnreachableDb",
            UserID = "sa",
            Password = "Wrong",
            ConnectTimeout = 1,
            TrustServerCertificate = true,
        };

        await using GroceryHubWebApplicationFactory unreachableFactory = new(unreachableBuilder.ConnectionString);
        using HttpClient unreachableClient = unreachableFactory.CreateClient();

        using HttpResponseMessage response = await unreachableClient.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        HealthStatusResponse? payload = await response.Content.ReadFromJsonAsync<HealthStatusResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Unhealthy", payload!.Status);
    }

    [Fact]
    public async Task GetSystemInfo_Returns200WithVersionEnvironmentAndEchoesCorrelationId()
    {
        const string correlationId = "corr-phase04-smoke-001";
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/system/info");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);

        using HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out IEnumerable<string>? headerValues));
        Assert.Equal(correlationId, headerValues!.Single());

        SystemInfoResponse? payload = await response.Content.ReadFromJsonAsync<SystemInfoResponse>();
        Assert.NotNull(payload);
        Assert.Equal("GroceryHub.Api", payload!.Service);
        Assert.Equal("1.0.0", payload.Version);
        Assert.Equal("Testing", payload.Environment);
        Assert.NotEqual(default, payload.TimestampUtc);
    }

    [Fact]
    public async Task OutboxDispatcher_ProcessesPublishedEventAndEnforcesConsumerReceiptIdempotency()
    {
        _factory.OrderPlacedHandler.Reset();

        Guid eventId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        TestOrderPlacedIntegrationEvent integrationEvent = new(
            EventId: eventId,
            OccurredAt: new DateTimeOffset(2026, 11, 15, 5, 0, 0, TimeSpan.Zero),
            OrderId: orderId,
            GrandTotalVnd: 185_000L);

        await using (AsyncServiceScope publishScope = _factory.Services.CreateAsyncScope())
        {
            IOutboxPublisher publisher = publishScope.ServiceProvider.GetRequiredService<IOutboxPublisher>();
            GroceryHubDbContext dbContext = publishScope.ServiceProvider.GetRequiredService<GroceryHubDbContext>();

            publisher.Enqueue(integrationEvent, correlationId: "corr-outbox-01");
            await dbContext.SaveChangesAsync();
        }

        await using (AsyncServiceScope firstDispatchScope = _factory.Services.CreateAsyncScope())
        {
            OutboxDispatcherProcessor processor = firstDispatchScope.ServiceProvider.GetRequiredService<OutboxDispatcherProcessor>();
            int processedCount = await processor.ProcessPendingBatchAsync();
            Assert.Equal(1, processedCount);
        }

        await using (AsyncServiceScope verifyScope = _factory.Services.CreateAsyncScope())
        {
            GroceryHubDbContext dbContext = verifyScope.ServiceProvider.GetRequiredService<GroceryHubDbContext>();

            OutboxMessage? message = await dbContext.OutboxMessages.SingleOrDefaultAsync(m => m.Id == eventId);
            Assert.NotNull(message);
            Assert.NotNull(message!.ProcessedAt);
            Assert.Equal(1, message.Attempts);
            Assert.Null(message.LastError);

            List<OutboxConsumerReceipt> receipts = await dbContext.OutboxConsumerReceipts
                .Where(r => r.MessageId == eventId)
                .ToListAsync();

            Assert.Single(receipts);
            Assert.Equal(typeof(TestOrderPlacedIntegrationEventHandler).FullName, receipts[0].HandlerName);
            Assert.Equal(1, _factory.OrderPlacedHandler.InvocationCount);
            Assert.Equal([orderId], _factory.OrderPlacedHandler.HandledOrderIds);

            // Simulate redelivery / crash before marking ProcessedAt by resetting ProcessedAt to null.
            message.ProcessedAt = null;
            await dbContext.SaveChangesAsync();
        }

        await using (AsyncServiceScope secondDispatchScope = _factory.Services.CreateAsyncScope())
        {
            OutboxDispatcherProcessor processor = secondDispatchScope.ServiceProvider.GetRequiredService<OutboxDispatcherProcessor>();
            int reprocessedCount = await processor.ProcessPendingBatchAsync();
            Assert.Equal(1, reprocessedCount);
        }

        await using (AsyncServiceScope finalVerifyScope = _factory.Services.CreateAsyncScope())
        {
            GroceryHubDbContext dbContext = finalVerifyScope.ServiceProvider.GetRequiredService<GroceryHubDbContext>();

            OutboxMessage? message = await dbContext.OutboxMessages.SingleOrDefaultAsync(m => m.Id == eventId);
            Assert.NotNull(message);
            Assert.NotNull(message!.ProcessedAt);

            int receiptCount = await dbContext.OutboxConsumerReceipts.CountAsync(r => r.MessageId == eventId);
            Assert.Equal(1, receiptCount);

            // Handler invocation count must still be 1 because OutboxConsumerReceipts prevented duplicate execution!
            Assert.Equal(1, _factory.OrderPlacedHandler.InvocationCount);
        }
    }

    [Fact]
    public async Task ProblemDetails_For404_422_And_500_ConformToRfc7807Contract()
    {
        // 1. 404 NOT_FOUND on unknown route
        using (HttpRequestMessage notFoundReq = new(HttpMethod.Get, "/api/v1/does-not-exist"))
        {
            notFoundReq.Headers.Add(CorrelationIdMiddleware.HeaderName, "corr-404-check");
            using HttpResponseMessage notFoundRes = await _client.SendAsync(notFoundReq);

            Assert.Equal(HttpStatusCode.NotFound, notFoundRes.StatusCode);
            Assert.Equal("application/problem+json", notFoundRes.Content.Headers.ContentType?.MediaType);

            using JsonDocument doc = JsonDocument.Parse(await notFoundRes.Content.ReadAsStringAsync());
            JsonElement root = doc.RootElement;

            Assert.Equal("https://groceryhub.vn/errors/NOT_FOUND", root.GetProperty("type").GetString());
            Assert.Equal("Resource not found", root.GetProperty("title").GetString());
            Assert.Equal(404, root.GetProperty("status").GetInt32());
            Assert.Contains("/api/v1/does-not-exist", root.GetProperty("detail").GetString(), StringComparison.Ordinal);
            Assert.Equal("NOT_FOUND", root.GetProperty("code").GetString());
            Assert.Equal("corr-404-check", root.GetProperty("traceId").GetString());
            Assert.Equal(JsonValueKind.Object, root.GetProperty("errors").ValueKind);
        }

        // 2. 422 VALIDATION_FAILED on invalid request payload
        using (HttpRequestMessage validationReq = new(HttpMethod.Post, "/__test/validation"))
        {
            validationReq.Headers.Add(CorrelationIdMiddleware.HeaderName, "corr-422-check");
            validationReq.Content = JsonContent.Create(new TestValidationRequest(Name: "", Quantity: 0));
            using HttpResponseMessage validationRes = await _client.SendAsync(validationReq);

            Assert.Equal(HttpStatusCode.UnprocessableEntity, validationRes.StatusCode);
            Assert.Equal("application/problem+json", validationRes.Content.Headers.ContentType?.MediaType);

            using JsonDocument doc = JsonDocument.Parse(await validationRes.Content.ReadAsStringAsync());
            JsonElement root = doc.RootElement;

            Assert.Equal("https://groceryhub.vn/errors/VALIDATION_FAILED", root.GetProperty("type").GetString());
            Assert.Equal("Validation failed", root.GetProperty("title").GetString());
            Assert.Equal(422, root.GetProperty("status").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("detail").GetString()));
            Assert.Equal("VALIDATION_FAILED", root.GetProperty("code").GetString());
            Assert.Equal("corr-422-check", root.GetProperty("traceId").GetString());

            JsonElement errors = root.GetProperty("errors");
            Assert.True(errors.TryGetProperty("name", out JsonElement nameErrors));
            Assert.NotEmpty(nameErrors.EnumerateArray());
            Assert.True(errors.TryGetProperty("quantity", out JsonElement quantityErrors));
            Assert.NotEmpty(quantityErrors.EnumerateArray());
        }

        // 3. 500 INTERNAL_ERROR on unhandled exception with zero stack trace or sensitive exception message leaked
        using (HttpRequestMessage exceptionReq = new(HttpMethod.Get, "/__test/exception"))
        {
            exceptionReq.Headers.Add(CorrelationIdMiddleware.HeaderName, "corr-500-check");
            using HttpResponseMessage exceptionRes = await _client.SendAsync(exceptionReq);

            Assert.Equal(HttpStatusCode.InternalServerError, exceptionRes.StatusCode);
            Assert.Equal("application/problem+json", exceptionRes.Content.Headers.ContentType?.MediaType);

            string rawBody = await exceptionRes.Content.ReadAsStringAsync();
            Assert.DoesNotContain("Simulated unhandled database secret exception", rawBody, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("InvalidOperationException", rawBody, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("at GroceryHub", rawBody, StringComparison.OrdinalIgnoreCase);

            using JsonDocument doc = JsonDocument.Parse(rawBody);
            JsonElement root = doc.RootElement;

            Assert.Equal("https://groceryhub.vn/errors/INTERNAL_ERROR", root.GetProperty("type").GetString());
            Assert.Equal("Internal server error", root.GetProperty("title").GetString());
            Assert.Equal(500, root.GetProperty("status").GetInt32());
            Assert.Equal("An unexpected error occurred while processing your request.", root.GetProperty("detail").GetString());
            Assert.Equal("INTERNAL_ERROR", root.GetProperty("code").GetString());
            Assert.Equal("corr-500-check", root.GetProperty("traceId").GetString());
            Assert.Equal(JsonValueKind.Object, root.GetProperty("errors").ValueKind);
        }
    }
}
