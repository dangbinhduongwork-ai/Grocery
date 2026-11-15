using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts", DatabaseSchemas.Cart);

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired(false);

        builder.Property(c => c.GuestCartToken)
            .HasMaxLength(64)
            .IsRequired(false);

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(c => c.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("IX_Carts_UserId");

        builder.HasIndex(c => c.GuestCartToken)
            .IsUnique()
            .HasDatabaseName("UQ_Carts_GuestCartToken")
            .HasFilter("[GuestCartToken] IS NOT NULL");
    }
}
