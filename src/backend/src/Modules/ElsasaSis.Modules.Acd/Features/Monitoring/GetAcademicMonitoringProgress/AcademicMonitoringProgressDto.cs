namespace ElsasaSis.Modules.Acd.Features.Monitoring.GetAcademicMonitoringProgress;

public sealed record AcademicMonitoringProgressDto(
    AcademicMonitoringOverviewDto Overview,
    AcademicMonitoringExceptionsDto Exceptions,
    IReadOnlyList<StudyGroupMonitoringRowDto> StudyGroups);

public sealed record AcademicMonitoringOverviewDto(
    decimal AttendanceRateTodayPercent,
    int TotalEnrolledStudentsCount,
    int PresentCount,
    int AbsentCount,
    int SickCount,
    int LateCount,
    int ExcusedCount,
    decimal GradingCompletenessPercent,
    int TotalAssignmentsCount,
    int ExpectedScoresCount,
    int ActualScoresCount,
    decimal ReportCardsFinalizedPercent,
    int TotalPlacedStudentsCount,
    int DraftReportCardsCount,
    int FinalizedReportCardsCount);

public sealed record AcademicMonitoringExceptionsDto(
    IReadOnlyList<MissingAttendanceAlertDto> MissingAttendanceStudyGroups,
    IReadOnlyList<OverdueGradingAlertDto> OverdueGradingAssignments,
    IReadOnlyList<PendingReportCardAlertDto> PendingReportCards);

public sealed record MissingAttendanceAlertDto(
    Guid StudyGroupId,
    string StudyGroupName,
    string SchoolLevel,
    string Grade,
    string? HomeroomTeacherName);

public sealed record OverdueGradingAlertDto(
    Guid AssignmentId,
    string Title,
    string SubjectName,
    string? TeacherName,
    string StudyGroupName,
    int DaysOverdue,
    int MissingScoresCount);

public sealed record PendingReportCardAlertDto(
    Guid StudyGroupId,
    string StudyGroupName,
    string SchoolLevel,
    string Grade,
    string? HomeroomTeacherName,
    int DraftCount,
    int TotalCount);

public sealed record StudyGroupMonitoringRowDto(
    Guid StudyGroupId,
    string StudyGroupName,
    string SchoolLevel,
    string Grade,
    string? HomeroomTeacherName,
    decimal AttendanceTodayPercent,
    int AssignmentsCount,
    decimal GradingCompletenessPercent,
    string ReportCardsFinalizedRatio,
    bool IsAttendanceMissingToday,
    bool HasOverdueGrading,
    bool HasPendingReportCards);
