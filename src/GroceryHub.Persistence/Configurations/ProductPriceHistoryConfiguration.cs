using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class ProductPriceHistoryConfiguration : IEntityTypeConfiguration<ProductPriceHistory>
{
    public void Configure(EntityTypeBuilder<ProductPriceHistory> builder)
    {
        builder.ToTable("ProductPriceHistory", DatabaseSchemas.Catalog);

        builder.HasKey(h => h.Id);

        builder.Property(h => h.ProductId)
            .IsRequired();

        builder.Property(h => h.OldSellingPrice)
            .IsRequired();

        builder.Property(h => h.NewSellingPrice)
            .IsRequired();

        builder.Property(h => h.OldListPrice)
            .IsRequired();

        builder.Property(h => h.NewListPrice)
            .IsRequired();

        builder.Property(h => h.ChangedBy)
            .IsRequired(false);

        builder.Property(h => h.Reason)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(h => h.ChangedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(h => h.Product)
            .WithMany(p => p.PriceHistories)
            .HasForeignKey(h => h.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.ProductId, h.ChangedAt })
            .HasDatabaseName("IX_ProductPriceHistory_ProductId_ChangedAt");
    }
}
