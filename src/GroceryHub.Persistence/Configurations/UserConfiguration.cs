using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", DatabaseSchemas.Identity, t =>
        {
            t.HasCheckConstraint("CK_Users_Status", "[Status] IN ('Active', 'Locked', 'Disabled')");
        });

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.NormalizedEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.UserName)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(u => u.NormalizedUserName)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(u => u.EmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.PhoneNumberConfirmed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.TwoFactorEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.Status)
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(UserStatus.Active);

        builder.Property(u => u.LockoutEnd)
            .IsRequired(false);

        builder.Property(u => u.LockoutEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.AccessFailedCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(u => u.SecurityStamp)
            .IsRequired(false);

        builder.Property(u => u.ConcurrencyStamp)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(u => u.ForcePasswordChange)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(u => u.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(u => u.DeletedAt)
            .IsRequired(false);

        builder.Property(u => u.RowVersion)
            .IsRowVersion()
            .IsRequired();

        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique();

        builder.HasIndex(u => u.NormalizedUserName)
            .IsUnique()
            .HasFilter("[NormalizedUserName] IS NOT NULL");

        builder.HasIndex(u => u.PhoneNumber)
            .HasFilter("[PhoneNumber] IS NOT NULL");

        builder.HasIndex(u => new { u.Status, u.CreatedAt });
    }
}
