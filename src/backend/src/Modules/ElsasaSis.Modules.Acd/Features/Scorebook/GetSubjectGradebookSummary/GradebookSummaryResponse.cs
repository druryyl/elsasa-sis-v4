namespace ElsasaSis.Modules.Acd.Features.Scorebook.GetSubjectGradebookSummary;

public sealed record GradebookSummaryResponse(
    Guid SubjectStageId,
    string SubjectStageName,
    Guid SubjectId,
    string SubjectName,
    Guid StudyGroupId,
    string StudyGroupName,
    Guid OperationalPeriodId,
    string OperationalPeriodName,
    IReadOnlyList<GradebookAssignmentDto> Assignments,
    IReadOnlyList<GradebookStudentRowDto> Students);

public sealed record GradebookAssignmentDto(
    Guid Id,
    string Title,
    string AssignmentType,
    decimal MaxScore,
    decimal? Weight,
    DateOnly AssignedDate,
    DateOnly? DueDate,
    Guid? LearningUnitId,
    string? LearningUnitName);

public sealed record GradebookStudentRowDto(
    Guid StudentId,
    string StudentName,
    string StudentNis,
    decimal? RunningScore,
    GradebookAttendanceMetricsDto Attendance,
    IReadOnlyList<GradebookStudentScoreDto> Scores);

public sealed record GradebookStudentScoreDto(
    Guid AssignmentId,
    decimal Score,
    string? Feedback,
    DateTime GradedAt);

public sealed record GradebookAttendanceMetricsDto(
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int ExcusedCount,
    int SickCount,
    int TotalRecorded);
