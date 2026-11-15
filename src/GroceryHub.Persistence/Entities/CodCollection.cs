namespace GroceryHub.Persistence.Entities;

public class CodCollection
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public Guid OrderId { get; set; }

    public Guid CollectedBy { get; set; }

    public long Amount { get; set; }

    public DateTimeOffset CollectedAt { get; set; }

    public string? Note { get; set; }

    public Payment? Payment { get; set; }

    public User? Collector { get; set; }
}
