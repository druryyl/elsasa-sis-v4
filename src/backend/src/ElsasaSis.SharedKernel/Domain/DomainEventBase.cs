using MediatR;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.SharedKernel.Domain;

/// <summary>
/// Base record for in-process domain events dispatched via MediatR after transaction commit.
/// </summary>
public abstract record DomainEventBase : INotification
{
    public Guid EventId { get; init; } = UuidV7.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
