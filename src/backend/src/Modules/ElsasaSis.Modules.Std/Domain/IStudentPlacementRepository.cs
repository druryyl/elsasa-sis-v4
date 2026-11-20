using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Std.Domain;

public interface IStudentPlacementRepository
{
    Task<StudentPlacement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StudentPlacement?> GetByStudentAndAcademicYearAsync(Guid studentId, Guid academicYearId, CancellationToken cancellationToken = default);
    Task<Result> UpsertAsync(StudentPlacement placement, CancellationToken cancellationToken = default);
    Task MarkAcademicallyCompletedAsync(Guid studentId, Guid operationalPeriodId, CancellationToken cancellationToken = default);
}
