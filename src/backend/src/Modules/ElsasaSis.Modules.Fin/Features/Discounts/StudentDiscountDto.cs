using ElsasaSis.Modules.Fin.Domain.Model;

namespace ElsasaSis.Modules.Fin.Features.Discounts;

public sealed record StudentDiscountDto(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    Guid ChargeTypeId,
    string ChargeTypeName,
    Guid StudentId,
    string StudentName,
    string StudentNis,
    string DiscountType,
    decimal DiscountValue,
    string Reason,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static StudentDiscountDto FromDomain(StudentDiscount discount) =>
        new(
            discount.Id,
            discount.AcademicYearId,
            discount.AcademicYearName,
            discount.ChargeTypeId,
            discount.ChargeTypeName,
            discount.StudentId,
            discount.StudentName,
            discount.StudentNis,
            discount.DiscountType,
            discount.DiscountValue,
            discount.Reason,
            discount.IsActive,
            discount.CreatedAt,
            discount.UpdatedAt);
}
