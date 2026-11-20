namespace ElsasaSis.Modules.Opr.Features.Timetables;

public sealed record StudyGroupScheduleDto(
    Guid Id,
    Guid OperationalPeriodId,
    Guid StudyGroupId,
    string StudyGroupName,
    string ScheduleType,
    int DayOfWeek,
    int PeriodSlot,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    Guid? RoomId,
    string? RoomName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record StudyGroupTimetableSlotDto(
    Guid Id,
    Guid OperationalPeriodId,
    Guid StudyGroupId,
    string StudyGroupName,
    string ScheduleType,
    int DayOfWeek,
    int PeriodSlot,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    Guid? RoomId,
    string? RoomName,
    bool TeacherConflict,
    bool RoomConflict,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record StudyGroupTimetableBlockDto(
    Guid Id,
    Guid OperationalPeriodId,
    Guid StudyGroupId,
    string StudyGroupName,
    string ScheduleType,
    DateOnly StartDate,
    DateOnly EndDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record TeacherPersonalScheduleSlotDto(
    Guid Id,
    Guid OperationalPeriodId,
    Guid StudyGroupId,
    string StudyGroupName,
    string ScheduleType,
    int DayOfWeek,
    int PeriodSlot,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    Guid? RoomId,
    string? RoomName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record TeacherScheduleGroupDto(
    string ScheduleType,
    IReadOnlyList<TeacherPersonalScheduleSlotDto> Slots);

public sealed record TeacherPersonalScheduleResponse(
    Guid TeacherId,
    Guid OperationalPeriodId,
    IReadOnlyList<TeacherScheduleGroupDto> ScheduleGroups);
