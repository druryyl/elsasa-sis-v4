using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Domain;

public enum AnnouncementAudience
{
    All = 0,
    Teachers = 1,
    Students = 2,
    Parents = 3
}

public static class AnnouncementAudienceExtensions
{
    public static string ToCode(this AnnouncementAudience audience) => audience switch
    {
        AnnouncementAudience.All => "ALL",
        AnnouncementAudience.Teachers => "TEACHERS",
        AnnouncementAudience.Students => "STUDENTS",
        AnnouncementAudience.Parents => "PARENTS",
        _ => throw new ArgumentOutOfRangeException(nameof(audience), audience, "Unsupported announcement audience.")
    };

    public static Result<AnnouncementAudience> Parse(string? raw)
    {
        return raw?.Trim().ToUpperInvariant() switch
        {
            "ALL" => Result<AnnouncementAudience>.Success(AnnouncementAudience.All),
            "TEACHERS" => Result<AnnouncementAudience>.Success(AnnouncementAudience.Teachers),
            "STUDENTS" => Result<AnnouncementAudience>.Success(AnnouncementAudience.Students),
            "PARENTS" => Result<AnnouncementAudience>.Success(AnnouncementAudience.Parents),
            _ => Result<AnnouncementAudience>.Failure(OprErrors.InvalidAnnouncementAudience(raw))
        };
    }
}
