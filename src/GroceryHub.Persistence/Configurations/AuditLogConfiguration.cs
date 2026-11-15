using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", DatabaseSchemas.Audit);

        builder.HasKey(al => al.Id);

        builder.Property(al => al.Id)
            .ValueGeneratedOnAdd();

        builder.Property(al => al.At)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(al => al.ActorUserId)
            .IsRequired(false);

        builder.Property(al => al.ActorRole)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(al => al.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.EntityId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.BeforeJson)
            .IsRequired(false);

        builder.Property(al => al.AfterJson)
            .IsRequired(false);

        builder.Property(al => al.Ip)
            .HasMaxLength(64)
            .IsRequired(false);

        builder.Property(al => al.CorrelationId)
            .HasMaxLength(64)
            .IsRequired(false);

        builder.HasIndex(al => new { al.EntityType, al.EntityId });

        builder.HasIndex(al => new { al.ActorUserId, al.At });
    }
}
