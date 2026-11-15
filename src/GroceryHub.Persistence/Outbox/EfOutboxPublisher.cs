using System.Text.Json;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;

namespace GroceryHub.Persistence.Outbox;

public sealed class EfOutboxPublisher(
    GroceryHubDbContext dbContext,
    TimeProvider timeProvider) : IOutboxPublisher
{
    private readonly GroceryHubDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public void Enqueue<TEvent>(TEvent integrationEvent, string? correlationId = null)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        Type runtimeType = integrationEvent.GetType();
        string eventType = !string.IsNullOrWhiteSpace(integrationEvent.EventType)
            ? integrationEvent.EventType
            : runtimeType.FullName ?? runtimeType.Name;

        OutboxEventTypeRegistry.Register(runtimeType, eventType);

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        Guid messageId = integrationEvent.EventId != Guid.Empty
            ? integrationEvent.EventId
            : Guid.CreateVersion7(utcNow);

        DateTimeOffset occurredAt = integrationEvent.OccurredAt != default
            ? integrationEvent.OccurredAt
            : utcNow;

        string payload = JsonSerializer.Serialize(
            integrationEvent,
            runtimeType,
            OutboxEventTypeRegistry.SerializerOptions);

        OutboxMessage outboxMessage = new()
        {
            Id = messageId,
            EventType = eventType,
            Payload = payload,
            CorrelationId = correlationId,
            OccurredAt = occurredAt,
            Attempts = 0,
        };

        _dbContext.OutboxMessages.Add(outboxMessage);
    }
}
