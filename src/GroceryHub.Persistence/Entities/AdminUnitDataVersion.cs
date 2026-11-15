namespace GroceryHub.Persistence.Entities;

public sealed class AdminUnitDataVersion
{
    public Guid Id { get; set; }

    public string Source { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public DateTimeOffset ImportedAt { get; set; }

    public string Licence { get; set; } = string.Empty;
}
