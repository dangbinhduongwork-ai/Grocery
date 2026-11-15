namespace GroceryHub.Persistence.Entities;

public class ProductImage
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string StorageKey { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsPrimary { get; set; }

    public string AltText { get; set; } = string.Empty;

    public int Width { get; set; } = 800;

    public int Height { get; set; } = 800;

    public DateTimeOffset CreatedAt { get; set; }

    public Product? Product { get; set; }
}
