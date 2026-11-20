namespace ElsasaSis.Modules.Acd.Features.LearningStructure.GetSubjects;

public sealed record SubjectResponse(
    Guid Id,
    string Name,
    string? Code,
    IReadOnlyList<SubjectStageResponse>? Stages = null);

public sealed record SubjectStageResponse(
    Guid Id,
    Guid SubjectId,
    string SubjectName,
    string Name,
    int SequenceOrder,
    string? Description,
    IReadOnlyList<LearningUnitResponse> Units,
    IReadOnlyList<SubjectStageGradeResponse> GradeMappings);

public sealed record LearningUnitResponse(
    Guid Id,
    Guid SubjectStageId,
    string Name,
    int SequenceOrder,
    string UnitType,
    string? Description);

public sealed record SubjectStageGradeResponse(
    Guid Id,
    Guid SubjectStageId,
    string SchoolLevel,
    string Grade);
