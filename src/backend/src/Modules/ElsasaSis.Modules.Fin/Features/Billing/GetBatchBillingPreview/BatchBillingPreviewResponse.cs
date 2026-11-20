namespace ElsasaSis.Modules.Fin.Features.Billing.GetBatchBillingPreview;

public sealed record BatchBillingPreviewResponse(
    int StudentCount,
    int ChargeCount,
    int SkippedCount,
    decimal TotalGrossAmount,
    decimal TotalDiscountAmount,
    decimal TotalNetAmount,
    IReadOnlyList<BatchBillingPreviewItemDto> Items);

public sealed record BatchBillingPreviewItemDto(
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid? StudyGroupId,
    string? StudyGroupName,
    string SchoolLevel,
    string Grade,
    Guid ChargeTypeId,
    string ChargeTypeName,
    decimal GrossAmount,
    decimal DiscountAmount,
    string? DiscountReason,
    decimal NetAmount,
    bool IsAlreadyBilled);
