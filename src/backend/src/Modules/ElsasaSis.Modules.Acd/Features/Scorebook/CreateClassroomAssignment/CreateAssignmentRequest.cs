namespace ElsasaSis.Modules.Acd.Features.Scorebook.CreateClassroomAssignment;

public sealed record CreateAssignmentRequest(
    Guid SubjectStageId,
    Guid SubjectId,
    Guid StudyGroupId,
    Guid OperationalPeriodId,
    Guid? LearningUnitId,
    string Title,
    string AssignmentType,
    decimal MaxScore,
    decimal? Weight,
    DateOnly AssignedDate,
    DateOnly? DueDate);
