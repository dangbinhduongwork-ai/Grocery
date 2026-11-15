using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class CouponScopeConfiguration : IEntityTypeConfiguration<CouponScope>
{
    public void Configure(EntityTypeBuilder<CouponScope> builder)
    {
        builder.ToTable("CouponScopes", DatabaseSchemas.Promotions, t =>
        {
            t.HasCheckConstraint("CK_CouponScopes_ScopeType", "[ScopeType] IN ('Category', 'Product')");
        });

        builder.HasKey(cs => new { cs.CouponId, cs.ScopeType, cs.ScopeId });

        builder.Property(cs => cs.ScopeType)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(cs => cs.ScopeId)
            .IsRequired();

        builder.Property(cs => cs.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(cs => cs.Coupon)
            .WithMany(c => c.Scopes)
            .HasForeignKey(cs => cs.CouponId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
