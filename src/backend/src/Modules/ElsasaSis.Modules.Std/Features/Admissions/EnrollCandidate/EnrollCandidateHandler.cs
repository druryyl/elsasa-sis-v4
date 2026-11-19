using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Std.Features.Admissions.EnrollCandidate;

public sealed class EnrollCandidateHandler(
    ICandidateRepository candidateRepository,
    IStudentEnrollmentRepository enrollmentRepository,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<EnrolledStudentResponse>> HandleAsync(
        Guid id,
        EnrollCandidateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nis))
        {
            return Result<EnrolledStudentResponse>.Failure(StdErrors.NisRequired());
        }

        var candidate = await candidateRepository.GetByIdAsync(id, cancellationToken);
        if (candidate is null)
        {
            return Result<EnrolledStudentResponse>.Failure(StdErrors.CandidateNotFound(id));
        }

        var enrollResult = await enrollmentRepository.EnrollCandidateAsync(
            candidate,
            request.Nis,
            request.Nisn,
            request.EnrollmentDate,
            cancellationToken: cancellationToken);

        if (enrollResult.IsFailure)
        {
            return Result<EnrolledStudentResponse>.Failure(enrollResult.Error);
        }

        var student = enrollResult.Value!;

        if (student.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(student.DomainEvents, cancellationToken);
            student.ClearDomainEvents();
        }

        return Result<EnrolledStudentResponse>.Success(student.ToEnrolledResponse());
    }
}
