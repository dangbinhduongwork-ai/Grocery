using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace GroceryHub.Modules.Promotions.Api;

internal static class PromotionsResults
{
    public const string ErrorTypeBaseUri = "https://groceryhub.vn/errors/";

    public static IResult Problem(HttpContext context, DomainError error)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(error);

        int statusCode = error.Type switch
        {
            ErrorType.BadRequest => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Concurrency => StatusCodes.Status412PreconditionFailed,
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorType.RateLimited => StatusCodes.Status429TooManyRequests,
            _ => (int)error.Type
        };

        string code = string.IsNullOrWhiteSpace(error.Code) ? "ERROR" : error.Code;
        string traceId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? context.TraceIdentifier;

        Dictionary<string, object?> extensions = new()
        {
            ["code"] = code,
            ["traceId"] = traceId,
            ["errors"] = error.FieldErrors ?? new Dictionary<string, string[]>()
        };

        return TypedResults.Problem(
            detail: error.Message,
            instance: context.Request.Path,
            statusCode: statusCode,
            title: GetTitle(error.Type, code),
            type: $"{ErrorTypeBaseUri}{code}",
            extensions: extensions);
    }

    private static string GetTitle(ErrorType errorType, string code) => errorType switch
    {
        ErrorType.BadRequest => "Bad request",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.NotFound => "Not found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Concurrency => "Precondition failed",
        ErrorType.Validation => "Unprocessable entity",
        ErrorType.RateLimited => "Too many requests",
        _ => "An error occurred"
    };
}
