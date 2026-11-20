namespace ElsasaSis.Modules.Acd.Features.ReportCards.GetStudentReportCards;

public sealed record StudentReportCardSummaryDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string AcademicYearName,
    string OperationalPeriodName,
    string? StudyGroupName,
    string Status,
    DateTime? FinalizedAt);
