namespace ElsasaSis.Modules.Acd.Features.Participations.EnrollStudentsInSubjectStages;

public sealed record EnrollStudentsInSubjectStagesRequest(
    Guid OperationalPeriodId,
    Guid SubjectStageId,
    Guid StudyGroupId);
