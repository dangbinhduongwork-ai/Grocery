using System.Security.Claims;
using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryHub.Modules.Identity.Application;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        Permission = permission;
    }
}

public sealed class AnyPermissionRequirement : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; }

    public AnyPermissionRequirement(params string[] permissions)
    {
        Permissions = permissions ?? [];
    }
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    public PermissionAuthorizationHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        string? sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out Guid userId))
        {
            return;
        }

        using IServiceScope scope = _serviceProvider.CreateScope();
        IPermissionService permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

        Result<IReadOnlySet<string>> result = await permissionService.GetEffectivePermissionsAsync(userId);
        if (result.IsSuccess && result.Value.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}

public sealed class AnyPermissionAuthorizationHandler : AuthorizationHandler<AnyPermissionRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    public AnyPermissionAuthorizationHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AnyPermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        string? sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out Guid userId))
        {
            return;
        }

        using IServiceScope scope = _serviceProvider.CreateScope();
        IPermissionService permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

        Result<IReadOnlySet<string>> result = await permissionService.GetEffectivePermissionsAsync(userId);
        if (result.IsSuccess && requirement.Permissions.Any(result.Value.Contains))
        {
            context.Succeed(requirement);
        }
    }
}
