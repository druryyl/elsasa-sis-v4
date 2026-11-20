using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Contracts.Student.Events;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ElsasaSis.Modules.Fin.Subscribers;

public sealed class CandidateEnrolledEventHandler(
    IOperationalContract operationalContract,
    IStudentContract studentContract,
    IChargeTypeRepository chargeTypeRepository,
    IStudentDiscountRepository studentDiscountRepository,
    IStudentChargeRepository studentChargeRepository,
    ILogger<CandidateEnrolledEventHandler> logger)
    : INotificationHandler<CandidateEnrolledDomainEvent>,
      INotificationHandler<StudentEnrolledDomainEvent>
{
    public async Task Handle(CandidateEnrolledDomainEvent notification, CancellationToken cancellationToken)
    {
        await ProcessEnrolledStudentAsync(notification.StudentId, cancellationToken);
    }

    public async Task Handle(StudentEnrolledDomainEvent notification, CancellationToken cancellationToken)
    {
        await ProcessEnrolledStudentAsync(notification.StudentId, cancellationToken);
    }

    private async Task ProcessEnrolledStudentAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var activeYear = await operationalContract.GetActiveAcademicYearAsync(cancellationToken);
        if (activeYear is null)
        {
            logger.LogWarning("CandidateEnrolledEventHandler: No active academic year found.");
            return;
        }

        var students = await studentContract.GetActiveStudentsByScopeAsync(null, null, null, cancellationToken);
        var student = students.FirstOrDefault(s => s.StudentId == studentId);
        if (student is null)
        {
            logger.LogInformation(
                "CandidateEnrolledEventHandler: Student '{StudentId}' has not yet been placed into a study group/grade. Initial tuition charges will be generated upon placement.",
                studentId);
            return;
        }

        var activeCharges = await chargeTypeRepository.GetActiveByScopeAsync(
            activeYear.Id,
            student.SchoolLevel,
            student.Grade,
            student.StudyGroupId,
            cancellationToken);

        var monthlyCharges = activeCharges
            .Where(c => c.Frequency == ChargeFrequencies.Monthly && c.Status == ChargeTypeStatuses.Active)
            .ToList();

        if (monthlyCharges.Count == 0)
        {
            logger.LogInformation(
                "CandidateEnrolledEventHandler: No active monthly charge types found for student '{StudentId}' in academic year '{AcademicYearId}'.",
                studentId,
                activeYear.Id);
            return;
        }

        var activePeriod = await operationalContract.GetActiveOperationalPeriodAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var billingMonth = now.ToString("yyyy-MM");
        var dueDate = new DateOnly(now.Year, now.Month, Math.Min(10, DateTime.DaysInMonth(now.Year, now.Month)));

        foreach (var chargeType in monthlyCharges)
        {
            var exists = await studentChargeRepository.ExistsMonthlyChargeAsync(
                student.StudentId,
                chargeType.Id,
                billingMonth,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            var discount = await studentDiscountRepository.GetActiveDiscountAsync(
                student.StudentId,
                chargeType.Id,
                activeYear.Id,
                cancellationToken);

            var discountAmount = discount != null ? discount.ComputeDiscount(chargeType.Amount) : 0m;

            var chargeResult = StudentCharge.Create(
                academicYearId: activeYear.Id,
                academicYearName: activeYear.Name,
                operationalPeriodId: activePeriod?.Id,
                operationalPeriodName: activePeriod?.Name,
                billingMonth: billingMonth,
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
                description: $"SPP Bulanan {billingMonth} - {chargeType.Name}",
                unitAmount: chargeType.Amount,
                quantity: 1,
                discountAmount: discountAmount,
                discountReason: discount?.Reason,
                dueDate: dueDate,
                utcNow: now);

            if (chargeResult.IsSuccess)
            {
                await studentChargeRepository.InsertAsync(chargeResult.Value!, cancellationToken);
                logger.LogInformation(
                    "CandidateEnrolledEventHandler: Created initial tuition charge for StudentId={StudentId}, ChargeTypeId={ChargeTypeId}, Month={BillingMonth}.",
                    student.StudentId,
                    chargeType.Id,
                    billingMonth);
            }
        }
    }
}
