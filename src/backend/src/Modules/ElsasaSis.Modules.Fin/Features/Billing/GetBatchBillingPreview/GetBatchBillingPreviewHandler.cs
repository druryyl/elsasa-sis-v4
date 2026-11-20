using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Fin.Features.Billing.GetBatchBillingPreview;

public sealed class GetBatchBillingPreviewHandler(
    IOperationalContract operationalContract,
    IStudentContract studentContract,
    IChargeTypeRepository chargeTypeRepository,
    IStudentDiscountRepository studentDiscountRepository,
    IStudentChargeRepository studentChargeRepository)
{
    public async Task<Result<BatchBillingPreviewResponse>> HandleAsync(
        BatchBillingPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var academicYear = await operationalContract.GetAcademicYearByIdAsync(request.AcademicYearId, cancellationToken);
        if (academicYear is null)
        {
            var activeYear = await operationalContract.GetActiveAcademicYearAsync(cancellationToken);
            if (activeYear is null || activeYear.Id != request.AcademicYearId)
            {
                return Result<BatchBillingPreviewResponse>.Failure(
                    DomainError.NotFound("Fin.AcademicYear.NotFound", $"Academic year with ID '{request.AcademicYearId}' was not found."));
            }
        }

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

        var items = new List<BatchBillingPreviewItemDto>();
        var distinctStudentsBilled = new HashSet<Guid>();
        var totalGross = 0m;
        var totalDiscount = 0m;
        var totalNet = 0m;
        var prospectiveChargeCount = 0;
        var skippedCount = 0;

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

                var discount = await studentDiscountRepository.GetActiveDiscountAsync(
                    student.StudentId,
                    chargeType.Id,
                    request.AcademicYearId,
                    cancellationToken);

                var gross = chargeType.Amount;
                var discountAmount = discount != null ? discount.ComputeDiscount(gross) : 0m;
                var net = gross - discountAmount;

                if (isAlreadyBilled)
                {
                    skippedCount++;
                }
                else
                {
                    prospectiveChargeCount++;
                    distinctStudentsBilled.Add(student.StudentId);
                    totalGross += gross;
                    totalDiscount += discountAmount;
                    totalNet += net;
                }

                items.Add(new BatchBillingPreviewItemDto(
                    student.StudentId,
                    student.StudentName,
                    student.StudentNis,
                    student.StudyGroupId == Guid.Empty ? null : student.StudyGroupId,
                    string.IsNullOrWhiteSpace(student.StudyGroupName) ? null : student.StudyGroupName,
                    student.SchoolLevel,
                    student.Grade,
                    chargeType.Id,
                    chargeType.Name,
                    gross,
                    discountAmount,
                    discount?.Reason,
                    net,
                    isAlreadyBilled));
            }
        }

        var response = new BatchBillingPreviewResponse(
            StudentCount: distinctStudentsBilled.Count > 0 ? distinctStudentsBilled.Count : students.Count,
            ChargeCount: prospectiveChargeCount,
            SkippedCount: skippedCount,
            TotalGrossAmount: totalGross,
            TotalDiscountAmount: totalDiscount,
            TotalNetAmount: totalNet,
            Items: items);

        return Result<BatchBillingPreviewResponse>.Success(response);
    }
}
