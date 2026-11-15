using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class StaffProfileConfiguration : IEntityTypeConfiguration<StaffProfile>
{
    public void Configure(EntityTypeBuilder<StaffProfile> builder)
    {
        builder.ToTable("StaffProfiles", DatabaseSchemas.Identity);

        builder.HasKey(sp => sp.UserId);

        builder.Property(sp => sp.EmployeeCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(sp => sp.Position)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sp => sp.HiredAt)
            .IsRequired();

        builder.Property(sp => sp.Note)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(sp => sp.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(sp => sp.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(sp => sp.User)
            .WithOne(u => u.StaffProfile)
            .HasForeignKey<StaffProfile>(sp => sp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sp => sp.EmployeeCode)
            .IsUnique();
    }
}
