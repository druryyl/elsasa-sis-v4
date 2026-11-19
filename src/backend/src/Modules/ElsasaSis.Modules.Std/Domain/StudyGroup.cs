using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Std.Domain;

public sealed class StudyGroup : AggregateRoot<Guid>
{
    public Guid AcademicYearId { get; private set; }
    public string AcademicYearName { get; private set; } = string.Empty;
    public SchoolLevel SchoolLevel { get; private set; }
    public string Grade { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid? HomeroomTeacherId { get; private set; }
    public string? HomeroomTeacherName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private StudyGroup()
    {
    }

    private StudyGroup(
        Guid id,
        Guid academicYearId,
        string academicYearName,
        SchoolLevel schoolLevel,
        string grade,
        string name,
        Guid? homeroomTeacherId,
        string? homeroomTeacherName,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        AcademicYearId = academicYearId;
        AcademicYearName = academicYearName;
        SchoolLevel = schoolLevel;
        Grade = grade;
        Name = name;
        HomeroomTeacherId = homeroomTeacherId;
        HomeroomTeacherName = homeroomTeacherName;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<StudyGroup> Create(
        Guid academicYearId,
        string academicYearName,
        SchoolLevel schoolLevel,
        string grade,
        string name,
        Guid? homeroomTeacherId = null,
        string? homeroomTeacherName = null,
        DateTime? utcNow = null)
    {
        var validationResult = ValidateDefinition(
            academicYearId,
            academicYearName,
            schoolLevel,
            grade,
            name,
            homeroomTeacherId,
            homeroomTeacherName);

        if (validationResult.IsFailure)
        {
            return Result<StudyGroup>.Failure(validationResult.Error);
        }

        var now = utcNow ?? DateTime.UtcNow;
        var studyGroup = new StudyGroup(
            UuidV7.NewGuid(),
            academicYearId,
            academicYearName.Trim(),
            schoolLevel,
            grade.Trim(),
            name.Trim(),
            homeroomTeacherId,
            homeroomTeacherId.HasValue ? homeroomTeacherName!.Trim() : null,
            now,
            now);

        return Result<StudyGroup>.Success(studyGroup);
    }

    public Result Update(
        SchoolLevel schoolLevel,
        string grade,
        string name,
        Guid? homeroomTeacherId = null,
        string? homeroomTeacherName = null,
        DateTime? utcNow = null) =>
        Update(
            AcademicYearId,
            AcademicYearName,
            schoolLevel,
            grade,
            name,
            homeroomTeacherId,
            homeroomTeacherName,
            utcNow);

    public Result Update(
        Guid academicYearId,
        string academicYearName,
        SchoolLevel schoolLevel,
        string grade,
        string name,
        Guid? homeroomTeacherId = null,
        string? homeroomTeacherName = null,
        DateTime? utcNow = null)
    {
        var validationResult = ValidateDefinition(
            academicYearId,
            academicYearName,
            schoolLevel,
            grade,
            name,
            homeroomTeacherId,
            homeroomTeacherName);

        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        AcademicYearId = academicYearId;
        AcademicYearName = academicYearName.Trim();
        SchoolLevel = schoolLevel;
        Grade = grade.Trim();
        Name = name.Trim();
        HomeroomTeacherId = homeroomTeacherId;
        HomeroomTeacherName = homeroomTeacherId.HasValue ? homeroomTeacherName!.Trim() : null;
        UpdatedAt = utcNow ?? DateTime.UtcNow;

        return Result.Success();
    }

    public static StudyGroup Rehydrate(
        Guid id,
        Guid academicYearId,
        string academicYearName,
        SchoolLevel schoolLevel,
        string grade,
        string name,
        Guid? homeroomTeacherId,
        string? homeroomTeacherName,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(
            id,
            academicYearId,
            academicYearName,
            schoolLevel,
            grade,
            name,
            homeroomTeacherId,
            homeroomTeacherName,
            createdAt,
            updatedAt);

    private static Result ValidateDefinition(
        Guid academicYearId,
        string academicYearName,
        SchoolLevel schoolLevel,
        string grade,
        string name,
        Guid? homeroomTeacherId,
        string? homeroomTeacherName)
    {
        var yearResult = StdDomainValidation.ValidateAcademicYearReference(academicYearId, academicYearName);
        if (yearResult.IsFailure)
        {
            return yearResult;
        }

        var levelResult = StdDomainValidation.ValidateSchoolLevelAndGrade(schoolLevel, grade);
        if (levelResult.IsFailure)
        {
            return levelResult;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(StdErrors.StudyGroupNameRequired());
        }

        if (homeroomTeacherId == Guid.Empty)
        {
            return Result.Failure(StdErrors.HomeroomTeacherReferenceIncomplete("homeroomTeacherId"));
        }

        if (homeroomTeacherId.HasValue && string.IsNullOrWhiteSpace(homeroomTeacherName))
        {
            return Result.Failure(StdErrors.HomeroomTeacherReferenceIncomplete("homeroomTeacherName"));
        }

        if (!homeroomTeacherId.HasValue && !string.IsNullOrWhiteSpace(homeroomTeacherName))
        {
            return Result.Failure(StdErrors.HomeroomTeacherReferenceIncomplete("homeroomTeacherId"));
        }

        return Result.Success();
    }
}
