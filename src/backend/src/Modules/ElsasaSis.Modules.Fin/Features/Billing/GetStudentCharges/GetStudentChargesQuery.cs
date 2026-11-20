namespace ElsasaSis.Modules.Fin.Features.Billing.GetStudentCharges;

public sealed record GetStudentChargesQuery(
    Guid? AcademicYearId = null,
    string? BillingMonth = null,
    string? SchoolLevel = null,
    string? Grade = null,
    Guid? StudyGroupId = null,
    Guid? StudentId = null,
    string? Status = null);
