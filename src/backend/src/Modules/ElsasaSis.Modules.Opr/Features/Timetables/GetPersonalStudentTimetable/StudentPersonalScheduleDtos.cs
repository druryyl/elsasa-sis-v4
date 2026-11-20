namespace ElsasaSis.Modules.Opr.Features.Timetables.GetPersonalStudentTimetable;

public sealed record StudentPersonalScheduleResponse(
    StudentTimetableInfoDto StudentInfo,
    ActiveSmkCycleDto ActiveCycle,
    IReadOnlyList<ScheduleSlotDto> TodayAgenda,
    IReadOnlyList<ScheduleSlotDto> WeeklySchedule);

public sealed record StudentTimetableInfoDto(
    Guid StudentId,
    string FullName,
    string Nis,
    Guid? StudyGroupId,
    string? StudyGroupName,
    string? SchoolLevel,
    string? Grade);

public sealed record ActiveSmkCycleDto(
    string ScheduleType,
    bool IsSmkRotation,
    DateOnly? StartDate,
    DateOnly? EndDate);

public sealed record ScheduleSlotDto(
    Guid Id,
    string DayOfWeek,
    int PeriodSlot,
    string StartTime,
    string EndTime,
    Guid SubjectId,
    string SubjectName,
    string SubjectCode,
    Guid TeacherId,
    string TeacherName,
    Guid? RoomId,
    string? RoomName,
    string ScheduleType);
