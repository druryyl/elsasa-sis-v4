using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Domain.Model;

public enum AssignmentType
{
    Formative = 1,
    Summative = 2,
    Exam = 3
}

public static class AssignmentTypeExtensions
{
    public static string ToCode(this AssignmentType type) => type switch
    {
        AssignmentType.Formative => "FORMATIVE",
        AssignmentType.Summative => "SUMMATIVE",
        AssignmentType.Exam => "EXAM",
        _ => type.ToString().ToUpperInvariant()
    };

    public static Result<AssignmentType> ParseAssignmentType(string? raw, string field = "assignmentType")
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Result<AssignmentType>.Failure(ScorebookErrors.InvalidAssignmentType(raw, field));
        }

        var normalized = raw.Trim().ToUpperInvariant();
        return normalized switch
        {
            "FORMATIVE" => Result<AssignmentType>.Success(AssignmentType.Formative),
            "SUMMATIVE" => Result<AssignmentType>.Success(AssignmentType.Summative),
            "EXAM" => Result<AssignmentType>.Success(AssignmentType.Exam),
            _ => Result<AssignmentType>.Failure(ScorebookErrors.InvalidAssignmentType(raw, field))
        };
    }
}
