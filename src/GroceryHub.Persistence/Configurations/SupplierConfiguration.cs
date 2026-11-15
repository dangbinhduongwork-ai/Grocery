using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers", DatabaseSchemas.Purchasing);

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.ContactPerson)
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(s => s.Phone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.Email)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(s => s.Address)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(s => s.TaxCode)
            .HasMaxLength(32)
            .IsRequired(false);

        builder.Property(s => s.Note)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.DeletedAt)
            .IsRequired(false);

        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(s => s.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(s => s.Code)
            .IsUnique()
            .HasDatabaseName("UQ_Suppliers_Code");

        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("UQ_Suppliers_Name");

        builder.HasIndex(s => s.Name)
            .HasDatabaseName("IX_Suppliers_Name")
            .HasFilter("[DeletedAt] IS NULL");
    }
}
