namespace GroceryHub.Persistence.Entities;

public class ProductPriceHistory
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public long OldSellingPrice { get; set; }

    public long NewSellingPrice { get; set; }

    public long OldListPrice { get; set; }

    public long NewListPrice { get; set; }

    public Guid? ChangedBy { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset ChangedAt { get; set; }

    public Product? Product { get; set; }
}
