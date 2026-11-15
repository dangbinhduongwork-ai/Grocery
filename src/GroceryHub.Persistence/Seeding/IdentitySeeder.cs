using GroceryHub.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GroceryHub.Persistence.Seeding;

public static class IdentitySeeder
{
    public static readonly IReadOnlyList<(string Code, string Group, string Description, bool AllowPerUserGrant)> AllPermissions =
    [
        ("catalog.read", "Catalog", "View products, categories, and brands", true),
        ("catalog.write", "Catalog", "Create and update catalog items", true),
        ("catalog.price.write", "Catalog", "Update selling and list prices", false),
        ("catalog.cost.read", "Catalog", "View moving-average cost prices", false),
        ("inventory.read", "Inventory", "View inventory balances and movements", true),
        ("inventory.adjust", "Inventory", "Perform manual stock adjustments", false),
        ("purchasing.read", "Purchasing", "View suppliers and purchase orders", true),
        ("purchasing.write", "Purchasing", "Create and update purchase orders", false),
        ("purchasing.receive", "Purchasing", "Receive goods against purchase orders", true),
        ("orders.read", "Orders", "View customer orders and pick lists", true),
        ("orders.process", "Orders", "Process orders and handle early cancellations", true),
        ("orders.ship", "Orders", "Manage packing, shipping, and delivery status", true),
        ("orders.cancel", "Orders", "Cancel orders in later stages or paid status", false),
        ("customers.read", "Customers", "View customer directory and history", true),
        ("customers.lock", "Customers", "Lock or unlock customer accounts", false),
        ("staff.read", "Staff", "View staff members and assignments", false),
        ("staff.create", "Staff", "Create staff accounts", true),
        ("staff.manage", "Staff", "Manage staff accounts, roles, and anonymization", false),
        ("roles.manage", "Roles", "Manage role permissions matrix", false),
        ("payments.read", "Payments", "View payments, attempts, and transactions", false),
        ("payments.review", "Payments", "Review and resolve anomalous payments", false),
        ("refunds.request", "Payments", "Request refunds for paid orders", true),
        ("refunds.approve", "Payments", "Approve, reject, or complete refunds", false),
        ("promotions.manage", "Promotions", "Manage coupons, scopes, and banners", false),
        ("reviews.moderate", "Reviews", "Moderate customer product reviews", true),
        ("reports.revenue", "Reporting", "View sales and revenue dashboard reports", false),
        ("reports.profit", "Reporting", "View profit and margin reports", false),
        ("audit.read", "Audit", "Query security and business audit logs", false),
        ("settings.manage", "Settings", "Manage global store and payment settings", false),
    ];

    public static readonly IReadOnlyList<string> ManagerPermissionCodes =
    [
        "catalog.read",
        "catalog.write",
        "catalog.price.write",
        "catalog.cost.read",
        "inventory.read",
        "inventory.adjust",
        "purchasing.read",
        "purchasing.write",
        "purchasing.receive",
        "orders.read",
        "orders.process",
        "orders.ship",
        "orders.cancel",
        "customers.read",
        "customers.lock",
        "staff.read",
        "payments.read",
        "payments.review",
        "refunds.request",
        "refunds.approve",
    ];

    public static readonly IReadOnlyList<string> StaffPermissionCodes =
    [
        "catalog.read",
        "inventory.read",
        "purchasing.read",
        "orders.read",
        "orders.process",
        "orders.ship",
        "customers.read",
    ];

    public static async Task SeedIdentityAsync(
        GroceryHubDbContext db,
        IConfiguration config,
        TimeProvider timeProvider,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        DateTimeOffset now = timeProvider.GetUtcNow();

        // 1. Seed Roles
        Dictionary<string, Role> seededRoles = await SeedRolesAsync(db, now, cancellationToken);

        // 2. Seed 29 Permissions (idempotent upsert)
        await SeedPermissionsAsync(db, now, cancellationToken);

        // 3. Seed RolePermissions matrix
        await SeedRolePermissionsAsync(db, seededRoles, now, cancellationToken);

        // 4. Admin bootstrap (only if no user with Admin role exists)
        await BootstrapAdminUserAsync(db, config, seededRoles[PredefinedRoles.Admin], timeProvider, logger, cancellationToken);
    }

    private static async Task<Dictionary<string, Role>> SeedRolesAsync(
        GroceryHubDbContext db,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        (Guid Id, string Name, string NormalizedName, string Description)[] definedRoles =
        [
            (PredefinedRoles.AdminRoleId, PredefinedRoles.Admin, "ADMIN", "System administrator with full permissions"),
            (PredefinedRoles.ManagerRoleId, PredefinedRoles.Manager, "MANAGER", "Store operations manager"),
            (PredefinedRoles.StaffRoleId, PredefinedRoles.Staff, "STAFF", "Warehouse and operations staff member"),
            (PredefinedRoles.CustomerRoleId, PredefinedRoles.Customer, "CUSTOMER", "Registered retail customer"),
        ];

        List<Role> existingRoles = await db.Roles.ToListAsync(cancellationToken);
        Dictionary<string, Role> roleMap = new(StringComparer.OrdinalIgnoreCase);

        foreach ((Guid id, string name, string normalizedName, string description) in definedRoles)
        {
            Role? existing = existingRoles.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                existing = new Role
                {
                    Id = id,
                    Name = name,
                    NormalizedName = normalizedName,
                    Description = description,
                    IsSystem = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                db.Roles.Add(existing);
            }
            else
            {
                existing.NormalizedName = normalizedName;
                existing.Description = description;
                existing.IsSystem = true;
                existing.UpdatedAt = now;
            }

            roleMap[name] = existing;
        }

        await db.SaveChangesAsync(cancellationToken);
        return roleMap;
    }

    private static async Task SeedPermissionsAsync(
        GroceryHubDbContext db,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        List<Permission> existingPermissions = await db.Permissions.ToListAsync(cancellationToken);
        Dictionary<string, Permission> existingMap = existingPermissions
            .ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);

        foreach ((string code, string group, string description, bool allowPerUserGrant) in AllPermissions)
        {
            if (existingMap.TryGetValue(code, out Permission? existing))
            {
                existing.Group = group;
                existing.Description = description;
                existing.AllowPerUserGrant = allowPerUserGrant;
                existing.UpdatedAt = now;
            }
            else
            {
                db.Permissions.Add(new Permission
                {
                    Code = code,
                    Group = group,
                    Description = description,
                    AllowPerUserGrant = allowPerUserGrant,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolePermissionsAsync(
        GroceryHubDbContext db,
        Dictionary<string, Role> roles,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        Role adminRole = roles[PredefinedRoles.Admin];
        Role managerRole = roles[PredefinedRoles.Manager];
        Role staffRole = roles[PredefinedRoles.Staff];
        Role customerRole = roles[PredefinedRoles.Customer];

        Dictionary<Guid, IReadOnlyList<string>> roleTargetPermissions = new()
        {
            [adminRole.Id] = AllPermissions.Select(p => p.Code).ToArray(),
            [managerRole.Id] = ManagerPermissionCodes,
            [staffRole.Id] = StaffPermissionCodes,
            [customerRole.Id] = [],
        };

        List<RolePermission> existingRolePermissions = await db.RolePermissions.ToListAsync(cancellationToken);
        HashSet<(Guid RoleId, string PermissionCode)> existingSet = existingRolePermissions
            .Select(rp => (rp.RoleId, rp.PermissionCode))
            .ToHashSet();

        bool hasChanges = false;
        foreach ((Guid roleId, IReadOnlyList<string> permissionCodes) in roleTargetPermissions)
        {
            foreach (string permCode in permissionCodes)
            {
                if (!existingSet.Contains((roleId, permCode)))
                {
                    db.RolePermissions.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionCode = permCode,
                        CreatedAt = now,
                    });
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task BootstrapAdminUserAsync(
        GroceryHubDbContext db,
        IConfiguration config,
        Role adminRole,
        TimeProvider timeProvider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        bool adminExists = await db.UserRoles
            .AnyAsync(ur => ur.RoleId == adminRole.Id, cancellationToken);

        if (adminExists)
        {
            return;
        }

        string adminEmail = config["Seed:AdminEmail"]
            ?? config["Seed__AdminEmail"]
            ?? "admin@groceryhub.vn";

        string adminPassword = config["Seed:AdminPassword"]
            ?? config["Seed__AdminPassword"]
            ?? "Admin@GroceryHub2026!";

        string normalizedEmail = adminEmail.ToUpperInvariant();

        User? existingUser = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();

        if (existingUser is not null)
        {
            existingUser.Status = UserStatus.Active;
            existingUser.EmailConfirmed = true;
            existingUser.ForcePasswordChange = true;
            existingUser.UpdatedAt = now;

            db.UserRoles.Add(new UserRole
            {
                UserId = existingUser.Id,
                RoleId = adminRole.Id,
                CreatedAt = now,
            });

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded initial Admin user {Email} (password not logged)", adminEmail);
            return;
        }

        User newAdmin = new()
        {
            Id = Guid.NewGuid(),
            Email = adminEmail,
            NormalizedEmail = normalizedEmail,
            UserName = adminEmail,
            NormalizedUserName = normalizedEmail,
            FullName = "Quản trị viên hệ thống",
            Status = UserStatus.Active,
            EmailConfirmed = true,
            LockoutEnabled = true,
            AccessFailedCount = 0,
            SecurityStamp = Guid.NewGuid().ToString("D"),
            ConcurrencyStamp = Guid.NewGuid().ToString("D"),
            ForcePasswordChange = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        PasswordHasher<User> passwordHasher = new();
        newAdmin.PasswordHash = passwordHasher.HashPassword(newAdmin, adminPassword);

        db.Users.Add(newAdmin);

        db.UserRoles.Add(new UserRole
        {
            UserId = newAdmin.Id,
            RoleId = adminRole.Id,
            CreatedAt = now,
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded initial Admin user {Email} (password not logged)", adminEmail);
    }
}
