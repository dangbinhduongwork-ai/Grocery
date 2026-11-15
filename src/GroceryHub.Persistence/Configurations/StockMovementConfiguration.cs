using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", DatabaseSchemas.Inventory, t =>
        {
            t.HasCheckConstraint("CK_StockMovements_Type", "[Type] IN ('PurchaseReceipt', 'Sale', 'Adjustment', 'Return', 'Waste')");
            t.HasCheckConstraint("CK_StockMovements_Quantities", "[OnHandBefore] >= 0 AND [OnHandAfter] >= 0 AND [UnitCost] >= 0");
        });

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .UseIdentityColumn();

        builder.Property(m => m.ProductId)
            .IsRequired();

        builder.Property(m => m.Type)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(m => m.Quantity)
            .IsRequired();

        builder.Property(m => m.OnHandBefore)
            .IsRequired();

        builder.Property(m => m.OnHandAfter)
            .IsRequired();

        builder.Property(m => m.UnitCost)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(m => m.ReferenceType)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(m => m.ReferenceId)
            .IsRequired(false);

        builder.Property(m => m.Reason)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(m => m.ActorUserId)
            .IsRequired(false);

        builder.Property(m => m.At)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(m => new { m.ProductId, m.At })
            .HasDatabaseName("IX_StockMovements_ProductId_At");

        builder.HasIndex(m => new { m.ReferenceType, m.ReferenceId })
            .HasDatabaseName("IX_StockMovements_Reference")
            .HasFilter("[ReferenceId] IS NOT NULL");
    }
}
