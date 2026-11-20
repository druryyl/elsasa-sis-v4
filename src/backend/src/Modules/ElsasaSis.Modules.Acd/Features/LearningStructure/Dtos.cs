using ElsasaSis.Modules.Acd.Domain.Model;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure;

public sealed record SubjectDto(
    Guid Id,
    string Name,
    string? Code,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record LearningUnitDto(
    Guid Id,
    Guid SubjectStageId,
    string Name,
    int SequenceOrder,
    string UnitType,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record SubjectStageGradeDto(
    Guid Id,
    Guid SubjectStageId,
    string SchoolLevel,
    string Grade,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record SubjectStageDto(
    Guid Id,
    Guid SubjectId,
    string SubjectName,
    string Name,
    int SequenceOrder,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<LearningUnitDto> Units,
    IReadOnlyList<SubjectStageGradeDto> GradeMappings);

public static class LearningStructureDtoExtensions
{
    public static SubjectDto ToDto(this Subject subject) =>
        new(subject.Id, subject.Name, subject.Code, subject.IsActive, subject.CreatedAt, subject.UpdatedAt);

    public static LearningUnitDto ToDto(this LearningUnit unit) =>
        new(unit.Id, unit.SubjectStageId, unit.Name, unit.SequenceOrder, unit.UnitType, unit.Description, unit.CreatedAt, unit.UpdatedAt);

    public static SubjectStageGradeDto ToDto(this SubjectStageGrade grade) =>
        new(grade.Id, grade.SubjectStageId, grade.SchoolLevel, grade.Grade, grade.CreatedAt, grade.UpdatedAt);

    public static SubjectStageDto ToDto(this SubjectStage stage) =>
        new(stage.Id,
            stage.SubjectId,
            stage.SubjectName,
            stage.Name,
            stage.SequenceOrder,
            stage.Description,
            stage.CreatedAt,
            stage.UpdatedAt,
            stage.LearningUnits.Select(u => u.ToDto()).ToList(),
            stage.GradeMappings.Select(g => g.ToDto()).ToList());
}
