using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("Provinces", DatabaseSchemas.Shared);

        builder.HasKey(x => x.Code);

        builder.Property(x => x.Code)
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
    }
}
