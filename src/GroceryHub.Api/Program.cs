using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GroceryHub.Api.Configuration;
using GroceryHub.Api.Endpoints;
using GroceryHub.Api.ErrorHandling;
using GroceryHub.Api.Logging;
using GroceryHub.Api.Middleware;
using GroceryHub.Api.RateLimiting;
using GroceryHub.Modules.Audit.Api;
using GroceryHub.Modules.Cart.Api;
using GroceryHub.Modules.Catalog.Api;
using GroceryHub.Modules.Customers.Api;
using GroceryHub.Modules.Identity.Api;
using GroceryHub.Modules.Inventory.Api;
using GroceryHub.Modules.Ordering.Api;
using GroceryHub.Modules.Payments.Api;
using GroceryHub.Modules.Promotions.Api;
using GroceryHub.Modules.Purchasing.Api;
using GroceryHub.Modules.Reporting.Api;
using GroceryHub.Modules.Reviews.Api;
using GroceryHub.Persistence;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(SerilogConfiguration.ConfigureLogger);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services
    .AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<SystemOptions>()
    .Bind(builder.Configuration.GetSection(SystemOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<RateLimitingOptions>()
    .Bind(builder.Configuration.GetSection(RateLimitingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        string traceId = CorrelationIdMiddleware.GetCorrelationId(context.HttpContext);
        int statusCode = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
        string defaultCode = ResultProblemExtensions.DefaultCodeForStatus(statusCode);

        context.ProblemDetails.Extensions.TryAdd("code", defaultCode);
        context.ProblemDetails.Extensions["traceId"] = traceId;
        context.ProblemDetails.Extensions.TryAdd("errors", new Dictionary<string, string[]>());
        context.ProblemDetails.Type ??= $"{ResultProblemExtensions.ErrorTypeBaseUri}{context.ProblemDetails.Extensions["code"]}";
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddGroceryHubRateLimiting(builder.Configuration, TimeProvider.System);

builder.Services
    .AddHealthChecks()
    .AddCheck("self", static () => HealthCheckResult.Healthy(), tags: ["live"]);

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "GroceryHub API";
        document.Info.Version = "1.0.0";
        document.Info.Description = "GroceryHub Vietnamese Online Grocery Platform REST API (v1)";
        document.Servers = [new() { Url = "/" }];
        return Task.CompletedTask;
    });
});

builder.Services.AddGroceryHubPersistence(builder.Configuration);

builder.Services
    .AddIdentityModule(builder.Configuration)
    .AddCustomersModule(builder.Configuration)
    .AddCatalogModule(builder.Configuration)
    .AddInventoryModule(builder.Configuration)
    .AddPurchasingModule(builder.Configuration)
    .AddPromotionsModule(builder.Configuration)
    .AddCartModule(builder.Configuration)
    .AddOrderingModule(builder.Configuration)
    .AddPaymentsModule(builder.Configuration)
    .AddReviewsModule(builder.Configuration)
    .AddReportingModule(builder.Configuration)
    .AddAuditModule(builder.Configuration);

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

app.UseStatusCodePages(async statusCodeContext =>
{
    HttpContext httpContext = statusCodeContext.HttpContext;
    int statusCode = httpContext.Response.StatusCode;

    DomainError domainError = statusCode switch
    {
        StatusCodes.Status401Unauthorized => DomainError.Unauthorized(),
        StatusCodes.Status403Forbidden => DomainError.Forbidden(),
        StatusCodes.Status404NotFound => DomainError.NotFound(
            $"No endpoint matched '{httpContext.Request.Method} {httpContext.Request.Path}'.",
            ErrorCodes.NotFound),
        StatusCodes.Status412PreconditionFailed => DomainError.Concurrency(),
        StatusCodes.Status429TooManyRequests => DomainError.RateLimited(),
        _ => new DomainError(
            ResultProblemExtensions.DefaultCodeForStatus(statusCode),
            "The request could not be completed.",
            ErrorType.BadRequest)
    };

    ProblemDetails problem = ResultProblemExtensions.CreateProblemDetails(httpContext, domainError);
    problem.Status = statusCode;
    httpContext.Response.ContentType = "application/problem+json";
    await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: httpContext.RequestAborted);
});

app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi("/openapi/v1.json")
    .AllowAnonymous();

RouteGroupBuilder apiV1Group = app.MapGroup("/api/v1");

app.MapSystemAndHealthEndpoints(apiV1Group);

apiV1Group
    .MapIdentityEndpoints()
    .MapCustomersEndpoints()
    .MapCatalogEndpoints()
    .MapInventoryEndpoints()
    .MapPurchasingEndpoints()
    .MapPromotionsEndpoints()
    .MapCartEndpoints()
    .MapOrderingEndpoints()
    .MapPaymentsEndpoints()
    .MapReviewsEndpoints()
    .MapReportingEndpoints()
    .MapAuditEndpoints();

int exportArgIndex = Array.IndexOf(args, "--export-openapi");
if (exportArgIndex >= 0)
{
    if (exportArgIndex + 1 >= args.Length || string.IsNullOrWhiteSpace(args[exportArgIndex + 1]))
    {
        Console.Error.WriteLine("Missing output path after --export-openapi.");
        Environment.ExitCode = 1;
        return;
    }

    string outputPath = ResolveOutputPath(args[exportArgIndex + 1], app.Environment.ContentRootPath);
    app.Urls.Clear();
    app.Urls.Add("http://127.0.0.1:0");

    await app.StartAsync();
    try
    {
        string address = app.Urls.First();
        using HttpClient httpClient = new() { BaseAddress = new Uri(address) };
        using HttpResponseMessage response = await httpClient.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        string rawJson = await response.Content.ReadAsStringAsync();
        using JsonDocument jsonDoc = JsonDocument.Parse(rawJson);
        string formattedJson = JsonSerializer.Serialize(
            jsonDoc.RootElement,
            new JsonSerializerOptions { WriteIndented = true }).Replace("\r\n", "\n", StringComparison.Ordinal);

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(outputPath, formattedJson + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"Exported OpenAPI document to {outputPath}");
    }
    finally
    {
        await app.StopAsync();
    }

    return;
}

await app.RunAsync();

static string ResolveOutputPath(string rawPath, string contentRootPath)
{
    if (Path.IsPathRooted(rawPath))
    {
        return Path.GetFullPath(rawPath);
    }

    DirectoryInfo? dir = new(contentRootPath);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "GroceryHub.sln")))
        {
            return Path.GetFullPath(Path.Combine(dir.FullName, rawPath));
        }

        dir = dir.Parent;
    }

    return Path.GetFullPath(rawPath, Directory.GetCurrentDirectory());
}

public partial class Program
{
}
