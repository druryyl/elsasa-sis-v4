using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Fin.Domain.Model;

public sealed class ChargeType : AggregateRoot<Guid>
{
    public const int MaxNameLength = 100;

    public Guid AcademicYearId { get; private set; }
    public string AcademicYearName { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Frequency { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string? SchoolLevel { get; private set; }
    public string? Grade { get; private set; }
    public Guid? StudyGroupId { get; private set; }
    public string? StudyGroupName { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ChargeType()
    {
    }

    private ChargeType(
        Guid id,
        Guid academicYearId,
        string academicYearName,
        string name,
        string frequency,
        decimal amount,
        string? schoolLevel,
        string? grade,
        Guid? studyGroupId,
        string? studyGroupName,
        string status,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        AcademicYearId = academicYearId;
        AcademicYearName = academicYearName;
        Name = name;
        Frequency = frequency;
        Amount = amount;
        SchoolLevel = schoolLevel;
        Grade = grade;
        StudyGroupId = studyGroupId;
        StudyGroupName = studyGroupName;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<ChargeType> Create(
        Guid academicYearId,
        string academicYearName,
        string name,
        string frequency,
        decimal amount,
        string? schoolLevel = null,
        string? grade = null,
        Guid? studyGroupId = null,
        string? studyGroupName = null,
        DateTime? utcNow = null)
    {
        if (academicYearId == Guid.Empty || string.IsNullOrWhiteSpace(academicYearName))
        {
            return Result<ChargeType>.Failure(
                DomainError.Validation("Fin.ChargeType.AcademicYearRequired", "Academic year is required.", "academicYearId"));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength)
        {
            return Result<ChargeType>.Failure(FinErrors.InvalidChargeTypeName());
        }

        var normalizedFrequency = frequency?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!ChargeFrequencies.IsValid(normalizedFrequency))
        {
            return Result<ChargeType>.Failure(FinErrors.InvalidChargeTypeFrequency(frequency ?? string.Empty));
        }

        if (amount < 0m)
        {
            return Result<ChargeType>.Failure(FinErrors.InvalidChargeTypeAmount());
        }

        var now = utcNow ?? DateTime.UtcNow;
        var chargeType = new ChargeType(
            UuidV7.NewGuid(),
            academicYearId,
            academicYearName.Trim(),
            name.Trim(),
            normalizedFrequency,
            amount,
            string.IsNullOrWhiteSpace(schoolLevel) ? null : schoolLevel.Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(grade) ? null : grade.Trim(),
            studyGroupId,
            string.IsNullOrWhiteSpace(studyGroupName) ? null : studyGroupName.Trim(),
            ChargeTypeStatuses.Draft,
            now,
            now);

        return Result<ChargeType>.Success(chargeType);
    }

    public Result Activate(DateTime? utcNow = null)
    {
        Status = ChargeTypeStatuses.Active;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result Update(
        string name,
        string frequency,
        decimal amount,
        string? schoolLevel = null,
        string? grade = null,
        Guid? studyGroupId = null,
        string? studyGroupName = null,
        DateTime? utcNow = null)
    {
        if (Status == ChargeTypeStatuses.Active)
        {
            return Result.Failure(FinErrors.ChargeTypeCannotBeModified(Id));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength)
        {
            return Result.Failure(FinErrors.InvalidChargeTypeName());
        }

        var normalizedFrequency = frequency?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!ChargeFrequencies.IsValid(normalizedFrequency))
        {
            return Result.Failure(FinErrors.InvalidChargeTypeFrequency(frequency ?? string.Empty));
        }

        if (amount < 0m)
        {
            return Result.Failure(FinErrors.InvalidChargeTypeAmount());
        }

        Name = name.Trim();
        Frequency = normalizedFrequency;
        Amount = amount;
        SchoolLevel = string.IsNullOrWhiteSpace(schoolLevel) ? null : schoolLevel.Trim().ToUpperInvariant();
        Grade = string.IsNullOrWhiteSpace(grade) ? null : grade.Trim();
        StudyGroupId = studyGroupId;
        StudyGroupName = string.IsNullOrWhiteSpace(studyGroupName) ? null : studyGroupName.Trim();
        UpdatedAt = utcNow ?? DateTime.UtcNow;

        return Result.Success();
    }

    public static ChargeType Rehydrate(
        Guid id,
        Guid academicYearId,
        string academicYearName,
        string name,
        string frequency,
        decimal amount,
        string? schoolLevel,
        string? grade,
        Guid? studyGroupId,
        string? studyGroupName,
        string status,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new ChargeType(
            id,
            academicYearId,
            academicYearName,
            name,
            frequency,
            amount,
            schoolLevel,
            grade,
            studyGroupId,
            studyGroupName,
            status,
            createdAt,
            updatedAt);
    }
}
