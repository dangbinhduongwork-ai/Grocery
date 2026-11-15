using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class AdminUnitDataVersionConfiguration : IEntityTypeConfiguration<AdminUnitDataVersion>
{
    public void Configure(EntityTypeBuilder<AdminUnitDataVersion> builder)
    {
        builder.ToTable("AdminUnitDataVersion", DatabaseSchemas.Shared);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Source)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Version)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(x => x.ImportedAt)
            .IsRequired();

        builder.Property(x => x.Licence)
            .HasMaxLength(120)
            .IsRequired();
    }
}
