namespace ElsasaSis.Modules.Acd.Features.ReportCards.GetReportCardById;

public sealed record AttendanceSummaryDto(
    int PresentCount,
    int AbsentCount,
    int SickCount,
    int LateCount,
    int ExcusedCount,
    int TotalRecordedDays);

public sealed record ReportCardEntryDetailDto(
    Guid Id,
    Guid SubjectId,
    string SubjectName,
    string? SubjectCode,
    Guid SubjectStageId,
    string SubjectStageName,
    Guid? TeacherId,
    string? TeacherName,
    decimal NumericScore,
    string? LetterGrade,
    string? TeacherNotes);

public sealed record ReportCardDetailDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string StudentNis,
    string? StudentNisn,
    Guid AcademicYearId,
    string AcademicYearName,
    Guid OperationalPeriodId,
    string OperationalPeriodName,
    Guid? StudyGroupId,
    string? StudyGroupName,
    Guid? HomeroomTeacherId,
    string? HomeroomTeacherName,
    string Status,
    string? OverrideReason,
    string? GeneralRemarks,
    DateTime? FinalizedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    decimal AverageScore,
    int TotalSubjects,
    AttendanceSummaryDto Attendance,
    IReadOnlyList<ReportCardEntryDetailDto> Entries);
