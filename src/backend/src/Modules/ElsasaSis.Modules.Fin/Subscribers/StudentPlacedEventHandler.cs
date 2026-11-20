using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Contracts.Student.Events;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ElsasaSis.Modules.Fin.Subscribers;

public sealed class StudentPlacedEventHandler(
    IOperationalContract operationalContract,
    IStudentContract studentContract,
    IChargeTypeRepository chargeTypeRepository,
    IStudentDiscountRepository studentDiscountRepository,
    IStudentChargeRepository studentChargeRepository,
    ILogger<StudentPlacedEventHandler> logger)
    : INotificationHandler<StudentPlacedDomainEvent>
{
    public async Task Handle(StudentPlacedDomainEvent notification, CancellationToken cancellationToken)
    {
        string academicYearName;
        var academicYear = await operationalContract.GetAcademicYearByIdAsync(notification.AcademicYearId, cancellationToken);
        if (academicYear is not null)
        {
            academicYearName = academicYear.Name;
        }
        else
        {
            var activeYear = await operationalContract.GetActiveAcademicYearAsync(cancellationToken);
            if (activeYear is not null && activeYear.Id == notification.AcademicYearId)
            {
                academicYearName = activeYear.Name;
            }
            else
            {
                logger.LogWarning("StudentPlacedEventHandler: Academic year '{AcademicYearId}' was not found.", notification.AcademicYearId);
                return;
            }
        }

        var students = await studentContract.GetActiveStudentsByScopeAsync(
            notification.StudyGroupId,
            null,
            null,
            cancellationToken);

        var student = students.FirstOrDefault(s => s.StudentId == notification.StudentId);
        if (student is null)
        {
            students = await studentContract.GetActiveStudentsByScopeAsync(null, null, null, cancellationToken);
            student = students.FirstOrDefault(s => s.StudentId == notification.StudentId);
        }

        if (student is null)
        {
            logger.LogWarning("StudentPlacedEventHandler: Student with ID '{StudentId}' was not found among active students.", notification.StudentId);
            return;
        }

        var activeCharges = await chargeTypeRepository.GetActiveByScopeAsync(
            notification.AcademicYearId,
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
                "StudentPlacedEventHandler: No active monthly charge types found for student '{StudentId}' in academic year '{AcademicYearId}'.",
                notification.StudentId,
                notification.AcademicYearId);
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
                notification.AcademicYearId,
                cancellationToken);

            var discountAmount = discount != null ? discount.ComputeDiscount(chargeType.Amount) : 0m;

            var chargeResult = StudentCharge.Create(
                academicYearId: notification.AcademicYearId,
                academicYearName: academicYearName,
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
                    "StudentPlacedEventHandler: Created monthly tuition charge for StudentId={StudentId}, ChargeTypeId={ChargeTypeId}, Month={BillingMonth}.",
                    student.StudentId,
                    chargeType.Id,
                    billingMonth);
            }
        }
    }
}
