using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds", DatabaseSchemas.Payments, t =>
        {
            t.HasCheckConstraint("CK_Refunds_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_Refunds_Status", "[Status] IN ('Requested', 'Approved', 'Rejected', 'Completed')");
            t.HasCheckConstraint("CK_Refunds_TwoPersonRule", "[DecidedBy] IS NULL OR [DecidedBy] <> [RequestedBy]");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.PaymentId)
            .IsRequired();

        builder.Property(r => r.OrderId)
            .IsRequired();

        builder.Property(r => r.Amount)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(r => r.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(r => r.RequestedBy)
            .IsRequired();

        builder.Property(r => r.RequestedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(r => r.BankReference)
            .HasMaxLength(128);

        builder.Property(r => r.Note)
            .HasMaxLength(500);

        builder.HasOne(r => r.Payment)
            .WithMany(p => p.Refunds)
            .HasForeignKey(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Requester)
            .WithMany()
            .HasForeignKey(r => r.RequestedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Decider)
            .WithMany()
            .HasForeignKey(r => r.DecidedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Completer)
            .WithMany()
            .HasForeignKey(r => r.CompletedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.PaymentId)
            .HasDatabaseName("IX_Refunds_PaymentId");

        builder.HasIndex(r => r.OrderId)
            .HasDatabaseName("IX_Refunds_OrderId");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_Refunds_Status");
    }
}
