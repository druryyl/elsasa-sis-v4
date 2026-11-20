namespace ElsasaSis.Modules.Dev.Domain;

public static class RecognitionLevel
{
    public const string School = "SCHOOL";
    public const string City = "CITY";
    public const string Province = "PROVINCE";
    public const string National = "NATIONAL";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        School,
        City,
        Province,
        National
    };

    public static bool IsValid(string? value) =>
        value is not null && All.Contains(value.Trim());

    public static string Normalize(string value) =>
        value.Trim().ToUpperInvariant();
}
