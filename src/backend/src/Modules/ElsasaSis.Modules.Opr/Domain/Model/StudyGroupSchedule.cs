using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Opr.Domain.Model;

public sealed class StudyGroupSchedule : AggregateRoot<Guid>
{
    public Guid OperationalPeriodId { get; private set; }
    public Guid StudyGroupId { get; private set; }
    public string StudyGroupName { get; private set; } = string.Empty;
    public string ScheduleType { get; private set; } = string.Empty;
    public int DayOfWeek { get; private set; }
    public int PeriodSlot { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public Guid SubjectId { get; private set; }
    public string SubjectName { get; private set; } = string.Empty;
    public Guid TeacherId { get; private set; }
    public string TeacherName { get; private set; } = string.Empty;
    public Guid? RoomId { get; private set; }
    public string? RoomName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private StudyGroupSchedule()
    {
    }

    private StudyGroupSchedule(
        Guid id,
        Guid operationalPeriodId,
        Guid studyGroupId,
        string studyGroupName,
        string scheduleType,
        int dayOfWeek,
        int periodSlot,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid subjectId,
        string subjectName,
        Guid teacherId,
        string teacherName,
        Guid? roomId,
        string? roomName,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        OperationalPeriodId = operationalPeriodId;
        StudyGroupId = studyGroupId;
        StudyGroupName = studyGroupName;
        ScheduleType = scheduleType;
        DayOfWeek = dayOfWeek;
        PeriodSlot = periodSlot;
        StartTime = startTime;
        EndTime = endTime;
        SubjectId = subjectId;
        SubjectName = subjectName;
        TeacherId = teacherId;
        TeacherName = teacherName;
        RoomId = roomId;
        RoomName = roomName;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<StudyGroupSchedule> Create(
        Guid operationalPeriodId,
        Guid studyGroupId,
        string studyGroupName,
        string scheduleType,
        int dayOfWeek,
        int periodSlot,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid subjectId,
        string subjectName,
        Guid teacherId,
        string teacherName,
        Guid? roomId = null,
        string? roomName = null,
        DateTime? utcNow = null)
    {
        var validation = Validate(
            operationalPeriodId,
            studyGroupId,
            studyGroupName,
            scheduleType,
            dayOfWeek,
            periodSlot,
            startTime,
            endTime,
            subjectId,
            subjectName,
            teacherId,
            teacherName);

        if (validation.IsFailure)
        {
            return Result<StudyGroupSchedule>.Failure(validation.Error);
        }

        var normalizedType = ScheduleTypeExtensions.Parse(scheduleType).Value.ToCode();
        var now = utcNow ?? DateTime.UtcNow;

        var schedule = new StudyGroupSchedule(
            UuidV7.NewGuid(),
            operationalPeriodId,
            studyGroupId,
            studyGroupName.Trim(),
            normalizedType,
            dayOfWeek,
            periodSlot,
            startTime,
            endTime,
            subjectId,
            subjectName.Trim(),
            teacherId,
            teacherName.Trim(),
            roomId,
            string.IsNullOrWhiteSpace(roomName) ? null : roomName.Trim(),
            now,
            now);

        return Result<StudyGroupSchedule>.Success(schedule);
    }

    public Result UpdateSlot(
        TimeOnly startTime,
        TimeOnly endTime,
        Guid subjectId,
        string subjectName,
        Guid teacherId,
        string teacherName,
        Guid? roomId = null,
        string? roomName = null,
        DateTime? utcNow = null)
    {
        if (startTime >= endTime)
        {
            return Result.Failure(TimetableErrors.InvalidTimeRange());
        }

        if (subjectId == Guid.Empty)
        {
            return Result.Failure(TimetableErrors.FieldRequired(nameof(subjectId)));
        }

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            return Result.Failure(TimetableErrors.FieldRequired(nameof(subjectName)));
        }

        if (teacherId == Guid.Empty)
        {
            return Result.Failure(TimetableErrors.FieldRequired(nameof(teacherId)));
        }

        if (string.IsNullOrWhiteSpace(teacherName))
        {
            return Result.Failure(TimetableErrors.FieldRequired(nameof(teacherName)));
        }

        StartTime = startTime;
        EndTime = endTime;
        SubjectId = subjectId;
        SubjectName = subjectName.Trim();
        TeacherId = teacherId;
        TeacherName = teacherName.Trim();
        RoomId = roomId;
        RoomName = string.IsNullOrWhiteSpace(roomName) ? null : roomName.Trim();
        UpdatedAt = utcNow ?? DateTime.UtcNow;

        return Result.Success();
    }

    public Result AssignRoom(Guid? roomId, string? roomName, DateTime? utcNow = null)
    {
        RoomId = roomId;
        RoomName = string.IsNullOrWhiteSpace(roomName) ? null : roomName.Trim();
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public static StudyGroupSchedule Rehydrate(
        Guid id,
        Guid operationalPeriodId,
        Guid studyGroupId,
        string studyGroupName,
        string scheduleType,
        int dayOfWeek,
        int periodSlot,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid subjectId,
        string subjectName,
        Guid teacherId,
        string teacherName,
        Guid? roomId,
        string? roomName,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new StudyGroupSchedule(
            id,
            operationalPeriodId,
            studyGroupId,
            studyGroupName,
            scheduleType,
            dayOfWeek,
            periodSlot,
            startTime,
            endTime,
            subjectId,
            subjectName,
            teacherId,
            teacherName,
            roomId,
            roomName,
            createdAt,
            updatedAt);
    }

    private static Result Validate(
        Guid operationalPeriodId,
        Guid studyGroupId,
        string studyGroupName,
        string scheduleType,
        int dayOfWeek,
        int periodSlot,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid subjectId,
        string subjectName,
        Guid teacherId,
        string teacherName)
    {
        if (operationalPeriodId == Guid.Empty)
        {
            return Result.Failure(TimetableErrors.FieldRequired(nameof(operationalPeriodId)));
        }

        if (studyGroupId == Guid.Empty)
        {
            return Result.Failure(TimetableErrors.FieldRequired(nameof(studyGroupId)));
        }

        if (string.IsNullOrWhiteSpace(studyGroupName))
        {
            return Result.Failure(TimetableErrors.FieldRequired(nameof(studyGroupName)));
        }

        var parseTypeResult = ScheduleTypeExtensions.Parse(scheduleType);
        if (parseTypeResult.IsFailure)
        {
            return Result.Failure(parseTypeResult.Error);
        }

        if (dayOfWeek < 1 || dayOfWeek > 7)
        {
            return Result.Failure(TimetableErrors.InvalidDayOfWeek(dayOfWeek));
        }

        if (periodSlot < 1)
        {
            return Result.Failure(TimetableErrors.InvalidPeriodSlot(periodSlot));
        }

        if (startTime >= endTime)
        {
            return Result.Failure(TimetableErrors.InvalidTimeRange());
        }

        if (subjectId == Guid.Empty)
        {
            return Result.Failure(TimetableErrors.FieldRequired(nameof(subjectId)));
        }

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            return Result.Failure(TimetableErrors.FieldRequired(nameof(subjectName)));
        }

        if (teacherId == Guid.Empty)
        {
            return Result.Failure(TimetableErrors.FieldRequired(nameof(teacherId)));
        }

        if (string.IsNullOrWhiteSpace(teacherName))
        {
            return Result.Failure(TimetableErrors.FieldRequired(nameof(teacherName)));
        }

        return Result.Success();
    }
}
