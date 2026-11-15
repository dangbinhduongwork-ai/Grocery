using GroceryHub.Modules.Identity.Application;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace GroceryHub.UnitTests.Identity;

public sealed class PermissionServiceTests : IDisposable
{
    private readonly GroceryHubDbContext _dbContext;
    private readonly IMemoryCache _memoryCache;
    private readonly PermissionService _permissionService;

    public PermissionServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<GroceryHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GroceryHubDbContext(dbOptions);
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _permissionService = new PermissionService(_dbContext, _memoryCache);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _memoryCache.Dispose();
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_CombinesRoleAndDirectPermissions()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "staff@groceryhub.vn",
            FullName = "Staff Member",
            Status = "Active"
        };

        var role = new Role { Id = Guid.NewGuid(), Name = "Staff" };
        var perm1 = new Permission { Code = "orders.read", Description = "Read orders", Group = "Ordering" };
        var perm2 = new Permission { Code = "orders.update_status", Description = "Update status", Group = "Ordering" };
        var permExtra = new Permission { Code = "coupons.read", Description = "Read coupons", Group = "Promotions" };

        _dbContext.Users.Add(user);
        _dbContext.Roles.Add(role);
        _dbContext.Permissions.AddRange(perm1, perm2, permExtra);
        _dbContext.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });
        _dbContext.RolePermissions.AddRange(
            new RolePermission { RoleId = role.Id, PermissionCode = perm1.Code },
            new RolePermission { RoleId = role.Id, PermissionCode = perm2.Code }
        );
        _dbContext.UserPermissions.Add(new UserPermission { UserId = userId, PermissionCode = permExtra.Code });

        await _dbContext.SaveChangesAsync();

        var result = await _permissionService.GetEffectivePermissionsAsync(userId);

        Assert.True(result.IsSuccess);
        var permissions = result.Value;
        Assert.Equal(3, permissions.Count);
        Assert.Contains("orders.read", permissions);
        Assert.Contains("orders.update_status", permissions);
        Assert.Contains("coupons.read", permissions);

        // Verify cached
        Assert.True(_memoryCache.TryGetValue($"perm:user:{userId}", out IReadOnlySet<string>? cached));
        Assert.NotNull(cached);
        Assert.Equal(3, cached.Count);
    }

    [Fact]
    public async Task InvalidateUser_RemovesUserFromCache()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "staff2@groceryhub.vn",
            FullName = "Staff Member 2",
            Status = "Active"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        await _permissionService.GetEffectivePermissionsAsync(userId);
        Assert.True(_memoryCache.TryGetValue($"perm:user:{userId}", out _));

        _permissionService.InvalidateUser(userId);

        Assert.False(_memoryCache.TryGetValue($"perm:user:{userId}", out _));
    }

    [Fact]
    public async Task InvalidateAll_ClearsAllPermissionCaches()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        _dbContext.Users.AddRange(
            new User { Id = user1, Email = "u1@groceryhub.vn", FullName = "U1", Status = "Active" },
            new User { Id = user2, Email = "u2@groceryhub.vn", FullName = "U2", Status = "Active" }
        );
        await _dbContext.SaveChangesAsync();

        await _permissionService.GetEffectivePermissionsAsync(user1);
        await _permissionService.GetEffectivePermissionsAsync(user2);

        Assert.True(_memoryCache.TryGetValue($"perm:user:{user1}", out _));
        Assert.True(_memoryCache.TryGetValue($"perm:user:{user2}", out _));

        _permissionService.InvalidateAll();

        Assert.False(_memoryCache.TryGetValue($"perm:user:{user1}", out _));
        Assert.False(_memoryCache.TryGetValue($"perm:user:{user2}", out _));
    }
}
