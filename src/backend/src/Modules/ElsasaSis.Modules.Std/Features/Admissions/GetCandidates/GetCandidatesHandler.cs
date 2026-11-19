using Dapper;
using ElsasaSis.SharedKernel.Pagination;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Std.Features.Admissions.GetCandidates;

public sealed class GetCandidatesHandler(ISqlConnectionFactory connectionFactory)
{
    public async Task<PaginatedResponse<CandidateSummaryDto>> HandleAsync(
        GetCandidatesQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, offset) = AdmissionsQueryHelper.NormalizePagination(query.Page, query.PageSize);
        var searchPattern = AdmissionsQueryHelper.ToSearchPattern(query.Search);
        var normalizedStatus = string.IsNullOrWhiteSpace(query.Status)
            ? null
            : query.Status.Trim().ToUpperInvariant();
        var normalizedSchoolLevel = string.IsNullOrWhiteSpace(query.SchoolLevel)
            ? null
            : query.SchoolLevel.Trim().ToUpperInvariant();
        var normalizedGrade = string.IsNullOrWhiteSpace(query.Grade)
            ? null
            : query.Grade.Trim();
        var orderBy = ResolveOrderBy(query.SortField, query.SortOrder);

        var conditions = new List<string>();

        if (query.AdmissionYearId.HasValue)
        {
            conditions.Add("c.admission_year_id = @AdmissionYearId");
        }

        if (normalizedStatus is not null)
        {
            conditions.Add("c.admission_status = @Status");
        }

        if (normalizedSchoolLevel is not null)
        {
            conditions.Add("c.target_school_level = @SchoolLevel");
        }

        if (normalizedGrade is not null)
        {
            conditions.Add("c.target_grade = @Grade");
        }

        if (searchPattern is not null)
        {
            conditions.Add("""
                (
                    c.full_name LIKE @SearchPattern ESCAPE '\'
                    OR c.nickname LIKE @SearchPattern ESCAPE '\'
                    OR c.phone LIKE @SearchPattern ESCAPE '\'
                    OR c.email LIKE @SearchPattern ESCAPE '\'
                )
                """);
        }

        var whereClause = conditions.Count == 0
            ? string.Empty
            : $"WHERE {string.Join(" AND ", conditions)}";

        var countSql = $"""
            SELECT COUNT(1)
            FROM std.candidates c
            {whereClause};
            """;

        var itemsSql = $"""
            SELECT
                c.id,
                c.admission_year_id,
                c.admission_year_name,
                c.full_name,
                c.nickname,
                c.date_of_birth,
                c.gender,
                c.phone,
                c.email,
                c.address,
                c.target_school_level,
                c.target_grade,
                c.admission_status,
                c.registered_at,
                c.decided_at,
                c.decision_notes,
                c.enrolled_student_id,
                c.created_at,
                c.updated_at
            FROM std.candidates c
            {whereClause}
            ORDER BY {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            query.AdmissionYearId,
            Status = normalizedStatus,
            SchoolLevel = normalizedSchoolLevel,
            Grade = normalizedGrade,
            SearchPattern = searchPattern,
            Offset = offset,
            PageSize = pageSize
        };

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        if (totalCount == 0)
        {
            return new PaginatedResponse<CandidateSummaryDto>([], page, pageSize, 0);
        }

        var rows = await connection.QueryAsync<CandidateRow>(
            new CommandDefinition(itemsSql, parameters, cancellationToken: cancellationToken));

        var items = rows
            .Select(r => new CandidateSummaryDto(
                r.id,
                r.admission_year_id,
                r.admission_year_name,
                r.full_name,
                r.nickname,
                DateOnly.FromDateTime(r.date_of_birth),
                r.gender,
                r.phone,
                r.email,
                r.address,
                r.target_school_level,
                r.target_grade,
                r.admission_status,
                AdmissionsQueryHelper.SpecifyUtc(r.registered_at),
                AdmissionsQueryHelper.SpecifyNullableUtc(r.decided_at),
                r.decision_notes,
                r.enrolled_student_id,
                AdmissionsQueryHelper.SpecifyUtc(r.created_at),
                AdmissionsQueryHelper.SpecifyUtc(r.updated_at)))
            .ToArray();

        return new PaginatedResponse<CandidateSummaryDto>(items, page, pageSize, totalCount);
    }

    private static string ResolveOrderBy(string? sortField, string? sortOrder)
    {
        var direction = AdmissionsQueryHelper.ResolveSortDirection(sortOrder);

        return sortField?.Trim().ToLowerInvariant() switch
        {
            "fullname" or "full_name" or "name" => $"c.full_name {direction ?? "ASC"}, c.registered_at DESC",
            "status" or "admissionstatus" or "admission_status" => $"c.admission_status {direction ?? "ASC"}, c.registered_at DESC",
            "schoollevel" or "targetschoollevel" or "target_school_level" => $"c.target_school_level {direction ?? "ASC"}, c.target_grade ASC, c.full_name ASC",
            "grade" or "targetgrade" or "target_grade" => $"c.target_grade {direction ?? "ASC"}, c.full_name ASC",
            "registeredat" or "registered_at" => $"c.registered_at {direction ?? "DESC"}, c.full_name ASC",
            "decidedat" or "decided_at" => $"c.decided_at {direction ?? "DESC"}, c.full_name ASC",
            _ => "c.registered_at DESC, c.full_name ASC"
        };
    }

    private sealed class CandidateRow
    {
        public Guid id { get; init; }
        public Guid admission_year_id { get; init; }
        public string admission_year_name { get; init; } = string.Empty;
        public string full_name { get; init; } = string.Empty;
        public string? nickname { get; init; }
        public DateTime date_of_birth { get; init; }
        public string gender { get; init; } = string.Empty;
        public string? phone { get; init; }
        public string? email { get; init; }
        public string? address { get; init; }
        public string target_school_level { get; init; } = string.Empty;
        public string target_grade { get; init; } = string.Empty;
        public string admission_status { get; init; } = string.Empty;
        public DateTime registered_at { get; init; }
        public DateTime? decided_at { get; init; }
        public string? decision_notes { get; init; }
        public Guid? enrolled_student_id { get; init; }
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
