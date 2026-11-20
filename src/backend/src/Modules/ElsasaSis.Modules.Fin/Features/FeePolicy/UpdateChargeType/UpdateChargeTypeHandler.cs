using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Fin.Features.FeePolicy.UpdateChargeType;

public sealed class UpdateChargeTypeHandler(
    IChargeTypeRepository chargeTypeRepository,
    IStudentContract studentContract)
{
    public async Task<Result<ChargeTypeDto>> HandleAsync(
        Guid id,
        UpdateChargeTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var chargeType = await chargeTypeRepository.GetByIdAsync(id, cancellationToken);
        if (chargeType is null)
        {
            return Result<ChargeTypeDto>.Failure(FinErrors.ChargeTypeNotFound(id));
        }

        if (chargeType.Status == ChargeTypeStatuses.Active)
        {
            return Result<ChargeTypeDto>.Failure(FinErrors.ChargeTypeCannotBeModified(id));
        }

        var exists = await chargeTypeRepository.ExistsByNameInYearAsync(
            request.Name.Trim(),
            chargeType.AcademicYearId,
            excludeId: id,
            cancellationToken);

        if (exists)
        {
            return Result<ChargeTypeDto>.Failure(FinErrors.DuplicateChargeTypeName(request.Name.Trim(), chargeType.AcademicYearName));
        }

        string? studyGroupName = request.StudyGroupName?.Trim();
        if (request.StudyGroupId.HasValue && request.StudyGroupId.Value != Guid.Empty && string.IsNullOrWhiteSpace(studyGroupName))
        {
            var sg = await studentContract.GetStudyGroupByIdAsync(request.StudyGroupId.Value, cancellationToken);
            if (sg is not null)
            {
                studyGroupName = sg.Name;
            }
        }

        var updateResult = chargeType.Update(
            request.Name,
            request.Frequency,
            request.Amount,
            request.SchoolLevel,
            request.Grade,
            request.StudyGroupId,
            studyGroupName);

        if (updateResult.IsFailure)
        {
            return Result<ChargeTypeDto>.Failure(updateResult.Error);
        }

        await chargeTypeRepository.UpdateAsync(chargeType, cancellationToken);
        return Result<ChargeTypeDto>.Success(ChargeTypeDto.FromDomain(chargeType));
    }
}
