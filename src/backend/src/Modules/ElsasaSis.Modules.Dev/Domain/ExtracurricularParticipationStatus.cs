namespace ElsasaSis.Modules.Dev.Domain;

public static class ExtracurricularParticipationStatus
{
    public const string Active = "ACTIVE";
    public const string Completed = "COMPLETED";
    public const string Withdrawn = "WITHDRAWN";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Active,
        Completed,
        Withdrawn
    };

    public static bool IsValid(string? value) =>
        value is not null && All.Contains(value.Trim());

    public static string Normalize(string value) =>
        value.Trim().ToUpperInvariant();
}
