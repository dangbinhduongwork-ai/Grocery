using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroceryHub.Persistence.Configurations;

public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems", DatabaseSchemas.Inventory, t =>
        {
            t.HasCheckConstraint("CK_StockItems_Invariants", "[OnHand] >= 0 AND [Reserved] >= 0 AND [Reserved] <= [OnHand]");
        });

        builder.HasKey(s => s.ProductId);

        builder.Property(s => s.OnHand)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.Reserved)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.ReorderLevel)
            .IsRequired()
            .HasDefaultValue(10);

        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(s => s.UpdatedAt)
            .IsRequired(false)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(s => s.RowVersion)
            .IsRowVersion();

        builder.HasIndex(s => new { s.OnHand, s.Reserved, s.ReorderLevel })
            .HasDatabaseName("IX_StockItems_LowStock");
    }
}
