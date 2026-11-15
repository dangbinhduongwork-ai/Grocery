using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders", DatabaseSchemas.Purchasing, t =>
        {
            t.HasCheckConstraint("CK_PurchaseOrders_Status", "[Status] IN ('Draft', 'Submitted', 'PartiallyReceived', 'Received', 'Closed', 'Cancelled')");
            t.HasCheckConstraint("CK_PurchaseOrders_TotalAmount", "[TotalAmount] >= 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PoNumber)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(p => p.SupplierId)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasMaxLength(24)
            .IsRequired()
            .HasDefaultValue(PurchaseOrderStatus.Draft);

        builder.Property(p => p.TotalAmount)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(p => p.Note)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(p => p.CreatedBy)
            .IsRequired();

        builder.Property(p => p.SubmittedAt)
            .IsRequired(false);

        builder.Property(p => p.ClosedAt)
            .IsRequired(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.HasOne(p => p.Supplier)
            .WithMany(s => s.PurchaseOrders)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.PoNumber)
            .IsUnique()
            .HasDatabaseName("UQ_PurchaseOrders_PoNumber");

        builder.HasIndex(p => new { p.SupplierId, p.Status, p.CreatedAt })
            .HasDatabaseName("IX_PurchaseOrders_SupplierId_Status");

        builder.HasIndex(p => new { p.Status, p.CreatedAt })
            .HasDatabaseName("IX_PurchaseOrders_Status_CreatedAt");
    }
}
