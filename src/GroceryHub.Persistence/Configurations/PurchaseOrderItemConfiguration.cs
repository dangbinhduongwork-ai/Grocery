using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("PurchaseOrderItems", DatabaseSchemas.Purchasing, t =>
        {
            t.HasCheckConstraint("CK_PurchaseOrderItems_Quantities", "[OrderedQuantity] > 0 AND [ReceivedQuantity] >= 0 AND [UnitCost] >= 0 AND [Subtotal] >= 0");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.PurchaseOrderId)
            .IsRequired();

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.Property(i => i.OrderedQuantity)
            .IsRequired();

        builder.Property(i => i.ReceivedQuantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(i => i.UnitCost)
            .IsRequired();

        builder.Property(i => i.Subtotal)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(i => i.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(i => i.PurchaseOrder)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.PurchaseOrderId, i.ProductId })
            .IsUnique()
            .HasDatabaseName("UQ_PurchaseOrderItems_PO_Product");

        builder.HasIndex(i => i.ProductId)
            .HasDatabaseName("IX_PurchaseOrderItems_ProductId");
    }
}
