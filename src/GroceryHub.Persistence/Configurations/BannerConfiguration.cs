using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.ToTable("Banners", DatabaseSchemas.Catalog, t =>
        {
            t.HasCheckConstraint("CK_Banners_Schedule", "[StartsAt] IS NULL OR [EndsAt] IS NULL OR [StartsAt] <= [EndsAt]");
        });

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(b => b.Subtitle)
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(b => b.ImageKey)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(b => b.LinkUrl)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(b => b.Placement)
            .HasMaxLength(40)
            .IsRequired()
            .HasDefaultValue(BannerPlacement.HomeHero);

        builder.Property(b => b.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(b => b.StartsAt)
            .IsRequired(false);

        builder.Property(b => b.EndsAt)
            .IsRequired(false);

        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(b => b.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(b => b.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(b => new { b.Placement, b.IsActive, b.SortOrder })
            .HasDatabaseName("IX_Banners_Placement_Active")
            .IncludeProperties(b => new { b.Title, b.ImageKey, b.LinkUrl, b.StartsAt, b.EndsAt });
    }
}
