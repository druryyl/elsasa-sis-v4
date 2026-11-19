using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Contracts.Operational.Events;

public sealed record OperationalPeriodActivatedDomainEvent(
    Guid AcademicYearId,
    Guid OperationalPeriodId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate) : DomainEventBase;
