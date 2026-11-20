using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Fin.Features.FeePolicy.CreateChargeType;

public sealed class CreateChargeTypeHandler(
    IChargeTypeRepository chargeTypeRepository,
    IOperationalContract operationalContract,
    IStudentContract studentContract)
{
    public async Task<Result<ChargeTypeDto>> HandleAsync(
        CreateChargeTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        Guid academicYearId;
        string academicYearName;

        if (request.AcademicYearId.HasValue && request.AcademicYearId.Value != Guid.Empty)
        {
            academicYearId = request.AcademicYearId.Value;
            if (!string.IsNullOrWhiteSpace(request.AcademicYearName))
            {
                academicYearName = request.AcademicYearName.Trim();
            }
            else
            {
                var year = await operationalContract.GetAcademicYearByIdAsync(academicYearId, cancellationToken);
                if (year is null)
                {
                    return Result<ChargeTypeDto>.Failure(DomainError.NotFound("Fin.AcademicYearNotFound", $"Academic year '{academicYearId}' was not found."));
                }
                academicYearName = year.Name;
            }
        }
        else
        {
            var activeYear = await operationalContract.GetActiveAcademicYearAsync(cancellationToken);
            if (activeYear is null)
            {
                return Result<ChargeTypeDto>.Failure(DomainError.NotFound("Fin.NoActiveAcademicYear", "No active academic year found."));
            }
            academicYearId = activeYear.Id;
            academicYearName = activeYear.Name;
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

        var exists = await chargeTypeRepository.ExistsByNameInYearAsync(
            request.Name.Trim(),
            academicYearId,
            null,
            cancellationToken);

        if (exists)
        {
            return Result<ChargeTypeDto>.Failure(FinErrors.DuplicateChargeTypeName(request.Name.Trim(), academicYearName));
        }

        var chargeTypeResult = ChargeType.Create(
            academicYearId,
            academicYearName,
            request.Name,
            request.Frequency,
            request.Amount,
            request.SchoolLevel,
            request.Grade,
            request.StudyGroupId,
            studyGroupName);

        if (chargeTypeResult.IsFailure)
        {
            return Result<ChargeTypeDto>.Failure(chargeTypeResult.Error);
        }

        var chargeType = chargeTypeResult.Value!;
        await chargeTypeRepository.InsertAsync(chargeType, cancellationToken);

        return Result<ChargeTypeDto>.Success(ChargeTypeDto.FromDomain(chargeType));
    }
}
