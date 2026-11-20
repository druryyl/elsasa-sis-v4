namespace ElsasaSis.Modules.Acd.Features.ReportCards.GetHomeroomReportCards;

public sealed record HomeroomReportCardDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string StudentNis,
    string? StudentNisn,
    Guid? StudyGroupId,
    string? StudyGroupName,
    Guid? HomeroomTeacherId,
    string? HomeroomTeacherName,
    string Status,
    DateTime? FinalizedAt,
    string? OverrideReason,
    string? GeneralRemarks,
    int TotalSubjects,
    int CompletedNotesCount,
    bool IsHomeroomRemarksCompleted,
    decimal AverageScore,
    DateTime CreatedAt,
    DateTime UpdatedAt);
