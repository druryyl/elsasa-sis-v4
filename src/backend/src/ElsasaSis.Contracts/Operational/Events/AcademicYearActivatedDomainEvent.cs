using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Contracts.Operational.Events;

public sealed record AcademicYearActivatedDomainEvent(
    Guid AcademicYearId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate) : DomainEventBase;
