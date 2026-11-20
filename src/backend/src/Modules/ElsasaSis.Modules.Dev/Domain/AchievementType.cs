namespace ElsasaSis.Modules.Dev.Domain;

public static class AchievementType
{
    public const string Sports = "SPORTS";
    public const string Arts = "ARTS";
    public const string Science = "SCIENCE";
    public const string Community = "COMMUNITY";
    public const string Other = "OTHER";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Sports,
        Arts,
        Science,
        Community,
        Other
    };

    public static bool IsValid(string? value) =>
        value is not null && All.Contains(value.Trim());

    public static string Normalize(string value) =>
        value.Trim().ToUpperInvariant();
}
