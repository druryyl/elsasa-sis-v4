namespace ElsasaSis.Modules.Acd.Features.Scorebook.GetSubjectGradebookSummary;

public sealed record GetSubjectGradebookSummaryQuery(
    Guid SubjectStageId,
    Guid StudyGroupId,
    Guid OperationalPeriodId);
