using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Entities;

public class ShippingRate : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid ZoneId { get; set; }

    public long BaseFee { get; set; }

    public long FreeShippingThreshold { get; set; } = 300_000L;

    public int EstimatedDaysMin { get; set; }

    public int EstimatedDaysMax { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public ShippingZone? Zone { get; set; }
}
