using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons", DatabaseSchemas.Promotions, t =>
        {
            t.HasCheckConstraint("CK_Coupons_Type", "[Type] IN ('Percent', 'FixedAmount', 'FreeShipping')");
            t.HasCheckConstraint(
                "CK_Coupons_ValueByType",
                "([Type] = 'Percent' AND [Value] BETWEEN 1 AND 100) OR ([Type] = 'FixedAmount' AND [Value] > 0) OR ([Type] = 'FreeShipping' AND [Value] = 0)");
            t.HasCheckConstraint(
                "CK_Coupons_Limits",
                "[MinSubtotal] >= 0 AND ([MaxDiscount] IS NULL OR [MaxDiscount] > 0) AND [UsedCount] >= 0 AND ([UsageLimit] IS NULL OR ([UsageLimit] > 0 AND [UsedCount] <= [UsageLimit])) AND ([PerUserLimit] IS NULL OR [PerUserLimit] > 0) AND [StartsAt] < [EndsAt]");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(c => c.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(c => c.Type)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(c => c.Value)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(c => c.MaxDiscount)
            .HasColumnType("bigint")
            .IsRequired(false);

        builder.Property(c => c.MinSubtotal)
            .HasColumnType("bigint")
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(c => c.StartsAt)
            .IsRequired();

        builder.Property(c => c.EndsAt)
            .IsRequired();

        builder.Property(c => c.UsageLimit)
            .IsRequired(false);

        builder.Property(c => c.PerUserLimit)
            .IsRequired(false)
            .HasDefaultValue(1);

        builder.Property(c => c.UsedCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(c => c.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(c => c.RowVersion)
            .IsRowVersion();

        builder.HasIndex(c => c.Code)
            .IsUnique()
            .HasDatabaseName("UQ_Coupons_Code");

        builder.HasIndex(c => new { c.IsActive, c.StartsAt, c.EndsAt })
            .HasDatabaseName("IX_Coupons_IsActive_Dates")
            .IncludeProperties(c => new
            {
                c.Code,
                c.Type,
                c.Value,
                c.MaxDiscount,
                c.MinSubtotal,
                c.UsageLimit,
                c.UsedCount
            });
    }
}
