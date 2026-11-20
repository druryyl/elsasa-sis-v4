namespace ElsasaSis.Modules.Fin.Features.Reports.ReceivablesSummary;

public sealed record ReceivablesSummaryDto(
    decimal TotalBilled,
    decimal TotalCollected,
    decimal TotalOutstanding,
    int TotalStudentsCount,
    int TotalChargesCount,
    IReadOnlyList<ReceivablesSummaryByLevelDto> Breakdown);

public sealed record ReceivablesSummaryByLevelDto(
    Guid AcademicYearId,
    string AcademicYearName,
    string SchoolLevel,
    decimal TotalBilled,
    decimal TotalCollected,
    decimal TotalOutstanding,
    int StudentCount,
    int ChargeCount);
