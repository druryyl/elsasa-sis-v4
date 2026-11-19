namespace ElsasaSis.Modules.Opr.Features.Announcements.GetAnnouncements;

public sealed record GetAnnouncementsQuery(
    Guid? AcademicYearId = null,
    string? TargetAudience = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20,
    string? SortField = null,
    string? SortOrder = null,
    string? Search = null);
