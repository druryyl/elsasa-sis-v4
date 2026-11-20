namespace ElsasaSis.Modules.Dev.Domain;

public static class BehaviourType
{
    public const string Positive = "POSITIVE";
    public const string Negative = "NEGATIVE";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Positive,
        Negative
    };

    public static bool IsValid(string? value) =>
        value is not null && All.Contains(value.Trim());

    public static string Normalize(string value) =>
        value.Trim().ToUpperInvariant();
}
