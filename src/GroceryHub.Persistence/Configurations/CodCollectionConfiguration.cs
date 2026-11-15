using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class CodCollectionConfiguration : IEntityTypeConfiguration<CodCollection>
{
    public void Configure(EntityTypeBuilder<CodCollection> builder)
    {
        builder.ToTable("CodCollections", DatabaseSchemas.Payments, t =>
        {
            t.HasCheckConstraint("CK_CodCollections_Amount", "[Amount] > 0");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.PaymentId)
            .IsRequired();

        builder.Property(c => c.OrderId)
            .IsRequired();

        builder.Property(c => c.CollectedBy)
            .IsRequired();

        builder.Property(c => c.Amount)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(c => c.CollectedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(c => c.Note)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.HasOne(c => c.Payment)
            .WithMany(p => p.CodCollections)
            .HasForeignKey(c => c.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Collector)
            .WithMany()
            .HasForeignKey(c => c.CollectedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.OrderId)
            .HasDatabaseName("IX_CodCollections_OrderId");
    }
}
