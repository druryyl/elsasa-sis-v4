using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Contracts.Student.Events;

public sealed record StudentEnrolledDomainEvent(
    Guid StudentId,
    Guid CandidateId,
    string Nis,
    string FullName) : DomainEventBase;
