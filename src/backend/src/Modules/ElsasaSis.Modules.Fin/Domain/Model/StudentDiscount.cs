using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Fin.Domain.Model;

public sealed class StudentDiscount : AggregateRoot<Guid>
{
    public const int MaxReasonLength = 500;

    public Guid AcademicYearId { get; private set; }
    public string AcademicYearName { get; private set; } = string.Empty;
    public Guid ChargeTypeId { get; private set; }
    public string ChargeTypeName { get; private set; } = string.Empty;
    public Guid StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string StudentNis { get; private set; } = string.Empty;
    public string DiscountType { get; private set; } = string.Empty;
    public decimal DiscountValue { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private StudentDiscount()
    {
    }

    private StudentDiscount(
        Guid id,
        Guid academicYearId,
        string academicYearName,
        Guid chargeTypeId,
        string chargeTypeName,
        Guid studentId,
        string studentName,
        string studentNis,
        string discountType,
        decimal discountValue,
        string reason,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        AcademicYearId = academicYearId;
        AcademicYearName = academicYearName;
        ChargeTypeId = chargeTypeId;
        ChargeTypeName = chargeTypeName;
        StudentId = studentId;
        StudentName = studentName;
        StudentNis = studentNis;
        DiscountType = discountType;
        DiscountValue = discountValue;
        Reason = reason;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<StudentDiscount> Create(
        Guid academicYearId,
        string academicYearName,
        Guid chargeTypeId,
        string chargeTypeName,
        Guid studentId,
        string studentName,
        string studentNis,
        string discountType,
        decimal discountValue,
        string reason,
        DateTime? utcNow = null)
    {
        if (academicYearId == Guid.Empty || string.IsNullOrWhiteSpace(academicYearName))
        {
            return Result<StudentDiscount>.Failure(
                DomainError.Validation("Fin.StudentDiscount.AcademicYearRequired", "Academic year is required.", "academicYearId"));
        }

        if (chargeTypeId == Guid.Empty || string.IsNullOrWhiteSpace(chargeTypeName))
        {
            return Result<StudentDiscount>.Failure(
                DomainError.Validation("Fin.StudentDiscount.ChargeTypeRequired", "Charge type is required.", "chargeTypeId"));
        }

        if (studentId == Guid.Empty || string.IsNullOrWhiteSpace(studentName) || string.IsNullOrWhiteSpace(studentNis))
        {
            return Result<StudentDiscount>.Failure(
                DomainError.Validation("Fin.StudentDiscount.StudentRequired", "Student details are required.", "studentId"));
        }

        var normalizedType = discountType?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!DiscountTypes.IsValid(normalizedType))
        {
            return Result<StudentDiscount>.Failure(FinErrors.InvalidDiscountType(discountType ?? string.Empty));
        }

        if (normalizedType == DiscountTypes.Percentage)
        {
            if (discountValue <= 0m || discountValue > 100m)
            {
                return Result<StudentDiscount>.Failure(
                    FinErrors.InvalidDiscountValue("Percentage discount value must be greater than 0% and at most 100%."));
            }
        }
        else if (normalizedType == DiscountTypes.FixedAmount)
        {
            if (discountValue <= 0m)
            {
                return Result<StudentDiscount>.Failure(
                    FinErrors.InvalidDiscountValue("Fixed amount discount must be greater than zero."));
            }
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > MaxReasonLength)
        {
            return Result<StudentDiscount>.Failure(
                DomainError.Validation("Fin.StudentDiscount.InvalidReason", "Discount reason is required and cannot exceed 500 characters.", "reason"));
        }

        var now = utcNow ?? DateTime.UtcNow;
        var discount = new StudentDiscount(
            UuidV7.NewGuid(),
            academicYearId,
            academicYearName.Trim(),
            chargeTypeId,
            chargeTypeName.Trim(),
            studentId,
            studentName.Trim(),
            studentNis.Trim(),
            normalizedType,
            discountValue,
            reason.Trim(),
            true,
            now,
            now);

        return Result<StudentDiscount>.Success(discount);
    }

    public Result Revoke(DateTime? utcNow = null)
    {
        if (!IsActive)
        {
            return Result.Failure(FinErrors.StudentDiscountAlreadyRevoked(Id));
        }

        IsActive = false;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public decimal ComputeDiscount(decimal grossAmount)
    {
        if (!IsActive || grossAmount <= 0m)
        {
            return 0m;
        }

        if (DiscountType == DiscountTypes.Percentage)
        {
            var calculated = Math.Round(grossAmount * (DiscountValue / 100m), 2, MidpointRounding.AwayFromZero);
            return Math.Min(calculated, grossAmount);
        }

        if (DiscountType == DiscountTypes.FixedAmount)
        {
            return Math.Min(DiscountValue, grossAmount);
        }

        return 0m;
    }

    public static StudentDiscount Rehydrate(
        Guid id,
        Guid academicYearId,
        string academicYearName,
        Guid chargeTypeId,
        string chargeTypeName,
        Guid studentId,
        string studentName,
        string studentNis,
        string discountType,
        decimal discountValue,
        string reason,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new StudentDiscount(
            id,
            academicYearId,
            academicYearName,
            chargeTypeId,
            chargeTypeName,
            studentId,
            studentName,
            studentNis,
            discountType,
            discountValue,
            reason,
            isActive,
            createdAt,
            updatedAt);
    }
}
