namespace ElsasaSis.Modules.Acd.Features.Attendance.RecordSubjectAttendance;

public sealed record RecordAttendanceItemRequest(
    Guid StudentId,
    string Status,
    string? Notes);

public sealed record RecordAttendanceRequest(
    Guid SubjectId,
    Guid StudyGroupId,
    DateOnly AttendanceDate,
    Guid OperationalPeriodId,
    IReadOnlyList<RecordAttendanceItemRequest> Items);
