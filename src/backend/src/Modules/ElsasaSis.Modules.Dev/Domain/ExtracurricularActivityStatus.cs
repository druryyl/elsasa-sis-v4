namespace ElsasaSis.Modules.Dev.Domain;

public static class ExtracurricularActivityStatus
{
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Active,
        Inactive
    };

    public static bool IsValid(string? value) =>
        value is not null && All.Contains(value.Trim());

    public static string Normalize(string value) =>
        value.Trim().ToUpperInvariant();
}
