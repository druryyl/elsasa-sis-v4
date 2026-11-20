namespace ElsasaSis.Modules.Acd.Features.ReportCards.GenerateDraftReportCards;

public sealed record GenerateDraftReportCardsResponse(
    int GeneratedCount,
    int SkippedCount);
