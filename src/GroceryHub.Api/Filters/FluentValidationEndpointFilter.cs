using FluentValidation;
using FluentValidation.Results;
using GroceryHub.Api.ErrorHandling;
using GroceryHub.SharedKernel;

namespace GroceryHub.Api.Filters;

public sealed class FluentValidationEndpointFilter<TRequest> : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IEnumerable<IValidator<TRequest>> validators = context.HttpContext.RequestServices
            .GetServices<IValidator<TRequest>>();

        IValidator<TRequest>[] validatorArray = validators as IValidator<TRequest>[] ?? [.. validators];
        if (validatorArray.Length == 0)
        {
            return await next(context);
        }

        TRequest? request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            DomainError missingBodyError = DomainError.BadRequest(
                ErrorCodes.BadRequest,
                "Request body is required.");
            return missingBodyError.ToProblemResult(context.HttpContext);
        }

        ValidationContext<TRequest> validationContext = new(request);
        List<ValidationFailure> failures = [];

        foreach (IValidator<TRequest> validator in validatorArray)
        {
            ValidationResult result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                failures.AddRange(result.Errors);
            }
        }

        if (failures.Count > 0)
        {
            Dictionary<string, string[]> errorMap = failures
                .GroupBy(static f => ToCamelCasePropertyName(f.PropertyName), StringComparer.Ordinal)
                .ToDictionary(
                    static g => g.Key,
                    static g => g.Select(static f => f.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                    StringComparer.Ordinal);

            DomainError validationError = DomainError.ValidationFailed(errorMap);
            return validationError.ToProblemResult(context.HttpContext);
        }

        return await next(context);
    }

    private static string ToCamelCasePropertyName(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return "request";
        }

        string[] segments = propertyName.Split('.');
        for (int i = 0; i < segments.Length; i++)
        {
            string s = segments[i];
            if (s.Length > 0 && char.IsUpper(s[0]))
            {
                segments[i] = char.ToLowerInvariant(s[0]) + s[1..];
            }
        }

        return string.Join('.', segments);
    }
}

public static class ValidationRouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder WithRequestValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder
            .AddEndpointFilter<FluentValidationEndpointFilter<TRequest>>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
