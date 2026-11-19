using ElsasaSis.SharedKernel.Pagination;

namespace ElsasaSis.Modules.Std.Features.Admissions;

internal static class AdmissionsQueryHelper
{
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

    public static string? ResolveSortDirection(string? sortOrder) =>
        string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase)
            ? "ASC"
            : string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                ? "DESC"
                : null;

    public static DateTime SpecifyUtc(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static DateTime? SpecifyNullableUtc(DateTime? value) =>
        value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
}
