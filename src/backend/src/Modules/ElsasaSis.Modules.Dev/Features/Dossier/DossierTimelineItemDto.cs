namespace ElsasaSis.Modules.Dev.Features.Dossier;

public sealed record DossierTimelineItemDto(
    Guid Id,
    string Category,
    DateOnly Date,
    string Title,
    string Description,
    string Badge,
    string RecordedBy,
    DateTime CreatedAt);
