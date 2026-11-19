using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Contracts.Student.Events;

public sealed record StudentPlacedDomainEvent(
    Guid PlacementId,
    Guid StudentId,
    Guid AcademicYearId,
    Guid? StudyGroupId) : DomainEventBase;
