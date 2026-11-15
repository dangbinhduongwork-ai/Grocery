using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", DatabaseSchemas.Ordering, t =>
        {
            t.HasCheckConstraint(
                "CK_Orders_Status",
                "[Status] IN ('PendingPayment', 'Placed', 'Confirmed', 'Preparing', 'Packed', 'Shipping', 'Delivered', 'Cancelled', 'DeliveryFailed')");
            t.HasCheckConstraint(
                "CK_Orders_PaymentMethod",
                "[PaymentMethod] IN ('COD', 'SEPAY_QR')");
            t.HasCheckConstraint(
                "CK_Orders_PaymentStatus",
                "[PaymentStatus] IN ('Unpaid', 'Pending', 'Paid', 'Failed', 'Expired', 'PartiallyRefunded', 'Refunded')");
            t.HasCheckConstraint(
                "CK_Orders_Amounts",
                "[Subtotal] >= 0 AND [CouponDiscount] >= 0 AND [ShippingFee] >= 0 AND [Total] >= 0");
        });

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(o => o.UserId)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(o => o.Subtotal)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(o => o.CouponDiscount)
            .HasColumnType("bigint")
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(o => o.ShippingFee)
            .HasColumnType("bigint")
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(o => o.Total)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(o => o.PaymentMethod)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(o => o.PaymentStatus)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(o => o.ShippingRecipientName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(o => o.ShippingPhone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.ShippingProvinceCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(o => o.ShippingWardCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(o => o.ShippingProvinceName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(o => o.ShippingWardName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(o => o.ShippingStreet)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(o => o.ShippingNote)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(o => o.CancellationReason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(o => o.CancelledBy)
            .IsRequired(false);

        builder.Property(o => o.CancelledAt)
            .IsRequired(false);

        builder.Property(o => o.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(o => o.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(o => o.RowVersion)
            .IsRowVersion();

        builder.HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique()
            .HasDatabaseName("UQ_Orders_OrderNumber");

        builder.HasIndex(o => new { o.UserId, o.CreatedAt })
            .HasDatabaseName("IX_Orders_UserId_CreatedAt");

        builder.HasIndex(o => new { o.Status, o.CreatedAt })
            .HasDatabaseName("IX_Orders_Status_CreatedAt");
    }
}
