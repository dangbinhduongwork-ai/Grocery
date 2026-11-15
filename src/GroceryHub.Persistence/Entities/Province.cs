namespace GroceryHub.Persistence.Entities;

public sealed class Province
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public ICollection<Ward> Wards { get; set; } = new List<Ward>();
}
