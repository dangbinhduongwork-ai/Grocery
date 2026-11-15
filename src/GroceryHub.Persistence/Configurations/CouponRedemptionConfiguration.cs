using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> builder)
    {
        builder.ToTable("CouponRedemptions", DatabaseSchemas.Promotions, t =>
        {
            t.HasCheckConstraint("CK_CouponRedemptions_Amount", "[Amount] >= 0");
        });

        builder.HasKey(cr => cr.Id);

        builder.Property(cr => cr.CouponId)
            .IsRequired();

        builder.Property(cr => cr.UserId)
            .IsRequired();

        builder.Property(cr => cr.OrderId)
            .IsRequired();

        builder.Property(cr => cr.Amount)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(cr => cr.RedeemedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(cr => cr.ReleasedAt)
            .IsRequired(false);

        builder.HasOne(cr => cr.Coupon)
            .WithMany(c => c.Redemptions)
            .HasForeignKey(cr => cr.CouponId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cr => cr.User)
            .WithMany()
            .HasForeignKey(cr => cr.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cr => cr.OrderId)
            .IsUnique()
            .HasDatabaseName("UQ_CouponRedemptions_OrderId");

        builder.HasIndex(cr => new { cr.CouponId, cr.UserId })
            .HasDatabaseName("IX_CouponRedemptions_CouponId_UserId")
            .IncludeProperties(cr => new { cr.OrderId, cr.Amount, cr.RedeemedAt })
            .HasFilter("[ReleasedAt] IS NULL");
    }
}
