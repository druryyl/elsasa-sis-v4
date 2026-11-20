namespace ElsasaSis.Modules.Fin.Features.Billing.GenerateBatchTuitionCharges;

public sealed record GenerateBatchTuitionChargesRequest(
    Guid AcademicYearId,
    string BillingMonth,
    Guid? StudyGroupId = null,
    string? SchoolLevel = null,
    string? Grade = null,
    DateOnly? DueDate = null);
