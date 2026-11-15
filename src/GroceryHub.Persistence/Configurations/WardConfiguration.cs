using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder.ToTable("Wards", DatabaseSchemas.Shared);

        builder.HasKey(x => x.Code);

        builder.Property(x => x.Code)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.ProvinceCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.FullName)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasMaxLength(40)
            .IsRequired();

        builder.HasOne(x => x.Province)
            .WithMany(p => p.Wards)
            .HasForeignKey(x => x.ProvinceCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ProvinceCode)
            .HasDatabaseName("IX_Wards_ProvinceCode");
    }
}
