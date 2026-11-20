using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Contracts.Academic.Events;

public sealed record ReportCardFinalizedDomainEvent(
    Guid ReportCardId,
    Guid StudentId,
    Guid OperationalPeriodId,
    DateTime FinalizedAt) : DomainEventBase;
