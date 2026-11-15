using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions", DatabaseSchemas.Identity);

        builder.HasKey(up => new { up.UserId, up.PermissionCode });

        builder.Property(up => up.PermissionCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(up => up.GrantedBy)
            .IsRequired();

        builder.Property(up => up.GrantedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(up => up.User)
            .WithMany(u => u.UserPermissions)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.Permission)
            .WithMany(p => p.UserPermissions)
            .HasForeignKey(up => up.PermissionCode)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.Granter)
            .WithMany()
            .HasForeignKey(up => up.GrantedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(up => up.PermissionCode);

        builder.HasIndex(up => up.GrantedBy);
    }
}
