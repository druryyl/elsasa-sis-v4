using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Std.Domain;

public interface IStudentEnrollmentRepository
{
    Task<string> GetNextNisAsync(int year, CancellationToken cancellationToken = default);

    Task<Result<Student>> EnrollCandidateAsync(
        Candidate candidate,
        string? requestedNis,
        string? nisn,
        DateOnly? enrollmentDate,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default);

    Task<Result> EnrollAsync(
        Student student,
        Candidate candidate,
        CancellationToken cancellationToken = default);
}
