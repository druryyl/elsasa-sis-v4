using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Features.Timetables.ManageSmkBlockRotations;

public sealed class GetSmkBlocksHandler(IStudyGroupTimetableRepository timetableRepository)
{
    public async Task<Result<IReadOnlyList<StudyGroupTimetableBlockDto>>> HandleAsync(
        Guid studyGroupId,
        Guid periodId,
        CancellationToken cancellationToken = default)
    {
        var blocks = await timetableRepository.GetByStudyGroupIdAsync(studyGroupId, periodId, cancellationToken);

        var dtos = blocks.Select(b => new StudyGroupTimetableBlockDto(
            b.Id,
            b.OperationalPeriodId,
            b.StudyGroupId,
            b.StudyGroupName,
            b.ScheduleType,
            b.StartDate,
            b.EndDate,
            b.CreatedAt,
            b.UpdatedAt)).ToList();

        return Result<IReadOnlyList<StudyGroupTimetableBlockDto>>.Success(dtos);
    }
}
