namespace ElsasaSis.Modules.Opr.Features.Announcements.UpdateAnnouncement;

public sealed record UpdateAnnouncementRequest(
    string Title,
    string Content,
    string TargetAudience);
