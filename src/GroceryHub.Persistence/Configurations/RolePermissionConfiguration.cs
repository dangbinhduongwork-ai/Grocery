using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions", DatabaseSchemas.Identity);

        builder.HasKey(rp => new { rp.RoleId, rp.PermissionCode });

        builder.Property(rp => rp.PermissionCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(rp => rp.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionCode)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rp => rp.PermissionCode);
    }
}
