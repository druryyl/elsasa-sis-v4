using ElsasaSis.Modules.Opr.Domain.Model;

namespace ElsasaSis.Modules.Opr.Domain;

public interface IStudyGroupTimetableRepository
{
    Task<StudyGroupTimetable?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<StudyGroupTimetable?> GetByPeriodGroupDateAsync(
        Guid operationalPeriodId,
        Guid studyGroupId,
        DateOnly startDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudyGroupTimetable>> GetByStudyGroupIdAsync(
        Guid studyGroupId,
        Guid operationalPeriodId,
        CancellationToken cancellationToken = default);

    Task AddAsync(StudyGroupTimetable timetable, CancellationToken cancellationToken = default);

    Task UpdateAsync(StudyGroupTimetable timetable, CancellationToken cancellationToken = default);

    Task UpsertAsync(StudyGroupTimetable timetable, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
