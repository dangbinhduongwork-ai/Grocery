using System.Linq.Expressions;
using System.Reflection;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace GroceryHub.Persistence;

public class GroceryHubDbContext : DbContext
{
    private readonly GroceryHubPersistenceOptions? _persistenceOptions;

    public GroceryHubDbContext(DbContextOptions<GroceryHubDbContext> options)
        : base(options)
    {
    }

    public GroceryHubDbContext(
        DbContextOptions<GroceryHubDbContext> options,
        GroceryHubPersistenceOptions? persistenceOptions)
        : base(options)
    {
        _persistenceOptions = persistenceOptions;
    }

    protected GroceryHubDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<OutboxConsumerReceipt> OutboxConsumerReceipts => Set<OutboxConsumerReceipt>();

    public DbSet<Setting> Settings => Set<Setting>();

    public DbSet<Province> Provinces => Set<Province>();

    public DbSet<Ward> Wards => Set<Ward>();

    public DbSet<AdminUnitDataVersion> AdminUnitDataVersions => Set<AdminUnitDataVersion>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    public DbSet<IdempotencyRecord> IdempotencyKeys => Set<IdempotencyRecord>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new SoftDeleteQueryFilterConvention());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasSequence<long>(DatabaseSequences.OrderNumberSeq, DatabaseSchemas.Ordering)
            .StartsAt(1)
            .IncrementsBy(1)
            .IsCyclic(false);

        modelBuilder.HasSequence<long>(DatabaseSequences.PaymentCodeSeq, DatabaseSchemas.Payments)
            .StartsAt(DatabaseSequences.PaymentCodeStartValue)
            .IncrementsBy(1)
            .IsCyclic(false);

        modelBuilder.HasSequence<long>(DatabaseSequences.PurchaseOrderSeq, DatabaseSchemas.Purchasing)
            .StartsAt(1)
            .IncrementsBy(1)
            .IsCyclic(false);

        modelBuilder.HasSequence<long>(DatabaseSequences.GoodsReceiptSeq, DatabaseSchemas.Purchasing)
            .StartsAt(1)
            .IncrementsBy(1)
            .IsCyclic(false);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GroceryHubDbContext).Assembly);

        if (_persistenceOptions is not null)
        {
            foreach (Assembly assembly in _persistenceOptions.ConfigurationAssemblies.Distinct())
            {
                if (assembly != typeof(GroceryHubDbContext).Assembly)
                {
                    modelBuilder.ApplyConfigurationsFromAssembly(assembly);
                }
            }

            _persistenceOptions.ConfigureModel?.Invoke(modelBuilder);
        }

        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    public static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.BaseType is null && typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(BuildSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    internal static LambdaExpression BuildSoftDeleteFilter(Type clrType)
    {
        ParameterExpression parameter = Expression.Parameter(clrType, "e");
        MemberExpression deletedAtProperty = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
        BinaryExpression filterBody = Expression.Equal(
            deletedAtProperty,
            Expression.Constant(null, typeof(DateTimeOffset?)));

        return Expression.Lambda(filterBody, parameter);
    }

    private sealed class SoftDeleteQueryFilterConvention : IModelFinalizingConvention
    {
        public void ProcessModelFinalizing(
            IConventionModelBuilder modelBuilder,
            IConventionContext<IConventionModelBuilder> context)
        {
            foreach (IConventionEntityType entityType in modelBuilder.Metadata.GetEntityTypes())
            {
                if (entityType.BaseType is null &&
                    typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType) &&
                    entityType.GetDeclaredQueryFilters().Count == 0)
                {
                    entityType.SetQueryFilter(BuildSoftDeleteFilter(entityType.ClrType));
                }
            }
        }
    }
}
