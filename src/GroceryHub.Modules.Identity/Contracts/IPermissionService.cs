using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Identity.Contracts;

public interface IPermissionService
{
    Task<Result<IReadOnlySet<string>>> GetEffectivePermissionsAsync(
        Guid userId,
        CancellationToken ct = default);

    void InvalidateUser(Guid userId);

    void InvalidateAll();

    void InvalidateUserPermissionsCache(Guid userId) => InvalidateUser(userId);

    void InvalidateAllPermissionsCache() => InvalidateAll();
}
