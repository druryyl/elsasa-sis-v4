using Dapper;
using ElsasaSis.SharedKernel.Pagination;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Opr.Features.CalendarEntries.GetCalendarEntries;

public sealed class GetCalendarEntriesHandler(ISqlConnectionFactory connectionFactory)
{
    public async Task<PaginatedResponse<CalendarEntryDto>> HandleAsync(
        GetCalendarEntriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, offset) = OprQueryHelper.NormalizePagination(query.Page, query.PageSize);
        var searchPattern = OprQueryHelper.ToSearchPattern(query.Search);
        var normalizedDayType = string.IsNullOrWhiteSpace(query.DayType)
            ? null
            : query.DayType.Trim().ToUpperInvariant();
        var orderBy = ResolveOrderBy(query.SortField, query.SortOrder);

        var conditions = new List<string>();
        if (query.AcademicYearId.HasValue)
        {
            conditions.Add("ce.academic_year_id = @AcademicYearId");
        }

        if (normalizedDayType is not null)
        {
            conditions.Add("ce.day_type = @DayType");
        }

        if (query.FromDate.HasValue)
        {
            conditions.Add("ce.entry_date >= @FromDate");
        }

        if (query.ToDate.HasValue)
        {
            conditions.Add("ce.entry_date <= @ToDate");
        }

        if (searchPattern is not null)
        {
            conditions.Add("(ce.description LIKE @SearchPattern ESCAPE '\\' OR ay.name LIKE @SearchPattern ESCAPE '\\')");
        }

        var whereClause = conditions.Count == 0
            ? string.Empty
            : $"WHERE {string.Join(" AND ", conditions)}";

        var countSql = $"""
            SELECT COUNT(1)
            FROM opr.calendar_entries ce
            LEFT JOIN opr.academic_years ay ON ay.id = ce.academic_year_id
            {whereClause};
            """;

        var itemsSql = $"""
            SELECT
                ce.id,
                ce.academic_year_id,
                COALESCE(ay.name, N'') AS academic_year_name,
                ce.entry_date,
                ce.day_type,
                ce.description,
                ce.created_at,
                ce.updated_at
            FROM opr.calendar_entries ce
            LEFT JOIN opr.academic_years ay ON ay.id = ce.academic_year_id
            {whereClause}
            ORDER BY {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            query.AcademicYearId,
            DayType = normalizedDayType,
            FromDate = query.FromDate?.ToDateTime(TimeOnly.MinValue),
            ToDate = query.ToDate?.ToDateTime(TimeOnly.MinValue),
            SearchPattern = searchPattern,
            Offset = offset,
            PageSize = pageSize
        };

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        if (totalCount == 0)
        {
            return new PaginatedResponse<CalendarEntryDto>([], page, pageSize, 0);
        }

        var rows = await connection.QueryAsync<CalendarEntryRow>(
            new CommandDefinition(itemsSql, parameters, cancellationToken: cancellationToken));

        var items = rows
            .Select(r => new CalendarEntryDto(
                r.id,
                r.academic_year_id,
                r.academic_year_name,
                DateOnly.FromDateTime(r.entry_date),
                r.day_type,
                r.description,
                DateTime.SpecifyKind(r.created_at, DateTimeKind.Utc),
                DateTime.SpecifyKind(r.updated_at, DateTimeKind.Utc)))
            .ToArray();

        return new PaginatedResponse<CalendarEntryDto>(items, page, pageSize, totalCount);
    }

    private static string ResolveOrderBy(string? sortField, string? sortOrder)
    {
        var direction = OprQueryHelper.ResolveSortDirection(sortOrder);

        return sortField?.Trim().ToLowerInvariant() switch
        {
            "entrydate" or "entry_date" => $"ce.entry_date {direction ?? "ASC"}, ce.created_at ASC",
            "daytype" or "day_type" => $"ce.day_type {direction ?? "ASC"}, ce.entry_date ASC",
            "description" => $"ce.description {direction ?? "ASC"}, ce.entry_date ASC",
            "createdat" or "created_at" => $"ce.created_at {direction ?? "DESC"}",
            _ => "ce.entry_date ASC, ce.created_at ASC"
        };
    }

    private sealed class CalendarEntryRow
    {
        public Guid id { get; init; }
        public Guid academic_year_id { get; init; }
        public string academic_year_name { get; init; } = string.Empty;
        public DateTime entry_date { get; init; }
        public string day_type { get; init; } = string.Empty;
        public string? description { get; init; }
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
