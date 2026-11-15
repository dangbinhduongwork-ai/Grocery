using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace GroceryHub.Api.Logging;

public static class SerilogConfiguration
{
    private static readonly HashSet<string> SensitiveHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-SePay-Signature"
    };

    public static void ConfigureLogger(HostBuilderContext context, IServiceProvider services, LoggerConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", context.Configuration["GroceryHub:ServiceName"] ?? "GroceryHub.Api")
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

        if (context.HostingEnvironment.IsProduction())
        {
            configuration.WriteTo.Console(new CompactJsonFormatter());
        }
        else
        {
            configuration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}");
        }
    }

    public static bool IsSensitiveHeader(string headerName) =>
        SensitiveHeaderNames.Contains(headerName);

    public static IReadOnlyDictionary<string, string> RedactHeaders(IHeaderDictionary headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        Dictionary<string, string> sanitized = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> pair in headers)
        {
            sanitized[pair.Key] = IsSensitiveHeader(pair.Key)
                ? "[REDACTED]"
                : pair.Value.ToString();
        }

        return sanitized;
    }
}
