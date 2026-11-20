using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Fin.Domain.Model;

public sealed class StudentCharge : AggregateRoot<Guid>
{
    public const int MaxDescriptionLength = 500;
    public const int MaxCancellationReasonLength = 500;

    public Guid AcademicYearId { get; private set; }
    public string AcademicYearName { get; private set; } = string.Empty;
    public Guid? OperationalPeriodId { get; private set; }
    public string? OperationalPeriodName { get; private set; }
    public string? BillingMonth { get; private set; }
    public Guid ChargeTypeId { get; private set; }
    public string ChargeTypeName { get; private set; } = string.Empty;
    public string ChargeFrequency { get; private set; } = string.Empty;
    public Guid StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string StudentNis { get; private set; } = string.Empty;
    public string SchoolLevel { get; private set; } = string.Empty;
    public string Grade { get; private set; } = string.Empty;
    public Guid? StudyGroupId { get; private set; }
    public string? StudyGroupName { get; private set; }
    public string? Description { get; private set; }
    public decimal UnitAmount { get; private set; }
    public int Quantity { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public string? DiscountReason { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal OutstandingAmount { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateOnly? DueDate { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private StudentCharge()
    {
    }

    private StudentCharge(
        Guid id,
        Guid academicYearId,
        string academicYearName,
        Guid? operationalPeriodId,
        string? operationalPeriodName,
        string? billingMonth,
        Guid chargeTypeId,
        string chargeTypeName,
        string chargeFrequency,
        Guid studentId,
        string studentName,
        string studentNis,
        string schoolLevel,
        string grade,
        Guid? studyGroupId,
        string? studyGroupName,
        string? description,
        decimal unitAmount,
        int quantity,
        decimal grossAmount,
        decimal discountAmount,
        string? discountReason,
        decimal netAmount,
        decimal paidAmount,
        decimal outstandingAmount,
        string status,
        DateOnly? dueDate,
        string? cancellationReason,
        DateTime? cancelledAt,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        AcademicYearId = academicYearId;
        AcademicYearName = academicYearName;
        OperationalPeriodId = operationalPeriodId;
        OperationalPeriodName = operationalPeriodName;
        BillingMonth = billingMonth;
        ChargeTypeId = chargeTypeId;
        ChargeTypeName = chargeTypeName;
        ChargeFrequency = chargeFrequency;
        StudentId = studentId;
        StudentName = studentName;
        StudentNis = studentNis;
        SchoolLevel = schoolLevel;
        Grade = grade;
        StudyGroupId = studyGroupId;
        StudyGroupName = studyGroupName;
        Description = description;
        UnitAmount = unitAmount;
        Quantity = quantity;
        GrossAmount = grossAmount;
        DiscountAmount = discountAmount;
        DiscountReason = discountReason;
        NetAmount = netAmount;
        PaidAmount = paidAmount;
        OutstandingAmount = outstandingAmount;
        Status = status;
        DueDate = dueDate;
        CancellationReason = cancellationReason;
        CancelledAt = cancelledAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<StudentCharge> Create(
        Guid academicYearId,
        string academicYearName,
        Guid? operationalPeriodId,
        string? operationalPeriodName,
        string? billingMonth,
        Guid chargeTypeId,
        string chargeTypeName,
        string chargeFrequency,
        Guid studentId,
        string studentName,
        string studentNis,
        string schoolLevel,
        string grade,
        Guid? studyGroupId = null,
        string? studyGroupName = null,
        string? description = null,
        decimal unitAmount = 0m,
        int quantity = 1,
        decimal discountAmount = 0m,
        string? discountReason = null,
        DateOnly? dueDate = null,
        DateTime? utcNow = null)
    {
        if (academicYearId == Guid.Empty || string.IsNullOrWhiteSpace(academicYearName))
        {
            return Result<StudentCharge>.Failure(
                DomainError.Validation("Fin.StudentCharge.AcademicYearRequired", "Academic year is required.", "academicYearId"));
        }

        if (chargeTypeId == Guid.Empty || string.IsNullOrWhiteSpace(chargeTypeName))
        {
            return Result<StudentCharge>.Failure(
                DomainError.Validation("Fin.StudentCharge.ChargeTypeRequired", "Charge type is required.", "chargeTypeId"));
        }

        var normalizedFrequency = chargeFrequency?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!ChargeFrequencies.IsValid(normalizedFrequency))
        {
            return Result<StudentCharge>.Failure(FinErrors.InvalidChargeTypeFrequency(chargeFrequency ?? string.Empty));
        }

        if (studentId == Guid.Empty || string.IsNullOrWhiteSpace(studentName) || string.IsNullOrWhiteSpace(studentNis))
        {
            return Result<StudentCharge>.Failure(
                DomainError.Validation("Fin.StudentCharge.StudentRequired", "Student details are required.", "studentId"));
        }

        if (string.IsNullOrWhiteSpace(schoolLevel))
        {
            return Result<StudentCharge>.Failure(
                DomainError.Validation("Fin.StudentCharge.SchoolLevelRequired", "School level is required.", "schoolLevel"));
        }

        if (string.IsNullOrWhiteSpace(grade))
        {
            return Result<StudentCharge>.Failure(
                DomainError.Validation("Fin.StudentCharge.GradeRequired", "Grade is required.", "grade"));
        }

        if (quantity <= 0)
        {
            return Result<StudentCharge>.Failure(
                DomainError.Validation("Fin.StudentCharge.InvalidQuantity", "Quantity must be greater than zero.", "quantity"));
        }

        if (unitAmount < 0m)
        {
            return Result<StudentCharge>.Failure(
                DomainError.Validation("Fin.StudentCharge.InvalidUnitAmount", "Unit amount cannot be negative.", "unitAmount"));
        }

        var grossAmount = unitAmount * quantity;
        var validDiscountAmount = Math.Max(0m, Math.Min(discountAmount, grossAmount));
        var netAmount = grossAmount - validDiscountAmount;
        var paidAmount = 0m;
        var outstandingAmount = netAmount;
        var status = netAmount == 0m ? ChargeStatuses.Paid : ChargeStatuses.Unpaid;

        var now = utcNow ?? DateTime.UtcNow;
        var charge = new StudentCharge(
            UuidV7.NewGuid(),
            academicYearId,
            academicYearName.Trim(),
            operationalPeriodId,
            string.IsNullOrWhiteSpace(operationalPeriodName) ? null : operationalPeriodName.Trim(),
            string.IsNullOrWhiteSpace(billingMonth) ? null : billingMonth.Trim(),
            chargeTypeId,
            chargeTypeName.Trim(),
            normalizedFrequency,
            studentId,
            studentName.Trim(),
            studentNis.Trim(),
            schoolLevel.Trim().ToUpperInvariant(),
            grade.Trim(),
            studyGroupId,
            string.IsNullOrWhiteSpace(studyGroupName) ? null : studyGroupName.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            unitAmount,
            quantity,
            grossAmount,
            validDiscountAmount,
            string.IsNullOrWhiteSpace(discountReason) ? null : discountReason.Trim(),
            netAmount,
            paidAmount,
            outstandingAmount,
            status,
            dueDate,
            null,
            null,
            now,
            now);

        return Result<StudentCharge>.Success(charge);
    }

    public Result ApplyPayment(decimal amount, DateTime? utcNow = null)
    {
        if (Status == ChargeStatuses.Cancelled)
        {
            return Result.Failure(FinErrors.StudentChargeAlreadyCancelled(Id));
        }

        if (amount <= 0m)
        {
            return Result.Failure(FinErrors.InvalidPaymentAmount(amount));
        }

        if (amount > OutstandingAmount)
        {
            return Result.Failure(FinErrors.PaymentAmountExceedsOutstanding(amount, OutstandingAmount));
        }

        PaidAmount += amount;
        OutstandingAmount = NetAmount - PaidAmount;

        if (OutstandingAmount == 0m)
        {
            Status = ChargeStatuses.Paid;
        }
        else
        {
            Status = ChargeStatuses.PartiallyPaid;
        }

        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result Cancel(string reason, DateTime utcNow)
    {
        if (Status == ChargeStatuses.Cancelled)
        {
            return Result.Failure(FinErrors.StudentChargeAlreadyCancelled(Id));
        }

        if (PaidAmount > 0m)
        {
            return Result.Failure(FinErrors.StudentChargeCannotBeCancelledWithPayments(Id, PaidAmount));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(FinErrors.MissingCancellationReason());
        }

        Status = ChargeStatuses.Cancelled;
        CancellationReason = reason.Trim();
        CancelledAt = utcNow;
        UpdatedAt = utcNow;

        return Result.Success();
    }

    public static StudentCharge Rehydrate(
        Guid id,
        Guid academicYearId,
        string academicYearName,
        Guid? operationalPeriodId,
        string? operationalPeriodName,
        string? billingMonth,
        Guid chargeTypeId,
        string chargeTypeName,
        string chargeFrequency,
        Guid studentId,
        string studentName,
        string studentNis,
        string schoolLevel,
        string grade,
        Guid? studyGroupId,
        string? studyGroupName,
        string? description,
        decimal unitAmount,
        int quantity,
        decimal grossAmount,
        decimal discountAmount,
        string? discountReason,
        decimal netAmount,
        decimal paidAmount,
        decimal outstandingAmount,
        string status,
        DateOnly? dueDate,
        string? cancellationReason,
        DateTime? cancelledAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new StudentCharge(
            id,
            academicYearId,
            academicYearName,
            operationalPeriodId,
            operationalPeriodName,
            billingMonth,
            chargeTypeId,
            chargeTypeName,
            chargeFrequency,
            studentId,
            studentName,
            studentNis,
            schoolLevel,
            grade,
            studyGroupId,
            studyGroupName,
            description,
            unitAmount,
            quantity,
            grossAmount,
            discountAmount,
            discountReason,
            netAmount,
            paidAmount,
            outstandingAmount,
            status,
            dueDate,
            cancellationReason,
            cancelledAt,
            createdAt,
            updatedAt);
    }
}
