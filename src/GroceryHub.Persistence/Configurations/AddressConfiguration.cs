using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses", DatabaseSchemas.Customers);

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.RecipientName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(a => a.Phone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.ProvinceCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(a => a.WardCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(a => a.ProvinceName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(a => a.WardName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(a => a.Street)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(a => a.Note)
            .HasMaxLength(250)
            .IsRequired(false);

        builder.Property(a => a.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.DeletedAt)
            .IsRequired(false);

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(a => a.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.UserId)
            .IsUnique()
            .HasDatabaseName("UQ_Addresses_UserId_Default")
            .HasFilter("[IsDefault] = 1 AND [DeletedAt] IS NULL");

        builder.HasIndex(a => new { a.UserId, a.DeletedAt })
            .HasDatabaseName("IX_Addresses_UserId_DeletedAt")
            .IncludeProperties(a => new { a.IsDefault, a.ProvinceCode, a.WardCode });
    }
}
