using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Tch.Domain;

public interface ITeacherAssignmentRepository
{
    Task<TeacherAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TeacherAssignment?> GetByTupleAsync(
        Guid teacherId,
        Guid subjectId,
        Guid studyGroupId,
        Guid operationalPeriodId,
        CancellationToken cancellationToken = default);

    Task<Result> AddAsync(TeacherAssignment assignment, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(TeacherAssignment assignment, CancellationToken cancellationToken = default);
}
