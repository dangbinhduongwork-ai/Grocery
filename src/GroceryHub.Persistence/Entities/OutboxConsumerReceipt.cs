namespace GroceryHub.Persistence.Entities;

public sealed class OutboxConsumerReceipt
{
    public Guid MessageId { get; set; }

    public string HandlerName { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; set; }
}
