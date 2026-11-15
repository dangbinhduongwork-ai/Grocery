using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", DatabaseSchemas.Catalog, t =>
        {
            t.HasCheckConstraint("CK_Products_SellingPrice", "[SellingPrice] >= 0");
            t.HasCheckConstraint("CK_Products_ListPrice", "[ListPrice] >= 0");
            t.HasCheckConstraint("CK_Products_SellingPrice_ListPrice", "[SellingPrice] <= [ListPrice]");
            t.HasCheckConstraint("CK_Products_CostPrice", "[CostPrice] >= 0");
            t.HasCheckConstraint("CK_Products_Status", "[Status] IN ('Draft', 'Active', 'Archived')");
            t.HasCheckConstraint("CK_Products_Rating", "[RatingAvg] >= 0.00 AND [RatingAvg] <= 5.00 AND [RatingCount] >= 0 AND [SoldCount] >= 0");
            t.HasCheckConstraint("CK_Products_SpecsJson", "[SpecsJson] IS NULL OR ISJSON([SpecsJson]) = 1");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(p => p.Barcode)
            .HasMaxLength(32)
            .IsRequired(false);

        builder.Property(p => p.Name)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasMaxLength(280)
            .IsRequired();

        builder.Property(p => p.SearchText)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.CategoryId)
            .IsRequired();

        builder.Property(p => p.BrandId)
            .IsRequired(false);

        builder.Property(p => p.Unit)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.PackSize)
            .HasMaxLength(60)
            .IsRequired(false);

        builder.Property(p => p.ListPrice)
            .IsRequired();

        builder.Property(p => p.SellingPrice)
            .IsRequired();

        builder.Property(p => p.CostPrice)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(p => p.Status)
            .HasMaxLength(24)
            .IsRequired()
            .HasDefaultValue(ProductStatus.Draft);

        builder.Property(p => p.ShortDescription)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(p => p.Description)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(p => p.SpecsJson)
            .HasMaxLength(4000)
            .IsRequired(false);

        builder.Property(p => p.IsFeatured)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.RatingAvg)
            .HasPrecision(3, 2)
            .IsRequired()
            .HasDefaultValue(0.00m);

        builder.Property(p => p.RatingCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.SoldCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.ExpiryTracked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.DeletedAt)
            .IsRequired(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Sku)
            .IsUnique()
            .HasDatabaseName("UQ_Products_Sku");

        builder.HasIndex(p => p.Slug)
            .IsUnique()
            .HasDatabaseName("UQ_Products_Slug");

        builder.HasIndex(p => p.Barcode)
            .HasDatabaseName("IX_Products_Barcode")
            .HasFilter("[Barcode] IS NOT NULL AND [DeletedAt] IS NULL")
            .IncludeProperties(p => new { p.Sku, p.Name, p.Status });

        builder.HasIndex(p => new { p.CategoryId, p.Status, p.SellingPrice })
            .HasDatabaseName("IX_Products_Category_Status_SellingPrice")
            .HasFilter("[DeletedAt] IS NULL")
            .IncludeProperties(p => new
            {
                p.Name,
                p.Slug,
                p.Sku,
                p.BrandId,
                p.Unit,
                p.PackSize,
                p.ListPrice,
                p.IsFeatured,
                p.RatingAvg,
                p.RatingCount,
                p.SoldCount,
                p.CreatedAt
            });

        builder.HasIndex(p => new { p.Status, p.SoldCount, p.CreatedAt })
            .HasDatabaseName("IX_Products_Status_SoldCount")
            .HasFilter("[DeletedAt] IS NULL")
            .IncludeProperties(p => new
            {
                p.Name,
                p.Slug,
                p.CategoryId,
                p.BrandId,
                p.SellingPrice,
                p.ListPrice,
                p.Unit,
                p.PackSize,
                p.RatingAvg,
                p.RatingCount
            });

        builder.HasIndex(p => new { p.BrandId, p.Status })
            .HasDatabaseName("IX_Products_BrandId_Status")
            .HasFilter("[BrandId] IS NOT NULL AND [DeletedAt] IS NULL")
            .IncludeProperties(p => new { p.CategoryId, p.SellingPrice });

        builder.HasIndex(p => p.SearchText)
            .HasDatabaseName("IX_Products_SearchText")
            .HasFilter("[Status] = 'Active' AND [DeletedAt] IS NULL")
            .IncludeProperties(p => new
            {
                p.Id,
                p.Name,
                p.Slug,
                p.CategoryId,
                p.BrandId,
                p.SellingPrice,
                p.ListPrice,
                p.Status,
                p.SoldCount
            });
    }
}
