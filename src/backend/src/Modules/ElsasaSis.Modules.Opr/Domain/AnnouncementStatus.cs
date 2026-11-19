using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Domain;

public enum AnnouncementStatus
{
    Draft = 0,
    Published = 1
}

public static class AnnouncementStatusExtensions
{
    public static string ToCode(this AnnouncementStatus status) => status switch
    {
        AnnouncementStatus.Draft => "DRAFT",
        AnnouncementStatus.Published => "PUBLISHED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported announcement status.")
    };

    public static Result<AnnouncementStatus> Parse(string? raw)
    {
        return raw?.Trim().ToUpperInvariant() switch
        {
            "DRAFT" => Result<AnnouncementStatus>.Success(AnnouncementStatus.Draft),
            "PUBLISHED" => Result<AnnouncementStatus>.Success(AnnouncementStatus.Published),
            _ => Result<AnnouncementStatus>.Failure(OprErrors.InvalidAnnouncementStatus(raw))
        };
    }
}
