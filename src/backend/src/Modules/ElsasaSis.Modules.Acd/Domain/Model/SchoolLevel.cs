namespace ElsasaSis.Modules.Acd.Domain.Model;

public static class SchoolLevel
{
    public const string Sd = "SD";
    public const string Smp = "SMP";
    public const string Sma = "SMA";
    public const string Smk = "SMK";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Sd,
        Smp,
        Sma,
        Smk
    };

    public static bool IsValid(string? schoolLevel) =>
        !string.IsNullOrWhiteSpace(schoolLevel) && All.Contains(schoolLevel.Trim());

    public static string Normalize(string schoolLevel) => schoolLevel.Trim().ToUpperInvariant();

    public static bool IsValidCombination(string schoolLevel, string grade)
    {
        if (string.IsNullOrWhiteSpace(schoolLevel) || string.IsNullOrWhiteSpace(grade))
        {
            return false;
        }

        var level = Normalize(schoolLevel);
        var g = grade.Trim();

        return level switch
        {
            Sd => int.TryParse(g, out var n) && n is >= 1 and <= 6,
            Smp => int.TryParse(g, out var n) && n is >= 7 and <= 9,
            Sma or Smk => int.TryParse(g, out var n) && n is >= 10 and <= 12,
            _ => false
        };
    }
}
