namespace ElsasaSis.Modules.Fin.Features.Discounts.GetStudentDiscounts;

public sealed record GetStudentDiscountsQuery(
    Guid? AcademicYearId = null,
    Guid? StudentId = null,
    Guid? ChargeTypeId = null,
    bool? IsActive = null);
