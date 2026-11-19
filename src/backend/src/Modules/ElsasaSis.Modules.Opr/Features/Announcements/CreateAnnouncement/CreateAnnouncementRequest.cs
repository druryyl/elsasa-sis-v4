namespace ElsasaSis.Modules.Opr.Features.Announcements.CreateAnnouncement;

public sealed record CreateAnnouncementRequest(
    Guid? AcademicYearId,
    string Title,
    string Content,
    string TargetAudience);
