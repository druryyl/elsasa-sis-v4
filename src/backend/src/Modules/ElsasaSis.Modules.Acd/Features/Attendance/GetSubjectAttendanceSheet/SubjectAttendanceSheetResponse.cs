namespace ElsasaSis.Modules.Acd.Features.Attendance.GetSubjectAttendanceSheet;

public sealed record AttendanceSheetItemDto(
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid? AttendanceRecordId,
    string? Status,
    string? Notes,
    Guid? RecordedByTeacherId,
    string? RecordedByTeacherName,
    DateTime? UpdatedAt);

public sealed record SubjectAttendanceSheetResponse(
    Guid SubjectId,
    Guid StudyGroupId,
    DateOnly AttendanceDate,
    Guid OperationalPeriodId,
    IReadOnlyList<AttendanceSheetItemDto> Items);
