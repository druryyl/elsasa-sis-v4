namespace ElsasaSis.Modules.Acd.Features.ReportCards.ReviewAllReportCards;

public sealed record ReportCardOverviewItemDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid? StudyGroupId,
    string? StudyGroupName,
    Guid? HomeroomTeacherId,
    string? HomeroomTeacherName,
    string Status,
    DateTime? FinalizedAt,
    decimal AverageScore,
    int TotalSubjects,
    int CompletedNotesCount,
    bool IsHomeroomRemarksCompleted);

public sealed record ReportCardsOverviewDto(
    int TotalReportCards,
    int DraftCount,
    int FinalizedCount,
    decimal SchoolAverageScore,
    IReadOnlyList<ReportCardOverviewItemDto> Items);
