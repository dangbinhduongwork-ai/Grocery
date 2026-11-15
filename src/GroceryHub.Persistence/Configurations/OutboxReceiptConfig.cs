using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class OutboxConsumerReceiptConfiguration : IEntityTypeConfiguration<OutboxConsumerReceipt>
{
    public void Configure(EntityTypeBuilder<OutboxConsumerReceipt> builder)
    {
        builder.ToTable("OutboxConsumerReceipts", DatabaseSchemas.Shared);

        builder.HasKey(x => new { x.MessageId, x.HandlerName });

        builder.Property(x => x.MessageId)
            .IsRequired();

        builder.Property(x => x.HandlerName)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .IsRequired();
    }
}
