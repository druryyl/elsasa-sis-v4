using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Fin.Features.FeePolicy.ActivateChargeType;

public sealed class ActivateChargeTypeHandler(IChargeTypeRepository chargeTypeRepository)
{
    public async Task<Result<ChargeTypeDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var chargeType = await chargeTypeRepository.GetByIdAsync(id, cancellationToken);
        if (chargeType is null)
        {
            return Result<ChargeTypeDto>.Failure(FinErrors.ChargeTypeNotFound(id));
        }

        var activateResult = chargeType.Activate();
        if (activateResult.IsFailure)
        {
            return Result<ChargeTypeDto>.Failure(activateResult.Error);
        }

        await chargeTypeRepository.UpdateAsync(chargeType, cancellationToken);
        return Result<ChargeTypeDto>.Success(ChargeTypeDto.FromDomain(chargeType));
    }
}
