using System.Data;
using Dapper;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Pagination;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Std.Features.Students;

internal static class StudentDirectoryAccess
{
    public static bool HasDirectoryView(ICurrentUser currentUser) =>
        currentUser.Permissions.Contains(Permissions.Students.Directory.View, StringComparer.Ordinal) ||
        currentUser.Permissions.Contains(Permissions.Students.Directory.Manage, StringComparer.Ordinal);

    public static bool HasRosterViewOwn(ICurrentUser currentUser) =>
        currentUser.Permissions.Contains(Permissions.Students.Roster.ViewOwn, StringComparer.Ordinal);

    public static Result<Guid?> ResolveScopedTeacherId(ICurrentUser currentUser)
    {
        if (HasDirectoryView(currentUser))
        {
            return Result<Guid?>.Success(null);
        }

        if (!HasRosterViewOwn(currentUser))
        {
            return Result<Guid?>.Failure(DomainError.Forbidden(
                "Std.Directory.Forbidden",
                "You do not have permission to access student directory or roster data."));
        }

        if (!currentUser.TeacherId.HasValue || currentUser.TeacherId.Value == Guid.Empty)
        {
            return Result<Guid?>.Failure(DomainError.Forbidden(
                "Std.Roster.TeacherProfileRequired",
                "A linked teacher profile is required to view homeroom roster data."));
        }

        return Result<Guid?>.Success(currentUser.TeacherId.Value);
    }

    public static DomainError StudentRosterForbidden(Guid studentId) =>
        DomainError.Forbidden(
            "Std.Student.RosterForbidden",
            $"You are not assigned as homeroom teacher for student '{studentId}'.");

    public static DomainError StudyGroupRosterForbidden(Guid studyGroupId) =>
        DomainError.Forbidden(
            "Std.StudyGroup.RosterForbidden",
            $"You are not assigned as homeroom teacher for study group '{studyGroupId}'.");
}

internal static class StudentReadQueryHelper
{
    private const string SelectGuardiansByStudentIdsSql = """
        SELECT id, student_id, full_name, relationship_type, phone, email, address, created_at, updated_at
        FROM std.student_guardians
        WHERE student_id IN @StudentIds
        ORDER BY CASE WHEN phone IS NOT NULL AND LTRIM(RTRIM(phone)) <> N'' THEN 0 ELSE 1 END ASC,
                 relationship_type ASC,
                 full_name ASC,
                 id ASC;
        """;

    public static (int Page, int PageSize, int Offset) NormalizePagination(int page, int pageSize)
    {
        var query = new PagedQuery(page, pageSize);
        return (query.NormalizedPage, query.NormalizedPageSize, query.Offset);
    }

    public static string? ToSearchPattern(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var escaped = search.Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);

        return $"%{escaped}%";
    }

    public static string? NormalizeCodeFilter(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    public static string? NormalizeTextFilter(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string? ResolveSortDirection(string? sortOrder) =>
        string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase)
            ? "ASC"
            : string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                ? "DESC"
                : null;

    public static DateTime SpecifyUtc(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static async Task<Dictionary<Guid, IReadOnlyList<StudentGuardianDto>>> LoadGuardiansByStudentIdsAsync(
        IDbConnection connection,
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken cancellationToken)
    {
        if (studentIds.Count == 0)
        {
            return [];
        }

        var rows = await connection.QueryAsync<StudentGuardianRow>(new CommandDefinition(
            SelectGuardiansByStudentIdsSql,
            new { StudentIds = studentIds },
            cancellationToken: cancellationToken));

        return rows
            .GroupBy(r => r.student_id)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<StudentGuardianDto>)g
                    .Select(r => new StudentGuardianDto(
                        r.id,
                        r.student_id,
                        r.full_name,
                        r.relationship_type,
                        r.phone,
                        r.email,
                        r.address,
                        SpecifyUtc(r.created_at),
                        SpecifyUtc(r.updated_at)))
                    .ToArray());
    }

    private sealed class StudentGuardianRow
    {
        public Guid id { get; init; }
        public Guid student_id { get; init; }
        public string full_name { get; init; } = string.Empty;
        public string relationship_type { get; init; } = string.Empty;
        public string? phone { get; init; }
        public string? email { get; init; }
        public string? address { get; init; }
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
