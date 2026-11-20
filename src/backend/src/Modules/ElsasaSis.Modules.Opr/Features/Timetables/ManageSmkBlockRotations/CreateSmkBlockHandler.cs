using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.Modules.Opr.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Features.Timetables.ManageSmkBlockRotations;

public sealed class CreateSmkBlockHandler(
    IStudyGroupTimetableRepository timetableRepository,
    IOperationalContract operationalContract,
    IStudentContract studentContract)
{
    public async Task<Result<StudyGroupTimetableBlockDto>> HandleAsync(
        Guid studyGroupId,
        CreateSmkBlockRequest request,
        CancellationToken cancellationToken = default)
    {
        var period = await operationalContract.GetOperationalPeriodByIdAsync(request.OperationalPeriodId, cancellationToken);
        if (period is null)
        {
            return Result<StudyGroupTimetableBlockDto>.Failure(TimetableErrors.OperationalPeriodNotFound(request.OperationalPeriodId));
        }

        if (!string.Equals(period.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            return Result<StudyGroupTimetableBlockDto>.Failure(TimetableErrors.OperationalPeriodNotActive(request.OperationalPeriodId));
        }

        var studyGroup = await studentContract.GetStudyGroupByIdAsync(studyGroupId, cancellationToken);
        if (studyGroup is null)
        {
            return Result<StudyGroupTimetableBlockDto>.Failure(TimetableErrors.StudyGroupNotFound(studyGroupId));
        }

        var parseResult = ScheduleTypeExtensions.ParseBlockScheduleType(request.ScheduleType);
        if (parseResult.IsFailure)
        {
            return Result<StudyGroupTimetableBlockDto>.Failure(parseResult.Error);
        }

        var normalizedType = parseResult.Value.ToCode();

        var existing = await timetableRepository.GetByPeriodGroupDateAsync(
            request.OperationalPeriodId,
            studyGroupId,
            request.StartDate,
            cancellationToken);

        StudyGroupTimetable block;
        if (existing is not null)
        {
            var updateResult = existing.Update(
                normalizedType,
                request.StartDate,
                request.EndDate);

            if (updateResult.IsFailure)
            {
                return Result<StudyGroupTimetableBlockDto>.Failure(updateResult.Error);
            }

            await timetableRepository.UpdateAsync(existing, cancellationToken);
            block = existing;
        }
        else
        {
            var createResult = StudyGroupTimetable.Create(
                request.OperationalPeriodId,
                studyGroupId,
                studyGroup.Name,
                normalizedType,
                request.StartDate,
                request.EndDate);

            if (createResult.IsFailure)
            {
                return Result<StudyGroupTimetableBlockDto>.Failure(createResult.Error);
            }

            block = createResult.Value!;
            await timetableRepository.AddAsync(block, cancellationToken);
        }

        return Result<StudyGroupTimetableBlockDto>.Success(new StudyGroupTimetableBlockDto(
            block.Id,
            block.OperationalPeriodId,
            block.StudyGroupId,
            block.StudyGroupName,
            block.ScheduleType,
            block.StartDate,
            block.EndDate,
            block.CreatedAt,
            block.UpdatedAt));
    }
}
