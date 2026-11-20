namespace ElsasaSis.Modules.Dev.Features.Extracurricular.GetExtracurricularActivities;

public sealed record GetExtracurricularActivitiesQuery(
    string? Status = null,
    Guid? SupervisorId = null);
