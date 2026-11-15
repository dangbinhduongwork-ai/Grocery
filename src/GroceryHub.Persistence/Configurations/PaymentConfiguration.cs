using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", DatabaseSchemas.Payments, t =>
        {
            t.HasCheckConstraint("CK_Payments_Method", "[Method] IN ('COD', 'SEPAY_QR')");
            t.HasCheckConstraint(
                "CK_Payments_Status",
                "[Status] IN ('Unpaid', 'Pending', 'Paid', 'Failed', 'Expired', 'PartiallyRefunded', 'Refunded')");
            t.HasCheckConstraint("CK_Payments_Amount", "[Amount] >= 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrderId)
            .IsRequired();

        builder.Property(p => p.PaymentCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.Method)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(p => p.PaidAt)
            .IsRequired(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.HasIndex(p => p.OrderId)
            .IsUnique()
            .HasDatabaseName("UQ_Payments_OrderId");

        builder.HasIndex(p => p.PaymentCode)
            .IsUnique()
            .HasDatabaseName("UQ_Payments_PaymentCode");
    }
}
