using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Fin.Features.Billing.GenerateBatchTuitionCharges;

public sealed class GenerateBatchTuitionChargesHandler(
    IOperationalContract operationalContract,
    IStudentContract studentContract,
    IChargeTypeRepository chargeTypeRepository,
    IStudentDiscountRepository studentDiscountRepository,
    IStudentChargeRepository studentChargeRepository)
{
    public async Task<Result<GenerateBatchTuitionChargesResponse>> HandleAsync(
        GenerateBatchTuitionChargesRequest request,
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
                return Result<GenerateBatchTuitionChargesResponse>.Failure(
                    DomainError.NotFound("Fin.AcademicYear.NotFound", $"Academic year with ID '{request.AcademicYearId}' was not found."));
            }
        }

        var activePeriod = await operationalContract.GetActiveOperationalPeriodAsync(cancellationToken);

        var students = await studentContract.GetActiveStudentsByScopeAsync(
            request.StudyGroupId,
            request.SchoolLevel,
            request.Grade,
            cancellationToken);

        var activeMonthlyCharges = await chargeTypeRepository.GetActiveByScopeAsync(
            request.AcademicYearId,
            request.SchoolLevel,
            request.Grade,
            request.StudyGroupId,
            cancellationToken);

        var monthlyChargeTypes = activeMonthlyCharges
            .Where(c => c.Frequency == ChargeFrequencies.Monthly && c.Status == ChargeTypeStatuses.Active)
            .ToList();

        DateOnly defaultDueDate;
        var parts = request.BillingMonth.Split('-');
        if (int.TryParse(parts[0], out var year) && int.TryParse(parts[1], out var month))
        {
            var day = Math.Min(10, DateTime.DaysInMonth(year, month));
            defaultDueDate = new DateOnly(year, month, day);
        }
        else
        {
            defaultDueDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        var effectiveDueDate = request.DueDate ?? defaultDueDate;
        var chargesToInsert = new List<StudentCharge>();
        var skippedCount = 0;
        var now = DateTime.UtcNow;

        foreach (var student in students)
        {
            var studentCharges = monthlyChargeTypes.Where(ct =>
                (string.IsNullOrEmpty(ct.SchoolLevel) || string.Equals(ct.SchoolLevel, student.SchoolLevel, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(ct.Grade) || string.Equals(ct.Grade, student.Grade, StringComparison.OrdinalIgnoreCase)) &&
                (!ct.StudyGroupId.HasValue || ct.StudyGroupId.Value == student.StudyGroupId)).ToList();

            foreach (var chargeType in studentCharges)
            {
                var isAlreadyBilled = await studentChargeRepository.ExistsMonthlyChargeAsync(
                    student.StudentId,
                    chargeType.Id,
                    request.BillingMonth,
                    cancellationToken);

                if (isAlreadyBilled)
                {
                    skippedCount++;
                    continue;
                }

                var discount = await studentDiscountRepository.GetActiveDiscountAsync(
                    student.StudentId,
                    chargeType.Id,
                    request.AcademicYearId,
                    cancellationToken);

                var discountAmount = discount != null ? discount.ComputeDiscount(chargeType.Amount) : 0m;

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
                    description: $"SPP Bulanan {request.BillingMonth} - {chargeType.Name}",
                    unitAmount: chargeType.Amount,
                    quantity: 1,
                    discountAmount: discountAmount,
                    discountReason: discount?.Reason,
                    dueDate: effectiveDueDate,
                    utcNow: now);

                if (chargeResult.IsFailure)
                {
                    return Result<GenerateBatchTuitionChargesResponse>.Failure(chargeResult.Error);
                }

                chargesToInsert.Add(chargeResult.Value!);
            }
        }

        if (chargesToInsert.Count > 0)
        {
            await studentChargeRepository.InsertBatchAsync(chargesToInsert, cancellationToken);
        }

        return Result<GenerateBatchTuitionChargesResponse>.Success(
            new GenerateBatchTuitionChargesResponse(chargesToInsert.Count, skippedCount));
    }
}
