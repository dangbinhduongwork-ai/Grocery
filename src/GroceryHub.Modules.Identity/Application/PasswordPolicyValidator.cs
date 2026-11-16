using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Identity.Application;

public sealed class PasswordPolicyValidator : IPasswordPolicyValidator
{
    public const int MinimumLength = 8;

    public Result Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return Result.Failure(DomainError.ValidationFailed(
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Password"] = ["Mật khẩu không được để trống."]
                },
                "Mật khẩu không được để trống."));
        }

        List<string> errors = [];

        if (password.Length < MinimumLength)
        {
            errors.Add($"Mật khẩu phải có độ dài từ {MinimumLength} ký tự trở lên.");
        }

        if (errors.Count > 0)
        {
            return Result.Failure(DomainError.ValidationFailed(
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Password"] = [.. errors]
                },
                "Mật khẩu không đủ độ dài yêu cầu (từ 8 ký tự trở lên)."));
        }

        return Result.Success();
    }
}
