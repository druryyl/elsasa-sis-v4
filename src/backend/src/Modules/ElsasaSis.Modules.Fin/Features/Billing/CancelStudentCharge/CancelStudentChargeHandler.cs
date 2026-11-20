using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Fin.Features.Billing.CancelStudentCharge;

public sealed class CancelStudentChargeHandler(IStudentChargeRepository studentChargeRepository)
{
    public async Task<Result<StudentChargeDto>> HandleAsync(
        Guid id,
        CancelStudentChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        var charge = await studentChargeRepository.GetByIdAsync(id, cancellationToken);
        if (charge is null)
        {
            return Result<StudentChargeDto>.Failure(FinErrors.StudentChargeNotFound(id));
        }

        var cancelResult = charge.Cancel(request.CancellationReason, DateTime.UtcNow);
        if (cancelResult.IsFailure)
        {
            return Result<StudentChargeDto>.Failure(cancelResult.Error);
        }

        await studentChargeRepository.UpdateAsync(charge, cancellationToken);
        return Result<StudentChargeDto>.Success(StudentChargeDto.FromDomain(charge));
    }
}
