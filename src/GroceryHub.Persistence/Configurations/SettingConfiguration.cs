using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("Settings", DatabaseSchemas.Shared);

        builder.HasKey(x => x.Key);

        builder.Property(x => x.Key)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Value)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.UpdatedBy);

        builder.Property(x => x.UpdatedAt)
            .IsRequired();
    }
}
