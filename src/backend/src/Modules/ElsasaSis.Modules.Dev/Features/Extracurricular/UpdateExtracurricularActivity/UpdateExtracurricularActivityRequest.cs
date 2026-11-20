namespace ElsasaSis.Modules.Dev.Features.Extracurricular.UpdateExtracurricularActivity;

public sealed record UpdateExtracurricularActivityRequest(
    string Name,
    string? Description,
    Guid SupervisorId,
    string Status);
