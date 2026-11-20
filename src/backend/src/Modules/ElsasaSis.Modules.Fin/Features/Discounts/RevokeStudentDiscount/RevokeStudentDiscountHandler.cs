using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Fin.Features.Discounts.RevokeStudentDiscount;

public sealed class RevokeStudentDiscountHandler(IStudentDiscountRepository discountRepository)
{
    public async Task<Result<StudentDiscountDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var discount = await discountRepository.GetByIdAsync(id, cancellationToken);
        if (discount is null)
        {
            return Result<StudentDiscountDto>.Failure(FinErrors.StudentDiscountNotFound(id));
        }

        var revokeResult = discount.Revoke();
        if (revokeResult.IsFailure)
        {
            return Result<StudentDiscountDto>.Failure(revokeResult.Error);
        }

        await discountRepository.UpdateAsync(discount, cancellationToken);
        return Result<StudentDiscountDto>.Success(StudentDiscountDto.FromDomain(discount));
    }
}
