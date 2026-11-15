using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class ShippingZoneProvinceConfiguration : IEntityTypeConfiguration<ShippingZoneProvince>
{
    public void Configure(EntityTypeBuilder<ShippingZoneProvince> builder)
    {
        builder.ToTable("ShippingZoneProvinces", DatabaseSchemas.Ordering);

        builder.HasKey(zp => new { zp.ZoneId, zp.ProvinceCode });

        builder.Property(zp => zp.ProvinceCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(zp => zp.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(zp => zp.Zone)
            .WithMany(z => z.ServiceableProvinces)
            .HasForeignKey(zp => zp.ZoneId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(zp => zp.ProvinceCode)
            .IsUnique()
            .HasDatabaseName("UQ_ShippingZoneProvinces_ProvinceCode");
    }
}
