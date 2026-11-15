namespace GroceryHub.Persistence.Entities;

public class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductSku { get; set; } = string.Empty;

    public string ProductUnit { get; set; } = string.Empty;

    public string? ProductPackSize { get; set; }

    public long UnitPrice { get; set; }

    public int Quantity { get; set; }

    public long Subtotal { get; set; }

    public long DiscountAllocated { get; set; } = 0;

    public long UnitCostSnapshot { get; set; } = 0;

    public DateTimeOffset CreatedAt { get; set; }

    public Order? Order { get; set; }

    public Product? Product { get; set; }
}
