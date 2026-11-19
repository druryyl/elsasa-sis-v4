namespace ElsasaSis.SharedKernel.Domain;

/// <summary>
/// Base class for aggregate roots capable of raising domain events.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<DomainEventBase> _domainEvents = [];

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {
    }

    public IReadOnlyList<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(DomainEventBase domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
