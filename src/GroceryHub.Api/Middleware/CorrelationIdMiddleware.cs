using System.Diagnostics;
using Serilog.Context;

namespace GroceryHub.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string correlationId = ResolveCorrelationId(context);
        context.TraceIdentifier = correlationId;
        context.Items[ItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(ItemKey, correlationId))
        {
            await next(context);
        }
    }

    public static string GetCorrelationId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Items.TryGetValue(ItemKey, out object? existing) && existing is string id && !string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        return string.IsNullOrWhiteSpace(context.TraceIdentifier)
            ? Activity.Current?.Id ?? Guid.NewGuid().ToString("D")
            : context.TraceIdentifier;
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            string? candidate = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= 64 && IsSafeHeaderValue(candidate))
            {
                return candidate.Trim();
            }
        }

        return Guid.NewGuid().ToString("D");
    }

    private static bool IsSafeHeaderValue(string value)
    {
        foreach (char c in value)
        {
            if (char.IsControl(c))
            {
                return false;
            }
        }

        return true;
    }
}
