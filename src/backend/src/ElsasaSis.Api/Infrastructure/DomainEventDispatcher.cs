using ElsasaSis.SharedKernel.Domain;
using ElsasaSis.SharedKernel.Events;
using MediatR;

namespace ElsasaSis.Api.Infrastructure;

/// <summary>
/// Dispatches domain events via MediatR <see cref="IPublisher"/>.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;

    public DomainEventDispatcher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task DispatchAsync(
        IEnumerable<DomainEventBase> domainEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
