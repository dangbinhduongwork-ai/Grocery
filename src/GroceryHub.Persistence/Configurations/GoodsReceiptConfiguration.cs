using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.ToTable("GoodsReceipts", DatabaseSchemas.Purchasing);

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReceiptNumber)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(r => r.PurchaseOrderId)
            .IsRequired(false);

        builder.Property(r => r.ReceivedBy)
            .IsRequired();

        builder.Property(r => r.ReceivedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(r => r.Note)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(r => r.PurchaseOrder)
            .WithMany(p => p.GoodsReceipts)
            .HasForeignKey(r => r.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.ReceiptNumber)
            .IsUnique()
            .HasDatabaseName("UQ_GoodsReceipts_ReceiptNumber");

        builder.HasIndex(r => r.PurchaseOrderId)
            .HasDatabaseName("IX_GoodsReceipts_PurchaseOrderId")
            .HasFilter("[PurchaseOrderId] IS NOT NULL");

        builder.HasIndex(r => r.ReceivedAt)
            .HasDatabaseName("IX_GoodsReceipts_ReceivedAt");
    }
}
