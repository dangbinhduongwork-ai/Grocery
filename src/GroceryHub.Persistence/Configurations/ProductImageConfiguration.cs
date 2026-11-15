using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages", DatabaseSchemas.Catalog);

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.Property(i => i.StorageKey)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(i => i.AltText)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(i => i.IsPrimary)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.Width)
            .IsRequired()
            .HasDefaultValue(800);

        builder.Property(i => i.Height)
            .IsRequired()
            .HasDefaultValue(800);

        builder.Property(i => i.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.StorageKey)
            .IsUnique()
            .HasDatabaseName("UQ_ProductImages_StorageKey");

        builder.HasIndex(i => i.ProductId)
            .IsUnique()
            .HasDatabaseName("UQ_ProductImages_ProductId_Primary")
            .HasFilter("[IsPrimary] = 1");

        builder.HasIndex(i => new { i.ProductId, i.SortOrder })
            .HasDatabaseName("IX_ProductImages_ProductId_SortOrder");
    }
}
