using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands", DatabaseSchemas.Catalog);

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(b => b.Slug)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(b => b.LogoKey)
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(b => b.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(b => b.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(b => b.DeletedAt)
            .IsRequired(false);

        builder.HasIndex(b => b.Name)
            .IsUnique()
            .HasDatabaseName("UQ_Brands_Name");

        builder.HasIndex(b => b.Slug)
            .IsUnique()
            .HasDatabaseName("UQ_Brands_Slug");

        builder.HasIndex(b => new { b.IsActive, b.Name })
            .HasDatabaseName("IX_Brands_IsActive_Name")
            .HasFilter("[DeletedAt] IS NULL");
    }
}
