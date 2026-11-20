using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Fin.Features.FeePolicy.GetChargeTypes;

public sealed class GetChargeTypesHandler(IChargeTypeRepository chargeTypeRepository)
{
    public async Task<Result<IReadOnlyList<ChargeTypeDto>>> HandleAsync(
        GetChargeTypesQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedFrequency = string.IsNullOrWhiteSpace(query.Frequency) ? null : query.Frequency.Trim().ToUpperInvariant();
        var normalizedStatus = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim().ToUpperInvariant();

        var list = await chargeTypeRepository.GetByAcademicYearAsync(
            query.AcademicYearId,
            normalizedStatus,
            normalizedFrequency,
            cancellationToken);

        var dtos = list.Select(ChargeTypeDto.FromDomain).ToList();
        return Result<IReadOnlyList<ChargeTypeDto>>.Success(dtos);
    }
}
