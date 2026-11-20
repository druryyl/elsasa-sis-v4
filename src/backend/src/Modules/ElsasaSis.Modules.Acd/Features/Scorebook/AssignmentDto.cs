namespace ElsasaSis.Modules.Acd.Features.Scorebook;

public sealed record AssignmentDto(
    Guid Id,
    Guid SubjectStageId,
    string SubjectStageName,
    Guid SubjectId,
    string SubjectName,
    Guid? LearningUnitId,
    string? LearningUnitName,
    Guid OperationalPeriodId,
    string OperationalPeriodName,
    Guid StudyGroupId,
    string StudyGroupName,
    string Title,
    string AssignmentType,
    decimal MaxScore,
    decimal? Weight,
    DateOnly AssignedDate,
    DateOnly? DueDate,
    Guid TeacherId,
    string TeacherName,
    DateTime CreatedAt,
    DateTime UpdatedAt);
