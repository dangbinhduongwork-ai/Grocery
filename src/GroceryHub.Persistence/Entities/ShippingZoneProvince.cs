namespace GroceryHub.Persistence.Entities;

public class ShippingZoneProvince
{
    public Guid ZoneId { get; set; }

    public string ProvinceCode { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public ShippingZone? Zone { get; set; }
}
