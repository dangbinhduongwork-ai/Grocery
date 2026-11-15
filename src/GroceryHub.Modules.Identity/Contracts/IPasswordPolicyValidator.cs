using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Identity.Contracts;

public interface IPasswordPolicyValidator
{
    Result Validate(string? password);
}
