using Dapper;
using ElsasaSis.SharedKernel.Pagination;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Opr.Features.AcademicYears.GetAcademicYears;

public sealed class GetAcademicYearsHandler(ISqlConnectionFactory connectionFactory)
{
    public async Task<PaginatedResponse<AcademicYearDto>> HandleAsync(
        PagedQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;
        var offset = query.Offset;

        var searchPattern = string.IsNullOrWhiteSpace(query.Search)
            ? null
            : $"%{EscapeLikePattern(query.Search.Trim())}%";

        var orderBy = ResolveOrderBy(query.SortField, query.SortOrder);

        var whereClause = searchPattern is null
            ? string.Empty
            : "WHERE name LIKE @SearchPattern ESCAPE '\\'";

        var countSql = $"""
            SELECT COUNT(1)
            FROM opr.academic_years
            {whereClause};
            """;

        var yearsSql = $"""
            SELECT id, name, start_date, end_date, status, created_at, updated_at
            FROM opr.academic_years
            {whereClause}
            ORDER BY {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var parameters = new
        {
            SearchPattern = searchPattern,
            Offset = offset,
            PageSize = pageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        if (totalCount == 0)
        {
            return new PaginatedResponse<AcademicYearDto>([], page, pageSize, 0);
        }

        var yearRows = (await connection.QueryAsync<AcademicYearRow>(
            new CommandDefinition(yearsSql, parameters, cancellationToken: cancellationToken))).ToList();

        if (yearRows.Count == 0)
        {
            return new PaginatedResponse<AcademicYearDto>([], page, pageSize, totalCount);
        }

        var yearIds = yearRows.Select(y => y.id).ToArray();

        const string periodsSql = """
            SELECT id, academic_year_id, name, start_date, end_date, status, created_at, updated_at
            FROM opr.operational_periods
            WHERE academic_year_id IN @AcademicYearIds
            ORDER BY start_date ASC, end_date ASC, name ASC;
            """;

        var periodRows = await connection.QueryAsync<OperationalPeriodRow>(
            new CommandDefinition(periodsSql, new { AcademicYearIds = yearIds }, cancellationToken: cancellationToken));

        var periodsByYear = periodRows
            .GroupBy(p => p.academic_year_id)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<OperationalPeriodDto>)g
                    .Select(p => new OperationalPeriodDto(
                        p.id,
                        p.academic_year_id,
                        p.name,
                        DateOnly.FromDateTime(p.start_date),
                        DateOnly.FromDateTime(p.end_date),
                        p.status,
                        p.created_at,
                        p.updated_at))
                    .ToArray());

        var items = yearRows
            .Select(y => new AcademicYearDto(
                y.id,
                y.name,
                DateOnly.FromDateTime(y.start_date),
                DateOnly.FromDateTime(y.end_date),
                y.status,
                y.created_at,
                y.updated_at,
                periodsByYear.TryGetValue(y.id, out var periods) ? periods : []))
            .ToArray();

        return new PaginatedResponse<AcademicYearDto>(items, page, pageSize, totalCount);
    }

    private static string ResolveOrderBy(string? sortField, string? sortOrder)
    {
        var direction = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase)
            ? "ASC"
            : string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                ? "DESC"
                : null;

        return sortField?.Trim().ToLowerInvariant() switch
        {
            "name" => $"name {direction ?? "ASC"}, start_date DESC",
            "startdate" or "start_date" => $"start_date {direction ?? "DESC"}, name ASC",
            "enddate" or "end_date" => $"end_date {direction ?? "DESC"}, name ASC",
            "status" => $"status {direction ?? "ASC"}, start_date DESC",
            "createdat" or "created_at" => $"created_at {direction ?? "DESC"}",
            _ => "start_date DESC, name ASC"
        };
    }

    private static string EscapeLikePattern(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);

    private sealed record AcademicYearRow(
        Guid id,
        string name,
        DateTime start_date,
        DateTime end_date,
        string status,
        DateTime created_at,
        DateTime updated_at);

    private sealed record OperationalPeriodRow(
        Guid id,
        Guid academic_year_id,
        string name,
        DateTime start_date,
        DateTime end_date,
        string status,
        DateTime created_at,
        DateTime updated_at);
}
