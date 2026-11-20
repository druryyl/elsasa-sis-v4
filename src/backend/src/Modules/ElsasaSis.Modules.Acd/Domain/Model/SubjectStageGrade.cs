using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Acd.Domain.Model;

public sealed class SubjectStageGrade : Entity<Guid>
{
    public Guid SubjectStageId { get; private set; }
    public string SchoolLevel { get; private set; } = string.Empty;
    public string Grade { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SubjectStageGrade()
    {
    }

    private SubjectStageGrade(
        Guid id,
        Guid subjectStageId,
        string schoolLevel,
        string grade,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        SubjectStageId = subjectStageId;
        SchoolLevel = schoolLevel;
        Grade = grade;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    internal static Result<SubjectStageGrade> Create(
        Guid subjectStageId,
        string schoolLevel,
        string grade,
        DateTime now)
    {
        if (!Model.SchoolLevel.IsValidCombination(schoolLevel, grade))
        {
            return Result<SubjectStageGrade>.Failure(LearningStructureErrors.InvalidGradeLevel(schoolLevel, grade));
        }

        var entity = new SubjectStageGrade(
            UuidV7.NewGuid(),
            subjectStageId,
            Model.SchoolLevel.Normalize(schoolLevel),
            grade.Trim(),
            now,
            now);

        return Result<SubjectStageGrade>.Success(entity);
    }

    public static SubjectStageGrade Rehydrate(
        Guid id,
        Guid subjectStageId,
        string schoolLevel,
        string grade,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new SubjectStageGrade(
            id,
            subjectStageId,
            schoolLevel,
            grade,
            createdAt,
            updatedAt);
    }
}
