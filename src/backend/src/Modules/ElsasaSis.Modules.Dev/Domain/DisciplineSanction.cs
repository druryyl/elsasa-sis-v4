namespace ElsasaSis.Modules.Dev.Domain;

public static class DisciplineSanction
{
    public const string VerbalWarning = "VERBAL_WARNING";
    public const string WrittenWarning = "WRITTEN_WARNING";
    public const string ParentMeeting = "PARENT_MEETING";
    public const string Suspension = "SUSPENSION";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        VerbalWarning,
        WrittenWarning,
        ParentMeeting,
        Suspension
    };

    public static readonly IReadOnlySet<string> TeacherAllowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        VerbalWarning,
        WrittenWarning
    };

    public static readonly IReadOnlySet<string> PrincipalAllowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ParentMeeting,
        Suspension
    };

    public static bool IsValid(string? value) =>
        value is not null && All.Contains(value.Trim());

    public static bool IsAllowedForTeacher(string? value) =>
        value is not null && TeacherAllowed.Contains(value.Trim());

    public static bool IsAllowedForPrincipal(string? value) =>
        value is not null && PrincipalAllowed.Contains(value.Trim());

    public static string Normalize(string value) =>
        value.Trim().ToUpperInvariant();
}
