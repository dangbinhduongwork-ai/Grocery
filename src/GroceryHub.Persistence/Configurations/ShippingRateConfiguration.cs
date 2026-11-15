using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class ShippingRateConfiguration : IEntityTypeConfiguration<ShippingRate>
{
    public void Configure(EntityTypeBuilder<ShippingRate> builder)
    {
        builder.ToTable("ShippingRates", DatabaseSchemas.Ordering, t =>
        {
            t.HasCheckConstraint("CK_ShippingRates_Amounts", "[BaseFee] >= 0 AND [FreeShippingThreshold] >= 0 AND [EstimatedDaysMin] >= 0 AND [EstimatedDaysMax] >= [EstimatedDaysMin]");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ZoneId)
            .IsRequired();

        builder.Property(r => r.BaseFee)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(r => r.FreeShippingThreshold)
            .HasColumnType("bigint")
            .IsRequired()
            .HasDefaultValue(300_000L);

        builder.Property(r => r.EstimatedDaysMin)
            .IsRequired();

        builder.Property(r => r.EstimatedDaysMax)
            .IsRequired();

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(r => r.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(r => r.Zone)
            .WithMany(z => z.ShippingRates)
            .HasForeignKey(r => r.ZoneId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.ZoneId)
            .HasDatabaseName("IX_ShippingRates_ZoneId");
    }
}
