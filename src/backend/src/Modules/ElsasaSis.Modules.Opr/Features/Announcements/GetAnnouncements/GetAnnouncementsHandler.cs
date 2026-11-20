using Dapper;
using ElsasaSis.SharedKernel.Pagination;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Opr.Features.Announcements.GetAnnouncements;

public sealed class GetAnnouncementsHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    public async Task<PaginatedResponse<AnnouncementDto>> HandleAsync(
        GetAnnouncementsQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, offset) = OprQueryHelper.NormalizePagination(query.Page, query.PageSize);
        var searchPattern = OprQueryHelper.ToSearchPattern(query.Search);
        var normalizedAudience = string.IsNullOrWhiteSpace(query.TargetAudience)
            ? null
            : query.TargetAudience.Trim().ToUpperInvariant();
        var normalizedStatus = string.IsNullOrWhiteSpace(query.Status)
            ? null
            : query.Status.Trim().ToUpperInvariant();
        var orderBy = ResolveOrderBy(query.SortField, query.SortOrder);

        var hasCreateOrPublish = AnnouncementAccessPolicy.HasCreateOrPublish(currentUser);
        var isTeacherOnly = AnnouncementAccessPolicy.IsTeacherOrHomeroomOnly(currentUser);
        var isParentOnly = AnnouncementAccessPolicy.IsParentOnly(currentUser);
        var isStudentOnly = AnnouncementAccessPolicy.IsStudentOnly(currentUser);

        var conditions = new List<string>();

        if (query.AcademicYearId.HasValue)
        {
            conditions.Add("a.academic_year_id = @AcademicYearId");
        }

        if (isParentOnly)
        {
            conditions.Add("a.status = 'PUBLISHED'");
            conditions.Add("a.target_audience IN ('ALL', 'PARENTS')");
        }
        else if (isStudentOnly)
        {
            conditions.Add("a.status = 'PUBLISHED'");
            conditions.Add("a.target_audience IN ('ALL', 'STUDENTS')");
        }
        else
        {
            if (!hasCreateOrPublish)
            {
                // Read-only callers lacking Create/Publish can only see PUBLISHED announcements.
                conditions.Add("a.status = 'PUBLISHED'");
            }
            else if (normalizedStatus is not null)
            {
                conditions.Add("a.status = @Status");
            }

            if (isTeacherOnly)
            {
                // Teacher/Homeroom-only callers are scoped to ALL and TEACHERS announcements.
                conditions.Add("a.target_audience IN ('ALL', 'TEACHERS')");
            }
        }

        if (normalizedAudience is not null)
        {
            conditions.Add("a.target_audience = @TargetAudience");
        }

        if (searchPattern is not null)
        {
            conditions.Add("(a.title LIKE @SearchPattern ESCAPE '\\' OR a.content LIKE @SearchPattern ESCAPE '\\' OR a.author_name LIKE @SearchPattern ESCAPE '\\')");
        }

        var whereClause = conditions.Count == 0
            ? string.Empty
            : $"WHERE {string.Join(" AND ", conditions)}";

        var countSql = $"""
            SELECT COUNT(1)
            FROM opr.announcements a
            {whereClause};
            """;

        var itemsSql = $"""
            SELECT
                a.id,
                a.academic_year_id,
                COALESCE(ay.name, N'') AS academic_year_name,
                a.title,
                a.content,
                a.author_id,
                a.author_name,
                a.target_audience,
                a.status,
                a.published_at,
                a.created_at,
                a.updated_at
            FROM opr.announcements a
            LEFT JOIN opr.academic_years ay ON ay.id = a.academic_year_id
            {whereClause}
            ORDER BY {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            query.AcademicYearId,
            Status = normalizedStatus,
            TargetAudience = normalizedAudience,
            SearchPattern = searchPattern,
            Offset = offset,
            PageSize = pageSize
        };

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        if (totalCount == 0)
        {
            return new PaginatedResponse<AnnouncementDto>([], page, pageSize, 0);
        }

        var rows = await connection.QueryAsync<AnnouncementRow>(
            new CommandDefinition(itemsSql, parameters, cancellationToken: cancellationToken));

        var items = rows
            .Select(r => new AnnouncementDto(
                r.id,
                r.academic_year_id,
                r.academic_year_name,
                r.title,
                r.content,
                r.author_id,
                r.author_name,
                r.target_audience,
                r.status,
                r.published_at.HasValue ? DateTime.SpecifyKind(r.published_at.Value, DateTimeKind.Utc) : null,
                DateTime.SpecifyKind(r.created_at, DateTimeKind.Utc),
                DateTime.SpecifyKind(r.updated_at, DateTimeKind.Utc)))
            .ToArray();

        return new PaginatedResponse<AnnouncementDto>(items, page, pageSize, totalCount);
    }

    private static string ResolveOrderBy(string? sortField, string? sortOrder)
    {
        var direction = OprQueryHelper.ResolveSortDirection(sortOrder);

        return sortField?.Trim().ToLowerInvariant() switch
        {
            "title" => $"a.title {direction ?? "ASC"}, a.created_at DESC",
            "status" => $"a.status {direction ?? "ASC"}, a.created_at DESC",
            "targetaudience" or "target_audience" => $"a.target_audience {direction ?? "ASC"}, a.created_at DESC",
            "publishedat" or "published_at" => $"a.published_at {direction ?? "DESC"}, a.created_at DESC",
            "createdat" or "created_at" => $"a.created_at {direction ?? "DESC"}",
            "updatedat" or "updated_at" => $"a.updated_at {direction ?? "DESC"}",
            _ => "COALESCE(a.published_at, a.created_at) DESC, a.created_at DESC"
        };
    }

    private sealed class AnnouncementRow
    {
        public Guid id { get; init; }
        public Guid academic_year_id { get; init; }
        public string academic_year_name { get; init; } = string.Empty;
        public string title { get; init; } = string.Empty;
        public string content { get; init; } = string.Empty;
        public Guid author_id { get; init; }
        public string author_name { get; init; } = string.Empty;
        public string target_audience { get; init; } = string.Empty;
        public string status { get; init; } = string.Empty;
        public DateTime? published_at { get; init; }
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
