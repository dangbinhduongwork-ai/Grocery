using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", DatabaseSchemas.Ordering, t =>
        {
            t.HasCheckConstraint("CK_OrderItems_Quantity", "[Quantity] BETWEEN 1 AND 99");
            t.HasCheckConstraint("CK_OrderItems_Amounts", "[UnitPrice] >= 0 AND [Subtotal] >= 0 AND [DiscountAllocated] >= 0 AND [UnitCostSnapshot] >= 0");
        });

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.OrderId)
            .IsRequired();

        builder.Property(oi => oi.ProductId)
            .IsRequired();

        builder.Property(oi => oi.ProductName)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(oi => oi.ProductSku)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(oi => oi.ProductUnit)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(oi => oi.ProductPackSize)
            .HasMaxLength(60)
            .IsRequired(false);

        builder.Property(oi => oi.UnitPrice)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(oi => oi.Quantity)
            .IsRequired();

        builder.Property(oi => oi.Subtotal)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(oi => oi.DiscountAllocated)
            .HasColumnType("bigint")
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(oi => oi.UnitCostSnapshot)
            .HasColumnType("bigint")
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(oi => oi.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(oi => new { oi.OrderId, oi.ProductId })
            .IsUnique()
            .HasDatabaseName("UQ_OrderItems_OrderId_ProductId");
    }
}
