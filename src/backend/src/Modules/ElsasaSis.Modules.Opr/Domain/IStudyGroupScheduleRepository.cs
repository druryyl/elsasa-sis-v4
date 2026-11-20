using ElsasaSis.Modules.Opr.Domain.Model;

namespace ElsasaSis.Modules.Opr.Domain;

public interface IStudyGroupScheduleRepository
{
    Task<StudyGroupSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<StudyGroupSchedule?> GetBySlotAsync(
        Guid operationalPeriodId,
        Guid studyGroupId,
        string scheduleType,
        int dayOfWeek,
        int periodSlot,
        CancellationToken cancellationToken = default);

    Task AddAsync(StudyGroupSchedule schedule, CancellationToken cancellationToken = default);

    Task UpdateAsync(StudyGroupSchedule schedule, CancellationToken cancellationToken = default);

    Task UpsertAsync(StudyGroupSchedule schedule, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
