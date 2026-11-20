using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Domain;

public sealed record StudentPlacementRosterDto(
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid StudyGroupId,
    string StudyGroupName,
    Guid AcademicYearId,
    string AcademicYearName);

public interface IAcademicParticipationRepository
{
    Task<AcademicParticipation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AcademicParticipation?> GetByStudentStagePeriodAsync(
        Guid studentId,
        Guid subjectStageId,
        Guid operationalPeriodId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcademicParticipation>> GetByCriteriaAsync(
        Guid? subjectStageId,
        Guid? studyGroupId,
        Guid? operationalPeriodId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Guid>> GetEnrolledStudentIdsAsync(
        Guid subjectStageId,
        Guid operationalPeriodId,
        IEnumerable<Guid> studentIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentPlacementRosterDto>> GetActivePlacementsByStudyGroupAsync(
        Guid studyGroupId,
        Guid academicYearId,
        CancellationToken cancellationToken = default);

    Task<Result> AddAsync(AcademicParticipation participation, CancellationToken cancellationToken = default);

    Task<int> AddBatchAsync(IEnumerable<AcademicParticipation> participations, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(AcademicParticipation participation, CancellationToken cancellationToken = default);
}
