using GroceryHub.Api.Middleware;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GroceryHub.Api.ErrorHandling;

public static class ResultProblemExtensions
{
    public const string ErrorTypeBaseUri = "https://groceryhub.vn/errors/";

    public static ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        DomainError error)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(error);

        int statusCode = MapStatusCode(error.Type);
        string code = string.IsNullOrWhiteSpace(error.Code) ? DefaultCodeForStatus(statusCode) : error.Code;
        string traceId = CorrelationIdMiddleware.GetCorrelationId(httpContext);

        ProblemDetails problem = new()
        {
            Type = $"{ErrorTypeBaseUri}{code}",
            Title = GetTitle(error.Type, code),
            Status = statusCode,
            Detail = error.Message,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = traceId;
        problem.Extensions["errors"] = error.FieldErrors ?? new Dictionary<string, string[]>();

        return problem;
    }

    public static ProblemHttpResult ToProblemResult(
        this DomainError error,
        HttpContext httpContext)
    {
        ProblemDetails problem = CreateProblemDetails(httpContext, error);
        return TypedResults.Problem(problem);
    }

    public static ProblemHttpResult ToProblemResult(
        this Result result,
        HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful Result to a ProblemHttpResult.");
        }

        return result.Error.ToProblemResult(httpContext);
    }

    public static int MapStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.BadRequest => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Concurrency => StatusCodes.Status412PreconditionFailed,
        ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
        ErrorType.RateLimited => StatusCodes.Status429TooManyRequests,
        _ => StatusCodes.Status500InternalServerError
    };

    public static string DefaultCodeForStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => ErrorCodes.BadRequest,
        StatusCodes.Status401Unauthorized => ErrorCodes.Unauthenticated,
        StatusCodes.Status403Forbidden => ErrorCodes.Forbidden,
        StatusCodes.Status404NotFound => ErrorCodes.NotFound,
        StatusCodes.Status409Conflict => ErrorCodes.Conflict,
        StatusCodes.Status412PreconditionFailed => ErrorCodes.ConcurrencyConflict,
        StatusCodes.Status422UnprocessableEntity => ErrorCodes.ValidationFailed,
        StatusCodes.Status429TooManyRequests => ErrorCodes.RateLimited,
        _ => ErrorCodes.InternalError
    };

    public static string GetTitle(ErrorType errorType, string code) => errorType switch
    {
        ErrorType.BadRequest => "Bad request",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => FormatCodeTitle(code, "Conflict"),
        ErrorType.Concurrency => "Concurrency conflict",
        ErrorType.Validation => "Validation failed",
        ErrorType.RateLimited => "Too many requests",
        _ => "Internal server error"
    };

    private static string FormatCodeTitle(string code, string fallback)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return fallback;
        }

        string lower = code.Replace('_', ' ').ToLowerInvariant();
        return char.ToUpperInvariant(lower[0]) + lower[1..];
    }
}

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        string traceId = CorrelationIdMiddleware.GetCorrelationId(httpContext);

        if (exception is BadHttpRequestException badRequest)
        {
            logger.LogWarning(
                badRequest,
                "Bad HTTP request on {Method} {Path} (TraceId: {TraceId})",
                httpContext.Request.Method,
                httpContext.Request.Path,
                traceId);

            DomainError badRequestError = DomainError.BadRequest(
                ErrorCodes.BadRequest,
                "The request payload or parameters could not be parsed.");
            ProblemDetails badProblem = ResultProblemExtensions.CreateProblemDetails(httpContext, badRequestError);
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(badProblem, cancellationToken: cancellationToken);
            return true;
        }

        logger.LogError(
            exception,
            "Unhandled exception on {Method} {Path} (TraceId: {TraceId})",
            httpContext.Request.Method,
            httpContext.Request.Path,
            traceId);

        DomainError unexpected = DomainError.Unexpected(
            "An unexpected error occurred while processing your request.",
            ErrorCodes.InternalError);
        ProblemDetails problem = ResultProblemExtensions.CreateProblemDetails(httpContext, unexpected);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
        return true;
    }
}
