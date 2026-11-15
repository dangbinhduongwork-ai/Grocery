using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace GroceryHub.Modules.Identity.Application;

public sealed class PermissionService : IPermissionService
{
    private static CancellationTokenSource _resetCacheToken = new();
    private static readonly object SyncRoot = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private readonly GroceryHubDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public PermissionService(GroceryHubDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<Result<IReadOnlySet<string>>> GetEffectivePermissionsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        string cacheKey = BuildUserCacheKey(userId);

        if (_cache.TryGetValue(cacheKey, out IReadOnlySet<string>? cached) && cached is not null)
        {
            return Result.Success(cached);
        }

        User? user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null || user.Status != UserStatus.Active || user.DeletedAt is not null)
        {
            return Result.Failure<IReadOnlySet<string>>(
                DomainError.Forbidden(ErrorCodes.AccountLocked, "User account is locked, deleted or inactive."));
        }

        List<string> rolePermissions = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.PermissionCode))
            .ToListAsync(ct);

        List<string> userPermissions = await _dbContext.UserPermissions
            .AsNoTracking()
            .Where(up => up.UserId == userId)
            .Select(up => up.PermissionCode)
            .ToListAsync(ct);

        HashSet<string> effective = new(rolePermissions.Concat(userPermissions), StringComparer.Ordinal);
        IReadOnlySet<string> immutableSet = effective;

        lock (SyncRoot)
        {
            MemoryCacheEntryOptions options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheTtl)
                .AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token));

            _cache.Set(cacheKey, immutableSet, options);
        }

        return Result.Success(immutableSet);
    }

    public void InvalidateUser(Guid userId)
    {
        _cache.Remove(BuildUserCacheKey(userId));
    }

    public void InvalidateAll()
    {
        lock (SyncRoot)
        {
            if (!_resetCacheToken.IsCancellationRequested && _resetCacheToken.Token.CanBeCanceled)
            {
                _resetCacheToken.Cancel();
                _resetCacheToken.Dispose();
            }

            _resetCacheToken = new CancellationTokenSource();
        }
    }

    public void InvalidateUserPermissionsCache(Guid userId) => InvalidateUser(userId);

    public void InvalidateAllPermissionsCache() => InvalidateAll();

    private static string BuildUserCacheKey(Guid userId) =>
        $"perm:user:{userId}";
}
