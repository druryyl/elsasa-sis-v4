using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Std.Domain;

public static class StdDomainParsing
{
    public static Result<Gender> ParseGender(string? raw, string field = "gender") =>
        TryParseNonNumericEnum<Gender>(raw, out var value)
            ? Result<Gender>.Success(value)
            : Result<Gender>.Failure(StdErrors.GenderInvalid(raw, field));

    public static Result<SchoolLevel> ParseSchoolLevel(string? raw, string field = "schoolLevel") =>
        TryParseNonNumericEnum<SchoolLevel>(raw, out var value)
            ? Result<SchoolLevel>.Success(value)
            : Result<SchoolLevel>.Failure(StdErrors.SchoolLevelInvalid(raw, field));

    public static Result<GuardianRelationshipType> ParseRelationshipType(
        string? raw,
        string field = "relationshipType") =>
        TryParseNonNumericEnum<GuardianRelationshipType>(raw, out var value)
            ? Result<GuardianRelationshipType>.Success(value)
            : Result<GuardianRelationshipType>.Failure(StdErrors.GuardianRelationshipTypeInvalid(raw, field));

    public static string ToCode(this AdmissionStatus status) => status.ToString().ToUpperInvariant();
    public static string ToCode(this Gender gender) => gender.ToString().ToUpperInvariant();
    public static string ToCode(this SchoolLevel schoolLevel) => schoolLevel.ToString().ToUpperInvariant();
    public static string ToCode(this GuardianRelationshipType relationshipType) => relationshipType.ToString().ToUpperInvariant();
    public static string ToCode(this StudentStatus status) => status.ToString().ToUpperInvariant();

    internal static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TryParseNonNumericEnum<TEnum>(string? raw, out TEnum value)
        where TEnum : struct, Enum
    {
        value = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim();
        if (long.TryParse(trimmed, out _))
        {
            return false;
        }

        return Enum.TryParse(trimmed, ignoreCase: true, out value) && Enum.IsDefined(value);
    }
}
