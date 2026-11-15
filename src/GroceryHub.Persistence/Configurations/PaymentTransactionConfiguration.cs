using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions", DatabaseSchemas.Payments, t =>
        {
            t.HasCheckConstraint("CK_PaymentTransactions_Amount", "[Amount] >= 0");
            t.HasCheckConstraint("CK_PaymentTransactions_Direction", "[Direction] IN ('in', 'out')");
            t.HasCheckConstraint("CK_PaymentTransactions_ReviewStatus", "[ReviewStatus] IN ('None', 'NeedsReview', 'Resolved')");
        });

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.WebhookEventId)
            .IsRequired();

        builder.Property(pt => pt.Direction)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(pt => pt.Amount)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(pt => pt.AccountNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(pt => pt.Gateway)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(pt => pt.ReferenceCode)
            .HasMaxLength(128);

        builder.Property(pt => pt.Content)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(pt => pt.TransactionTime)
            .IsRequired();

        builder.Property(pt => pt.MatchStatus)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(pt => pt.ReviewStatus)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(pt => pt.ResolutionNote)
            .HasMaxLength(500);

        builder.HasOne(pt => pt.WebhookEvent)
            .WithOne(we => we.Transaction)
            .HasForeignKey<PaymentTransaction>(pt => pt.WebhookEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pt => pt.PaymentAttempt)
            .WithMany(pa => pa.Transactions)
            .HasForeignKey(pt => pt.PaymentAttemptId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(pt => pt.Resolver)
            .WithMany()
            .HasForeignKey(pt => pt.ResolvedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pt => pt.WebhookEventId)
            .IsUnique()
            .HasDatabaseName("UQ_PaymentTransactions_WebhookEventId");

        builder.HasIndex(pt => pt.ReviewStatus)
            .HasDatabaseName("IX_PaymentTransactions_ReviewStatus");

        builder.HasIndex(pt => pt.ReferenceCode)
            .HasDatabaseName("IX_PaymentTransactions_ReferenceCode");

        builder.HasIndex(pt => pt.PaymentAttemptId)
            .HasDatabaseName("IX_PaymentTransactions_PaymentAttemptId");
    }
}
