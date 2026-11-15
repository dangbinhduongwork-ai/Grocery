namespace GroceryHub.SharedKernel;

public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}

public abstract record DomainEvent(Guid EventId, DateTimeOffset OccurredAt) : IDomainEvent;

public interface IIntegrationEvent : IDomainEvent
{
    string EventType { get; }
}

public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task<Result> HandleAsync(TEvent integrationEvent, CancellationToken ct = default);
}

public interface IOutboxPublisher
{
    void Enqueue<TEvent>(TEvent integrationEvent, string? correlationId = null)
        where TEvent : IIntegrationEvent;
}

public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; }

    DateTimeOffset? UpdatedAt { get; }
}

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; }

    bool IsDeleted => DeletedAt.HasValue;
}

public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity()
    {
    }

    protected Entity(TId id)
    {
        Id = id;
    }

    public TId Id { get; protected init; } = default!;

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() =>
        EqualityComparer<TId>.Default.GetHashCode(Id);
}

public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public IReadOnlyList<IDomainEvent> DequeueDomainEvents()
    {
        if (_domainEvents.Count == 0)
        {
            return [];
        }

        IDomainEvent[] copy = [.. _domainEvents];
        _domainEvents.Clear();
        return copy;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
