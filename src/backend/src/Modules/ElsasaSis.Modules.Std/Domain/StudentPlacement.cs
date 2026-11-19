using ElsasaSis.Contracts.Student.Events;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Std.Domain;

public sealed class StudentPlacement : AggregateRoot<Guid>
{
    public Guid StudentId { get; private set; }
    public Guid AcademicYearId { get; private set; }
    public string AcademicYearName { get; private set; } = string.Empty;
    public SchoolLevel SchoolLevel { get; private set; }
    public string Grade { get; private set; } = string.Empty;
    public Guid? StudyGroupId { get; private set; }
    public string? StudyGroupName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private StudentPlacement()
    {
    }

    private StudentPlacement(
        Guid id,
        Guid studentId,
        Guid academicYearId,
        string academicYearName,
        SchoolLevel schoolLevel,
        string grade,
        Guid? studyGroupId,
        string? studyGroupName,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        StudentId = studentId;
        AcademicYearId = academicYearId;
        AcademicYearName = academicYearName;
        SchoolLevel = schoolLevel;
        Grade = grade;
        StudyGroupId = studyGroupId;
        StudyGroupName = studyGroupName;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<StudentPlacement> CreateOrUpdate(
        Guid studentId,
        Guid academicYearId,
        string academicYearName,
        SchoolLevel schoolLevel,
        string grade,
        Guid? studyGroupId = null,
        string? studyGroupName = null,
        DateTime? utcNow = null) =>
        CreateOrUpdate(
            existing: null,
            studentId,
            academicYearId,
            academicYearName,
            schoolLevel,
            grade,
            studyGroupId,
            studyGroupName,
            utcNow);

    public static Result<StudentPlacement> CreateOrUpdate(
        StudentPlacement? existing,
        Guid studentId,
        Guid academicYearId,
        string academicYearName,
        SchoolLevel schoolLevel,
        string grade,
        Guid? studyGroupId = null,
        string? studyGroupName = null,
        DateTime? utcNow = null)
    {
        var validationResult = ValidatePlacement(
            studentId,
            academicYearId,
            academicYearName,
            schoolLevel,
            grade,
            studyGroupId,
            studyGroupName);

        if (validationResult.IsFailure)
        {
            return Result<StudentPlacement>.Failure(validationResult.Error);
        }

        var now = utcNow ?? DateTime.UtcNow;

        if (existing is not null)
        {
            if (existing.StudentId != studentId || existing.AcademicYearId != academicYearId)
            {
                return Result<StudentPlacement>.Failure(StdErrors.PlacementAggregateMismatch(existing.Id));
            }

            var updateResult = existing.Update(
                academicYearName,
                schoolLevel,
                grade,
                studyGroupId,
                studyGroupName,
                now);

            return updateResult.IsFailure
                ? Result<StudentPlacement>.Failure(updateResult.Error)
                : Result<StudentPlacement>.Success(existing);
        }

        var placement = new StudentPlacement(
            UuidV7.NewGuid(),
            studentId,
            academicYearId,
            academicYearName.Trim(),
            schoolLevel,
            grade.Trim(),
            studyGroupId,
            studyGroupId.HasValue ? studyGroupName!.Trim() : null,
            now,
            now);

        placement.RaiseDomainEvent(new StudentPlacedDomainEvent(
            placement.Id,
            placement.StudentId,
            placement.AcademicYearId,
            placement.StudyGroupId));

        return Result<StudentPlacement>.Success(placement);
    }

    public static Result<StudentPlacement> CreateOrUpdateForStudyGroup(
        StudentPlacement? existing,
        Guid studentId,
        Guid academicYearId,
        string academicYearName,
        SchoolLevel schoolLevel,
        string grade,
        StudyGroup? studyGroup,
        DateTime? utcNow = null)
    {
        if (studyGroup is not null)
        {
            // BR-STU-008: Optional study group must match academic year, school level, and grade.
            var trimmedGrade = string.IsNullOrWhiteSpace(grade) ? string.Empty : grade.Trim();
            if (studyGroup.AcademicYearId != academicYearId ||
                studyGroup.SchoolLevel != schoolLevel ||
                !string.Equals(studyGroup.Grade, trimmedGrade, StringComparison.OrdinalIgnoreCase))
            {
                return Result<StudentPlacement>.Failure(StdErrors.PlacementStudyGroupMismatch(studyGroup.Id));
            }
        }

        return CreateOrUpdate(
            existing,
            studentId,
            academicYearId,
            academicYearName,
            schoolLevel,
            grade,
            studyGroup?.Id,
            studyGroup?.Name,
            utcNow);
    }

    public Result Update(
        string academicYearName,
        SchoolLevel schoolLevel,
        string grade,
        Guid? studyGroupId = null,
        string? studyGroupName = null,
        DateTime? utcNow = null)
    {
        var validationResult = ValidatePlacement(
            StudentId,
            AcademicYearId,
            academicYearName,
            schoolLevel,
            grade,
            studyGroupId,
            studyGroupName);

        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        AcademicYearName = academicYearName.Trim();
        SchoolLevel = schoolLevel;
        Grade = grade.Trim();
        StudyGroupId = studyGroupId;
        StudyGroupName = studyGroupId.HasValue ? studyGroupName!.Trim() : null;
        UpdatedAt = utcNow ?? DateTime.UtcNow;

        RaiseDomainEvent(new StudentPlacedDomainEvent(
            Id,
            StudentId,
            AcademicYearId,
            StudyGroupId));

        return Result.Success();
    }

    public static StudentPlacement Rehydrate(
        Guid id,
        Guid studentId,
        Guid academicYearId,
        string academicYearName,
        SchoolLevel schoolLevel,
        string grade,
        Guid? studyGroupId,
        string? studyGroupName,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(
            id,
            studentId,
            academicYearId,
            academicYearName,
            schoolLevel,
            grade,
            studyGroupId,
            studyGroupName,
            createdAt,
            updatedAt);

    private static Result ValidatePlacement(
        Guid studentId,
        Guid academicYearId,
        string academicYearName,
        SchoolLevel schoolLevel,
        string grade,
        Guid? studyGroupId,
        string? studyGroupName)
    {
        if (studentId == Guid.Empty)
        {
            return Result.Failure(StdErrors.StudentIdRequired());
        }

        // BR-STU-008: Placement must reference a valid academic year, school level, and grade.
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

        // EXT-STU-001: study_group_id and study_group_name must both be null or both populated.
        if (studyGroupId == Guid.Empty)
        {
            return Result.Failure(StdErrors.StudyGroupReferenceIncomplete("studyGroupId"));
        }

        if (studyGroupId.HasValue && string.IsNullOrWhiteSpace(studyGroupName))
        {
            return Result.Failure(StdErrors.StudyGroupReferenceIncomplete("studyGroupName"));
        }

        if (!studyGroupId.HasValue && !string.IsNullOrWhiteSpace(studyGroupName))
        {
            return Result.Failure(StdErrors.StudyGroupReferenceIncomplete("studyGroupId"));
        }

        return Result.Success();
    }
}
