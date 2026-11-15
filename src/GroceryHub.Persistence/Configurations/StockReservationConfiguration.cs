using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("StockReservations", DatabaseSchemas.Inventory, t =>
        {
            t.HasCheckConstraint("CK_StockReservations_Status", "[Status] IN ('Active', 'Released', 'Committed')");
            t.HasCheckConstraint("CK_StockReservations_Quantity", "[Quantity] > 0");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.OrderId)
            .IsRequired();

        builder.Property(r => r.ProductId)
            .IsRequired();

        builder.Property(r => r.Quantity)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasMaxLength(24)
            .IsRequired()
            .HasDefaultValue(StockReservationStatus.Active);

        builder.Property(r => r.ExpiresAt)
            .IsRequired(false);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(r => r.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(r => r.StockItem)
            .WithMany(s => s.Reservations)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.OrderId, r.ProductId })
            .IsUnique()
            .HasDatabaseName("UQ_StockReservations_OrderId_ProductId");

        builder.HasIndex(r => new { r.Status, r.ExpiresAt })
            .HasDatabaseName("IX_StockReservations_Status_ExpiresAt")
            .HasFilter("[Status] = 'Active' AND [ExpiresAt] IS NOT NULL")
            .IncludeProperties(r => new { r.OrderId, r.ProductId, r.Quantity });

        builder.HasIndex(r => new { r.ProductId, r.Status })
            .HasDatabaseName("IX_StockReservations_ProductId_Status")
            .IncludeProperties(r => new { r.Quantity, r.OrderId });
    }
}
