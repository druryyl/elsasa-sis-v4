namespace ElsasaSis.Modules.Acd.Features.ReportCards.GenerateDraftReportCards;

public sealed record GenerateDraftReportCardsRequest(
    Guid StudyGroupId,
    Guid OperationalPeriodId);
