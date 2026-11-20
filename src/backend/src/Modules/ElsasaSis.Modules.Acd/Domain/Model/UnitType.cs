namespace ElsasaSis.Modules.Acd.Domain.Model;

public static class UnitType
{
    public const string Module = "MODULE";
    public const string Pace = "PACE";
    public const string Lesson = "LESSON";
    public const string Presentation = "PRESENTATION";
    public const string Book = "BOOK";
    public const string Other = "OTHER";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Module,
        Pace,
        Lesson,
        Presentation,
        Book,
        Other
    };

    public static bool IsValid(string? unitType) =>
        !string.IsNullOrWhiteSpace(unitType) && All.Contains(unitType.Trim());

    public static string Normalize(string unitType) => unitType.Trim().ToUpperInvariant();
}
