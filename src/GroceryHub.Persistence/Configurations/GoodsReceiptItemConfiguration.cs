using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class GoodsReceiptItemConfiguration : IEntityTypeConfiguration<GoodsReceiptItem>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptItem> builder)
    {
        builder.ToTable("GoodsReceiptItems", DatabaseSchemas.Purchasing, t =>
        {
            t.HasCheckConstraint("CK_GoodsReceiptItems_Quantities", "[ReceivedQuantity] > 0 AND [UnitCost] >= 0");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.GoodsReceiptId)
            .IsRequired();

        builder.Property(i => i.PurchaseOrderItemId)
            .IsRequired(false);

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.Property(i => i.ReceivedQuantity)
            .IsRequired();

        builder.Property(i => i.UnitCost)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(i => i.GoodsReceipt)
            .WithMany(r => r.Items)
            .HasForeignKey(i => i.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.GoodsReceiptId)
            .HasDatabaseName("IX_GoodsReceiptItems_GoodsReceiptId");

        builder.HasIndex(i => i.ProductId)
            .HasDatabaseName("IX_GoodsReceiptItems_ProductId");
    }
}
