namespace ElsasaSis.Modules.Acd.Features.Attendance.GetSubjectAttendanceSheet;

public sealed record GetSubjectAttendanceSheetQuery(
    Guid SubjectId,
    Guid StudyGroupId,
    DateOnly AttendanceDate,
    Guid OperationalPeriodId);
