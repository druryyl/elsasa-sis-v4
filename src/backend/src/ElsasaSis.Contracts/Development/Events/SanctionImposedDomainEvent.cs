using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Contracts.Development.Events;

public sealed record SanctionImposedDomainEvent(
    Guid DisciplineRecordId,
    Guid StudentId,
    string Sanction,
    DateTime IncidentDate) : DomainEventBase;
