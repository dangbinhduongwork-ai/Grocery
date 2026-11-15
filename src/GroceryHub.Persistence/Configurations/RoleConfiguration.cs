using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", DatabaseSchemas.Identity);

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.NormalizedName)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(r => r.Description)
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(r => r.IsSystem)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.ConcurrencyStamp)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(r => r.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(r => r.Name)
            .IsUnique();

        builder.HasIndex(r => r.NormalizedName)
            .IsUnique()
            .HasFilter("[NormalizedName] IS NOT NULL");
    }
}
