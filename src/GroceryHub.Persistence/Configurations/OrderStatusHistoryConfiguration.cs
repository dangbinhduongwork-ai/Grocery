using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("OrderStatusHistory", DatabaseSchemas.Ordering);

        builder.HasKey(h => h.Id);

        builder.Property(h => h.OrderId)
            .IsRequired();

        builder.Property(h => h.FromStatus)
            .HasMaxLength(24)
            .IsRequired(false);

        builder.Property(h => h.ToStatus)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(h => h.Reason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(h => h.ActorUserId)
            .IsRequired(false);

        builder.Property(h => h.ActorRole)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(h => h.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(h => h.Order)
            .WithMany(o => o.StatusHistory)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ActorUser)
            .WithMany()
            .HasForeignKey(h => h.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.OrderId, h.CreatedAt })
            .HasDatabaseName("IX_OrderStatusHistory_OrderId_CreatedAt");
    }
}
