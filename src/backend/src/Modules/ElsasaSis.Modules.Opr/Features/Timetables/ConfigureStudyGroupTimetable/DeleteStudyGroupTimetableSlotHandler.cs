using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Features.Timetables.ConfigureStudyGroupTimetable;

public sealed class DeleteStudyGroupTimetableSlotHandler(IStudyGroupScheduleRepository scheduleRepository)
{
    public async Task<Result> HandleAsync(
        Guid studyGroupId,
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await scheduleRepository.GetByIdAsync(scheduleId, cancellationToken);
        if (schedule is null || schedule.StudyGroupId != studyGroupId)
        {
            return Result.Failure(TimetableErrors.ScheduleNotFound(scheduleId));
        }

        await scheduleRepository.DeleteAsync(scheduleId, cancellationToken);
        return Result.Success();
    }
}
