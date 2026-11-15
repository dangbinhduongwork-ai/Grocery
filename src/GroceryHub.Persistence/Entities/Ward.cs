namespace GroceryHub.Persistence.Entities;

public sealed class Ward
{
    public string Code { get; set; } = string.Empty;

    public string ProvinceCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public Province? Province { get; set; }
}
