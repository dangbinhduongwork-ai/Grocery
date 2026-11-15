using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class ShippingZoneConfiguration : IEntityTypeConfiguration<ShippingZone>
{
    public void Configure(EntityTypeBuilder<ShippingZone> builder)
    {
        builder.ToTable("ShippingZones", DatabaseSchemas.Ordering);

        builder.HasKey(z => z.Id);

        builder.Property(z => z.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(z => z.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(z => z.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(z => z.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(z => z.Name)
            .IsUnique()
            .HasDatabaseName("UQ_ShippingZones_Name");
    }
}
