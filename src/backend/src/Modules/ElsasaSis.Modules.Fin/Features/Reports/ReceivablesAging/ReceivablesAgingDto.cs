namespace ElsasaSis.Modules.Fin.Features.Reports.ReceivablesAging;

public sealed record ReceivablesAgingDto(
    ReceivablesAgingSummaryDto Summary,
    IReadOnlyList<ReceivablesAgingStudentItemDto> Students);

public sealed record ReceivablesAgingSummaryDto(
    decimal CurrentAmount,
    decimal Days1To30Amount,
    decimal Days31To60Amount,
    decimal Days61To90Amount,
    decimal Over90DaysAmount,
    decimal TotalOutstanding,
    decimal ArrearsOver30Days,
    int TotalStudentsCount,
    int TotalChargesCount);

public sealed record ReceivablesAgingStudentItemDto(
    Guid StudentId,
    string StudentName,
    string StudentNis,
    string SchoolLevel,
    string Grade,
    Guid? StudyGroupId,
    string? StudyGroupName,
    decimal CurrentAmount,
    decimal Days1To30Amount,
    decimal Days31To60Amount,
    decimal Days61To90Amount,
    decimal Over90DaysAmount,
    decimal TotalOutstanding,
    int UnpaidChargeCount);
