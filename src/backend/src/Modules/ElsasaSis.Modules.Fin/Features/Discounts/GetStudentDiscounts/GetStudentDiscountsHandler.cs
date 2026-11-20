using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Fin.Features.Discounts.GetStudentDiscounts;

public sealed class GetStudentDiscountsHandler(IStudentDiscountRepository discountRepository)
{
    public async Task<Result<IReadOnlyList<StudentDiscountDto>>> HandleAsync(
        GetStudentDiscountsQuery query,
        CancellationToken cancellationToken = default)
    {
        var discounts = await discountRepository.GetDiscountsAsync(
            query.AcademicYearId,
            query.StudentId,
            query.ChargeTypeId,
            query.IsActive,
            cancellationToken);

        var dtos = discounts.Select(StudentDiscountDto.FromDomain).ToList();
        return Result<IReadOnlyList<StudentDiscountDto>>.Success(dtos);
    }
}
