namespace ElsasaSis.Modules.Fin.Features.Billing.GetBatchBillingPreview;

public sealed record BatchBillingPreviewRequest(
    Guid AcademicYearId,
    string BillingMonth,
    Guid? StudyGroupId = null,
    string? SchoolLevel = null,
    string? Grade = null);
