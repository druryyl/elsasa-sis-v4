using ElsasaSis.Contracts.Academic;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Contracts.Teaching;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.Modules.Opr.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Features.Timetables.ConfigureStudyGroupTimetable;

public sealed class ConfigureStudyGroupTimetableHandler(
    IStudyGroupScheduleRepository scheduleRepository,
    IOperationalContract operationalContract,
    IStudentContract studentContract,
    IAcademicContract academicContract,
    ITeachingContract teachingContract,
    IRoomRepository roomRepository)
{
    public async Task<Result<StudyGroupScheduleDto>> HandleAsync(
        Guid studyGroupId,
        ConfigureStudyGroupTimetableRequest request,
        CancellationToken cancellationToken = default)
    {
        var period = await operationalContract.GetOperationalPeriodByIdAsync(request.OperationalPeriodId, cancellationToken);
        if (period is null)
        {
            return Result<StudyGroupScheduleDto>.Failure(TimetableErrors.OperationalPeriodNotFound(request.OperationalPeriodId));
        }

        if (!string.Equals(period.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            return Result<StudyGroupScheduleDto>.Failure(TimetableErrors.OperationalPeriodNotActive(request.OperationalPeriodId));
        }

        var studyGroup = await studentContract.GetStudyGroupByIdAsync(studyGroupId, cancellationToken);
        if (studyGroup is null)
        {
            return Result<StudyGroupScheduleDto>.Failure(TimetableErrors.StudyGroupNotFound(studyGroupId));
        }

        var subject = await academicContract.GetSubjectByIdAsync(request.SubjectId, cancellationToken);
        if (subject is null)
        {
            return Result<StudyGroupScheduleDto>.Failure(TimetableErrors.SubjectNotFound(request.SubjectId));
        }

        var teacher = await teachingContract.GetTeacherByIdAsync(request.TeacherId, cancellationToken);
        if (teacher is null)
        {
            return Result<StudyGroupScheduleDto>.Failure(TimetableErrors.TeacherNotFound(request.TeacherId));
        }

        string? roomName = null;
        if (request.RoomId.HasValue && request.RoomId.Value != Guid.Empty)
        {
            var room = await roomRepository.GetByIdAsync(request.RoomId.Value, cancellationToken);
            if (room is null)
            {
                return Result<StudyGroupScheduleDto>.Failure(TimetableErrors.RoomNotFound(request.RoomId.Value));
            }

            roomName = room.Name;
        }

        var normalizedScheduleType = ScheduleTypeExtensions.Parse(request.ScheduleType).Value.ToCode();

        var existing = await scheduleRepository.GetBySlotAsync(
            request.OperationalPeriodId,
            studyGroupId,
            normalizedScheduleType,
            request.DayOfWeek,
            request.PeriodSlot,
            cancellationToken);

        StudyGroupSchedule schedule;
        if (existing is not null)
        {
            var updateResult = existing.UpdateSlot(
                request.StartTime,
                request.EndTime,
                request.SubjectId,
                subject.Name,
                request.TeacherId,
                teacher.FullName,
                request.RoomId,
                roomName);

            if (updateResult.IsFailure)
            {
                return Result<StudyGroupScheduleDto>.Failure(updateResult.Error);
            }

            await scheduleRepository.UpdateAsync(existing, cancellationToken);
            schedule = existing;
        }
        else
        {
            var createResult = StudyGroupSchedule.Create(
                request.OperationalPeriodId,
                studyGroupId,
                studyGroup.Name,
                normalizedScheduleType,
                request.DayOfWeek,
                request.PeriodSlot,
                request.StartTime,
                request.EndTime,
                request.SubjectId,
                subject.Name,
                request.TeacherId,
                teacher.FullName,
                request.RoomId,
                roomName);

            if (createResult.IsFailure)
            {
                return Result<StudyGroupScheduleDto>.Failure(createResult.Error);
            }

            schedule = createResult.Value!;
            await scheduleRepository.AddAsync(schedule, cancellationToken);
        }

        return Result<StudyGroupScheduleDto>.Success(new StudyGroupScheduleDto(
            schedule.Id,
            schedule.OperationalPeriodId,
            schedule.StudyGroupId,
            schedule.StudyGroupName,
            schedule.ScheduleType,
            schedule.DayOfWeek,
            schedule.PeriodSlot,
            schedule.StartTime,
            schedule.EndTime,
            schedule.SubjectId,
            schedule.SubjectName,
            schedule.TeacherId,
            schedule.TeacherName,
            schedule.RoomId,
            schedule.RoomName,
            schedule.CreatedAt,
            schedule.UpdatedAt));
    }
}
