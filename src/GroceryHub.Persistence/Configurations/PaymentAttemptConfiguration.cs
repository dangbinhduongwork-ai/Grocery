using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.ToTable("PaymentAttempts", DatabaseSchemas.Payments, t =>
        {
            t.HasCheckConstraint("CK_PaymentAttempts_Status", "[Status] IN ('Open', 'Paid', 'Expired', 'Superseded')");
            t.HasCheckConstraint("CK_PaymentAttempts_Amount", "[Amount] > 0");
        });

        builder.HasKey(pa => pa.Id);

        builder.Property(pa => pa.PaymentId)
            .IsRequired();

        builder.Property(pa => pa.PaymentCode)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(pa => pa.AttemptNumber)
            .IsRequired();

        builder.Property(pa => pa.Amount)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(pa => pa.Status)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(pa => pa.ExpiresAt)
            .IsRequired();

        builder.Property(pa => pa.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(pa => pa.Payment)
            .WithMany(p => p.Attempts)
            .HasForeignKey(pa => pa.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pa => pa.PaymentCode)
            .IsUnique()
            .HasDatabaseName("UQ_PaymentAttempts_PaymentCode");

        builder.HasIndex(pa => pa.ExpiresAt)
            .HasFilter("[Status] = 'Open'")
            .HasDatabaseName("IX_PaymentAttempts_ExpiresAt_Open");

        builder.HasIndex(pa => new { pa.PaymentId, pa.CreatedAt })
            .HasDatabaseName("IX_PaymentAttempts_PaymentId_CreatedAt");
    }
}
