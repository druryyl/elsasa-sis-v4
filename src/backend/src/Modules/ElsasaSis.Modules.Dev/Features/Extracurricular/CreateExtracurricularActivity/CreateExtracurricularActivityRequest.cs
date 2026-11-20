namespace ElsasaSis.Modules.Dev.Features.Extracurricular.CreateExtracurricularActivity;

public sealed record CreateExtracurricularActivityRequest(
    string Name,
    string? Description,
    Guid SupervisorId);
