using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("WebhookEvents", DatabaseSchemas.Payments, t =>
        {
            t.HasCheckConstraint("CK_WebhookEvents_ProcessingStatus", "[ProcessingStatus] IN ('Received', 'Processed', 'Ignored', 'Failed')");
        });

        builder.HasKey(we => we.Id);

        builder.Property(we => we.Provider)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(we => we.ProviderTransactionId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(we => we.RawBody)
            .IsRequired();

        builder.Property(we => we.SignatureValid)
            .IsRequired();

        builder.Property(we => we.ReceivedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(we => we.ProcessingStatus)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(we => we.Error)
            .HasMaxLength(1000);

        builder.Property(we => we.DeliveryCount)
            .IsRequired()
            .HasDefaultValue(1);

        builder.HasIndex(we => new { we.Provider, we.ProviderTransactionId })
            .IsUnique()
            .HasDatabaseName("UQ_WebhookEvents_Provider_ProviderTransactionId");

        builder.HasIndex(we => new { we.ProcessingStatus, we.ReceivedAt })
            .HasDatabaseName("IX_WebhookEvents_ProcessingStatus_ReceivedAt");
    }
}
