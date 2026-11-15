using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Identity.Application;

public sealed class PasswordPolicyValidator : IPasswordPolicyValidator
{
    public const int MinimumLength = 10;

    public Result Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return Result.Failure(DomainError.ValidationFailed(
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Password"] = ["Password is required."]
                },
                "Password is required."));
        }

        List<string> errors = [];

        if (password.Length < MinimumLength)
        {
            errors.Add($"Password must be at least {MinimumLength} characters long.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit.");
        }

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            errors.Add("Password must contain at least one non-alphanumeric character.");
        }

        if (errors.Count > 0)
        {
            return Result.Failure(DomainError.ValidationFailed(
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Password"] = [.. errors]
                },
                "Password does not meet complexity requirements."));
        }

        return Result.Success();
    }
}
