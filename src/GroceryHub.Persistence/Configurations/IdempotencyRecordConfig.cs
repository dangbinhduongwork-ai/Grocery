using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyKeys", DatabaseSchemas.Ordering);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Scope)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.UserId);

        builder.Property(x => x.RequestHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ResponseStatus)
            .IsRequired();

        builder.Property(x => x.ResponseContentType)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.ResponseBody)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.HasIndex(x => new { x.Key, x.Scope })
            .IsUnique()
            .HasDatabaseName("UQ_IdempotencyKeys_Key_Scope");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_IdempotencyKeys_ExpiresAt");
    }
}
