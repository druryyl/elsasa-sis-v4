using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Fin.Features.Billing.CreateAdHocCharge;

public sealed class CreateAdHocChargeHandler(
    IOperationalContract operationalContract,
    IStudentContract studentContract,
    IChargeTypeRepository chargeTypeRepository,
    IStudentDiscountRepository studentDiscountRepository,
    IStudentChargeRepository studentChargeRepository)
{
    public async Task<Result<StudentChargeDto>> HandleAsync(
        CreateAdHocChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        string academicYearName;
        var academicYear = await operationalContract.GetAcademicYearByIdAsync(request.AcademicYearId, cancellationToken);
        if (academicYear is not null)
        {
            academicYearName = academicYear.Name;
        }
        else
        {
            var activeYear = await operationalContract.GetActiveAcademicYearAsync(cancellationToken);
            if (activeYear is not null && activeYear.Id == request.AcademicYearId)
            {
                academicYearName = activeYear.Name;
            }
            else
            {
                return Result<StudentChargeDto>.Failure(
                    DomainError.NotFound("Fin.AcademicYear.NotFound", $"Academic year with ID '{request.AcademicYearId}' was not found."));
            }
        }

        var chargeType = await chargeTypeRepository.GetByIdAsync(request.ChargeTypeId, cancellationToken);
        if (chargeType is null)
        {
            return Result<StudentChargeDto>.Failure(FinErrors.ChargeTypeNotFound(request.ChargeTypeId));
        }

        var students = await studentContract.GetActiveStudentsByScopeAsync(null, null, null, cancellationToken);
        var student = students.FirstOrDefault(s => s.StudentId == request.StudentId);
        if (student is null)
        {
            return Result<StudentChargeDto>.Failure(FinErrors.StudentNotFound(request.StudentId));
        }

        var activePeriod = await operationalContract.GetActiveOperationalPeriodAsync(cancellationToken);

        var discount = await studentDiscountRepository.GetActiveDiscountAsync(
            request.StudentId,
            request.ChargeTypeId,
            request.AcademicYearId,
            cancellationToken);

        var grossAmount = request.UnitAmount * request.Quantity;
        var discountAmount = discount != null ? discount.ComputeDiscount(grossAmount) : 0m;

        var chargeResult = StudentCharge.Create(
            academicYearId: request.AcademicYearId,
            academicYearName: academicYearName,
            operationalPeriodId: activePeriod?.Id,
            operationalPeriodName: activePeriod?.Name,
            billingMonth: request.BillingMonth,
            chargeTypeId: chargeType.Id,
            chargeTypeName: chargeType.Name,
            chargeFrequency: chargeType.Frequency,
            studentId: student.StudentId,
            studentName: student.StudentName,
            studentNis: student.StudentNis,
            schoolLevel: student.SchoolLevel,
            grade: student.Grade,
            studyGroupId: student.StudyGroupId == Guid.Empty ? null : student.StudyGroupId,
            studyGroupName: string.IsNullOrWhiteSpace(student.StudyGroupName) ? null : student.StudyGroupName,
            description: request.Description,
            unitAmount: request.UnitAmount,
            quantity: request.Quantity,
            discountAmount: discountAmount,
            discountReason: discount?.Reason,
            dueDate: request.DueDate);

        if (chargeResult.IsFailure)
        {
            return Result<StudentChargeDto>.Failure(chargeResult.Error);
        }

        var charge = chargeResult.Value!;
        await studentChargeRepository.InsertAsync(charge, cancellationToken);

        return Result<StudentChargeDto>.Success(StudentChargeDto.FromDomain(charge));
    }
}
