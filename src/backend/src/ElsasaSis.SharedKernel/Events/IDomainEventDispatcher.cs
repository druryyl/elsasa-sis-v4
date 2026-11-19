using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.SharedKernel.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IEnumerable<DomainEventBase> domainEvents,
        CancellationToken cancellationToken = default);
}
