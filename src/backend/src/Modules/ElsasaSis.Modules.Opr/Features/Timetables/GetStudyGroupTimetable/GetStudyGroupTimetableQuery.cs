namespace ElsasaSis.Modules.Opr.Features.Timetables.GetStudyGroupTimetable;

public sealed record GetStudyGroupTimetableQuery(
    Guid StudyGroupId,
    Guid PeriodId,
    string? ScheduleType = null);
