using ElsasaSis.Modules.Dev.Domain;

namespace ElsasaSis.Modules.Dev.Features.Extracurricular;

public sealed record ExtracurricularActivityDto(
    Guid Id,
    string Name,
    string? Description,
    Guid SupervisorId,
    string SupervisorName,
    string Status,
    int ActiveParticipantCount,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ExtracurricularActivityDto FromDomain(ExtracurricularActivity activity) =>
        new(
            activity.Id,
            activity.Name,
            activity.Description,
            activity.SupervisorId,
            activity.SupervisorName,
            activity.Status,
            activity.Participations.Count(p => string.Equals(p.Status, ExtracurricularParticipationStatus.Active, StringComparison.OrdinalIgnoreCase)),
            activity.CreatedAt,
            activity.UpdatedAt);
}
