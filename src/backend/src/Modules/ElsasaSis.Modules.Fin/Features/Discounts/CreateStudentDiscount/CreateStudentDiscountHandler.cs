using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Fin.Features.Discounts.CreateStudentDiscount;

public sealed class CreateStudentDiscountHandler(
    IStudentDiscountRepository discountRepository,
    IChargeTypeRepository chargeTypeRepository,
    IStudentContract studentContract,
    IOperationalContract operationalContract)
{
    public async Task<Result<StudentDiscountDto>> HandleAsync(
        CreateStudentDiscountRequest request,
        CancellationToken cancellationToken = default)
    {
        var chargeType = await chargeTypeRepository.GetByIdAsync(request.ChargeTypeId, cancellationToken);
        if (chargeType is null)
        {
            return Result<StudentDiscountDto>.Failure(FinErrors.ChargeTypeNotFound(request.ChargeTypeId));
        }

        Guid academicYearId = chargeType.AcademicYearId;
        string academicYearName = chargeType.AcademicYearName;

        if (request.AcademicYearId.HasValue && request.AcademicYearId.Value != Guid.Empty && request.AcademicYearId.Value != academicYearId)
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
                    return Result<StudentDiscountDto>.Failure(DomainError.NotFound("Fin.AcademicYearNotFound", $"Academic year '{academicYearId}' was not found."));
                }
                academicYearName = year.Name;
            }
        }

        var studentName = request.StudentName?.Trim() ?? string.Empty;
        var studentNis = request.StudentNis?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(studentName) || string.IsNullOrWhiteSpace(studentNis))
        {
            var activeStudents = await studentContract.GetActiveStudentsByScopeAsync(
                studyGroupId: null,
                schoolLevel: null,
                grade: null,
                cancellationToken);

            var student = activeStudents.FirstOrDefault(s => s.StudentId == request.StudentId);
            if (student is null)
            {
                return Result<StudentDiscountDto>.Failure(FinErrors.StudentNotFound(request.StudentId));
            }

            studentName = student.StudentName;
            studentNis = student.StudentNis;
        }

        var exists = await discountRepository.ExistsForStudentAndChargeTypeAsync(
            request.StudentId,
            request.ChargeTypeId,
            academicYearId,
            null,
            cancellationToken);

        if (exists)
        {
            return Result<StudentDiscountDto>.Failure(
                FinErrors.StudentDiscountAlreadyExists(request.StudentId, request.ChargeTypeId, academicYearId));
        }

        var createResult = StudentDiscount.Create(
            academicYearId,
            academicYearName,
            chargeType.Id,
            chargeType.Name,
            request.StudentId,
            studentName,
            studentNis,
            request.DiscountType,
            request.DiscountValue,
            request.Reason);

        if (createResult.IsFailure)
        {
            return Result<StudentDiscountDto>.Failure(createResult.Error);
        }

        var discount = createResult.Value!;
        await discountRepository.InsertAsync(discount, cancellationToken);

        return Result<StudentDiscountDto>.Success(StudentDiscountDto.FromDomain(discount));
    }
}
