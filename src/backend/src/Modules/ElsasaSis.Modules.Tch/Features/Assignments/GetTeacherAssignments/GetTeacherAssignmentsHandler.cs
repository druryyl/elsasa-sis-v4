using Dapper;
using ElsasaSis.SharedKernel.Pagination;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Tch.Features.Assignments.GetTeacherAssignments;

public sealed class GetTeacherAssignmentsHandler(ISqlConnectionFactory connectionFactory)
{
    public async Task<PaginatedResponse<TeacherAssignmentDto>> HandleAsync(
        GetTeacherAssignmentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, offset) = TchQueryHelper.NormalizePagination(query.Page, query.PageSize);
        var searchPattern = TchQueryHelper.ToSearchPattern(query.Search);
        var normalizedStatus = string.IsNullOrWhiteSpace(query.Status)
            ? null
            : query.Status.Trim().ToUpperInvariant();
        var orderBy = ResolveOrderBy(query.SortField, query.SortOrder);

        var conditions = new List<string>();
        if (query.OperationalPeriodId.HasValue)
        {
            conditions.Add("ta.operational_period_id = @OperationalPeriodId");
        }

        if (query.AcademicYearId.HasValue)
        {
            conditions.Add("ta.academic_year_id = @AcademicYearId");
        }

        if (query.StudyGroupId.HasValue)
        {
            conditions.Add("ta.study_group_id = @StudyGroupId");
        }

        if (query.TeacherId.HasValue)
        {
            conditions.Add("ta.teacher_id = @TeacherId");
        }

        if (query.SubjectId.HasValue)
        {
            conditions.Add("ta.subject_id = @SubjectId");
        }

        if (normalizedStatus is not null)
        {
            conditions.Add("ta.status = @Status");
        }

        if (searchPattern is not null)
        {
            conditions.Add("""
                (
                    ta.teacher_name LIKE @SearchPattern ESCAPE '\'
                    OR ta.subject_name LIKE @SearchPattern ESCAPE '\'
                    OR ta.study_group_name LIKE @SearchPattern ESCAPE '\'
                )
                """);
        }

        var whereClause = conditions.Count == 0
            ? string.Empty
            : $"WHERE {string.Join(" AND ", conditions)}";

        var countSql = $"""
            SELECT COUNT(1)
            FROM tch.teacher_assignments ta
            {whereClause};
            """;

        var itemsSql = $"""
            SELECT
                ta.id,
                ta.teacher_id,
                ta.teacher_name,
                ta.academic_year_id,
                ta.academic_year_name,
                ta.operational_period_id,
                ta.operational_period_name,
                ta.subject_id,
                ta.subject_name,
                ta.study_group_id,
                ta.study_group_name,
                ta.status,
                ta.created_at,
                ta.updated_at
            FROM tch.teacher_assignments ta
            {whereClause}
            ORDER BY {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            query.OperationalPeriodId,
            query.AcademicYearId,
            query.StudyGroupId,
            query.TeacherId,
            query.SubjectId,
            Status = normalizedStatus,
            SearchPattern = searchPattern,
            Offset = offset,
            PageSize = pageSize
        };

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        if (totalCount == 0)
        {
            return new PaginatedResponse<TeacherAssignmentDto>([], page, pageSize, 0);
        }

        var rows = await connection.QueryAsync<TeacherAssignmentRow>(
            new CommandDefinition(itemsSql, parameters, cancellationToken: cancellationToken));

        var items = rows
            .Select(a => new TeacherAssignmentDto(
                a.id,
                a.teacher_id,
                a.teacher_name,
                a.academic_year_id,
                a.academic_year_name,
                a.operational_period_id,
                a.operational_period_name,
                a.subject_id,
                a.subject_name,
                a.study_group_id,
                a.study_group_name,
                a.status,
                DateTime.SpecifyKind(a.created_at, DateTimeKind.Utc),
                DateTime.SpecifyKind(a.updated_at, DateTimeKind.Utc)))
            .ToArray();

        return new PaginatedResponse<TeacherAssignmentDto>(items, page, pageSize, totalCount);
    }

    private static string ResolveOrderBy(string? sortField, string? sortOrder)
    {
        var direction = TchQueryHelper.ResolveSortDirection(sortOrder);

        return sortField?.Trim().ToLowerInvariant() switch
        {
            "teachername" or "teacher_name" => $"ta.teacher_name {direction ?? "ASC"}, ta.study_group_name ASC, ta.subject_name ASC",
            "subjectname" or "subject_name" => $"ta.subject_name {direction ?? "ASC"}, ta.study_group_name ASC, ta.teacher_name ASC",
            "studygroupname" or "study_group_name" => $"ta.study_group_name {direction ?? "ASC"}, ta.subject_name ASC, ta.teacher_name ASC",
            "status" => $"ta.status {direction ?? "ASC"}, ta.study_group_name ASC, ta.subject_name ASC",
            "createdat" or "created_at" => $"ta.created_at {direction ?? "DESC"}",
            _ => "ta.academic_year_name DESC, ta.operational_period_name ASC, ta.study_group_name ASC, ta.subject_name ASC, ta.teacher_name ASC"
        };
    }

    private sealed class TeacherAssignmentRow
    {
        public Guid id { get; init; }
        public Guid teacher_id { get; init; }
        public string teacher_name { get; init; } = string.Empty;
        public Guid academic_year_id { get; init; }
        public string academic_year_name { get; init; } = string.Empty;
        public Guid operational_period_id { get; init; }
        public string operational_period_name { get; init; } = string.Empty;
        public Guid subject_id { get; init; }
        public string subject_name { get; init; } = string.Empty;
        public Guid study_group_id { get; init; }
        public string study_group_name { get; init; } = string.Empty;
        public string status { get; init; } = string.Empty;
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
