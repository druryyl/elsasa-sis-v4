namespace ElsasaSis.Modules.Fin.Features.Discounts.CreateStudentDiscount;

public sealed record CreateStudentDiscountRequest(
    Guid StudentId,
    Guid ChargeTypeId,
    Guid? AcademicYearId = null,
    string? AcademicYearName = null,
    string? StudentName = null,
    string? StudentNis = null,
    string DiscountType = "",
    decimal DiscountValue = 0m,
    string Reason = "");
