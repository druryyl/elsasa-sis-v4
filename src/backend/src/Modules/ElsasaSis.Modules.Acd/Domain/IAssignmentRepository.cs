using ElsasaSis.Modules.Acd.Domain.Model;

namespace ElsasaSis.Modules.Acd.Domain;

public sealed record TeacherAssignmentSnapshotDto(
    Guid TeacherId,
    string TeacherName,
    Guid SubjectId,
    string SubjectName,
    Guid StudyGroupId,
    string StudyGroupName,
    Guid OperationalPeriodId,
    string OperationalPeriodName);

public sealed record ActiveParticipationSnapshotDto(
    Guid StudentId,
    string StudentName,
    string StudentNis);

public interface IAssignmentRepository
{
    Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Assignment assignment, CancellationToken cancellationToken = default);

    Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken = default);

    Task UpsertScoresAsync(Guid assignmentId, IReadOnlyList<AssignmentScore> scores, CancellationToken cancellationToken = default);

    Task<bool> HasTeacherAssignmentAsync(
        Guid teacherId,
        Guid studyGroupId,
        Guid subjectId,
        Guid operationalPeriodId,
        CancellationToken cancellationToken = default);

    Task<TeacherAssignmentSnapshotDto?> GetActiveTeacherAssignmentAsync(
        Guid studyGroupId,
        Guid subjectId,
        Guid operationalPeriodId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActiveParticipationSnapshotDto>> GetActiveParticipationsAsync(
        Guid subjectStageId,
        Guid studyGroupId,
        Guid operationalPeriodId,
        CancellationToken cancellationToken = default);
}
