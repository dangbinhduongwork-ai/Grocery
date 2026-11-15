using GroceryHub.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace GroceryHub.Modules.Identity.Application;

public sealed class DynamicPermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public const string StaffCreateOrManagePolicy = "staff.create.or.manage";

    public DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        AuthorizationPolicy? policy = await base.GetPolicyAsync(policyName);
        if (policy is not null)
        {
            return policy;
        }

        if (string.Equals(policyName, "auth", StringComparison.OrdinalIgnoreCase))
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        }

        if (string.Equals(policyName, StaffCreateOrManagePolicy, StringComparison.OrdinalIgnoreCase))
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new AnyPermissionRequirement(Permissions.StaffCreate, Permissions.StaffManage))
                .Build();
        }

        if (Permissions.All.Contains(policyName, StringComparer.Ordinal))
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();
        }

        return null;
    }
}
