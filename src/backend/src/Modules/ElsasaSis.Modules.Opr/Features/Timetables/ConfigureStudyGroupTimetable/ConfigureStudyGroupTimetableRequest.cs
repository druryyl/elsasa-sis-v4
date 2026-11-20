namespace ElsasaSis.Modules.Opr.Features.Timetables.ConfigureStudyGroupTimetable;

public sealed record ConfigureStudyGroupTimetableRequest(
    Guid OperationalPeriodId,
    string ScheduleType,
    int DayOfWeek,
    int PeriodSlot,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid SubjectId,
    Guid TeacherId,
    Guid? RoomId);
