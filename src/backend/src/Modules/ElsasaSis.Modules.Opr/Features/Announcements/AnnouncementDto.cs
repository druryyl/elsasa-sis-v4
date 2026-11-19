using ElsasaSis.Modules.Opr.Domain;

namespace ElsasaSis.Modules.Opr.Features.Announcements;

public sealed record AnnouncementDto(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    string Title,
    string Content,
    Guid AuthorId,
    string AuthorName,
    string TargetAudience,
    string Status,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

internal static class AnnouncementMappings
{
    public static AnnouncementDto ToDto(this Announcement announcement, string academicYearName) =>
        new(
            announcement.Id,
            announcement.AcademicYearId,
            academicYearName,
            announcement.Title,
            announcement.Content,
            announcement.AuthorId,
            announcement.AuthorName,
            announcement.TargetAudience.ToCode(),
            announcement.Status.ToCode(),
            announcement.PublishedAt,
            announcement.CreatedAt,
            announcement.UpdatedAt);
}
