using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Contracts.Teaching.Events;

public sealed record TeacherAssignedDomainEvent(
    Guid AssignmentId,
    Guid TeacherId,
    Guid SubjectId,
    Guid StudyGroupId,
    Guid OperationalPeriodId) : DomainEventBase;
