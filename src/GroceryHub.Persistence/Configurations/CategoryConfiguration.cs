using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", DatabaseSchemas.Catalog, t =>
        {
            t.HasCheckConstraint("CK_Categories_NoSelfParent", "[ParentId] IS NULL OR [ParentId] <> [Id]");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(c => c.Slug)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(c => c.ImageKey)
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(c => c.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(c => c.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(c => c.DeletedAt)
            .IsRequired(false);

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasDatabaseName("UQ_Categories_Slug");

        builder.HasIndex(c => new { c.ParentId, c.SortOrder })
            .HasDatabaseName("IX_Categories_ParentId_SortOrder")
            .HasFilter("[DeletedAt] IS NULL")
            .IncludeProperties(c => new { c.Name, c.Slug, c.IsActive });
    }
}
