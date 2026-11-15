using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public class ShippingZone : IAuditableEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<ShippingZoneProvince> ServiceableProvinces { get; set; } = new List<ShippingZoneProvince>();

    public ICollection<ShippingRate> ShippingRates { get; set; } = new List<ShippingRate>();
}
