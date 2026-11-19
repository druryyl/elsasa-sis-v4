using Dapper;
using ElsasaSis.SharedKernel.Pagination;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Tch.Features.Teachers.GetTeachers;

public sealed class GetTeachersHandler(ISqlConnectionFactory connectionFactory)
{
    public async Task<PaginatedResponse<TeacherDto>> HandleAsync(
        GetTeachersQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, offset) = TchQueryHelper.NormalizePagination(query.Page, query.PageSize);
        var searchPattern = TchQueryHelper.ToSearchPattern(query.Search);
        var normalizedStatus = string.IsNullOrWhiteSpace(query.Status)
            ? null
            : query.Status.Trim().ToUpperInvariant();
        var orderBy = ResolveOrderBy(query.SortField, query.SortOrder);

        var conditions = new List<string>();
        if (normalizedStatus is not null)
        {
            conditions.Add("t.status = @Status");
        }

        if (query.SubjectId.HasValue)
        {
            conditions.Add("""
                EXISTS (
                    SELECT 1
                    FROM tch.teaching_capabilities tc
                    WHERE tc.teacher_id = t.id
                      AND tc.subject_id = @SubjectId
                )
                """);
        }

        if (searchPattern is not null)
        {
            conditions.Add("""
                (
                    t.full_name LIKE @SearchPattern ESCAPE '\'
                    OR t.nickname LIKE @SearchPattern ESCAPE '\'
                    OR t.nip LIKE @SearchPattern ESCAPE '\'
                    OR t.nik LIKE @SearchPattern ESCAPE '\'
                )
                """);
        }

        var whereClause = conditions.Count == 0
            ? string.Empty
            : $"WHERE {string.Join(" AND ", conditions)}";

        var countSql = $"""
            SELECT COUNT(1)
            FROM tch.teachers t
            {whereClause};
            """;

        var teachersSql = $"""
            SELECT
                t.id,
                t.full_name,
                t.nickname,
                t.nip,
                t.nik,
                t.date_of_birth,
                t.gender,
                t.phone,
                t.email,
                t.address,
                t.status,
                t.created_at,
                t.updated_at
            FROM tch.teachers t
            {whereClause}
            ORDER BY {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            Status = normalizedStatus,
            query.SubjectId,
            SearchPattern = searchPattern,
            Offset = offset,
            PageSize = pageSize
        };

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        if (totalCount == 0)
        {
            return new PaginatedResponse<TeacherDto>([], page, pageSize, 0);
        }

        var teacherRows = (await connection.QueryAsync<TeacherRow>(
            new CommandDefinition(teachersSql, parameters, cancellationToken: cancellationToken))).ToArray();

        if (teacherRows.Length == 0)
        {
            return new PaginatedResponse<TeacherDto>([], page, pageSize, totalCount);
        }

        var teacherIds = teacherRows.Select(t => t.id).ToArray();

        const string capabilitiesSql = """
            SELECT id, teacher_id, subject_id, subject_name, created_at, updated_at
            FROM tch.teaching_capabilities
            WHERE teacher_id IN @TeacherIds
            ORDER BY subject_name ASC;
            """;

        const string activeAssignmentCountsSql = """
            SELECT teacher_id, COUNT(1) AS active_count
            FROM tch.teacher_assignments
            WHERE teacher_id IN @TeacherIds
              AND status = 'ASSIGNED'
            GROUP BY teacher_id;
            """;

        var capabilityRows = await connection.QueryAsync<CapabilityRow>(
            new CommandDefinition(capabilitiesSql, new { TeacherIds = teacherIds }, cancellationToken: cancellationToken));

        var countRows = await connection.QueryAsync<ActiveAssignmentCountRow>(
            new CommandDefinition(activeAssignmentCountsSql, new { TeacherIds = teacherIds }, cancellationToken: cancellationToken));

        var capabilitiesByTeacher = capabilityRows
            .GroupBy(c => c.teacher_id)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<TeachingCapabilityDto>)g
                    .Select(c => new TeachingCapabilityDto(
                        c.id,
                        c.teacher_id,
                        c.subject_id,
                        c.subject_name,
                        DateTime.SpecifyKind(c.created_at, DateTimeKind.Utc),
                        DateTime.SpecifyKind(c.updated_at, DateTimeKind.Utc)))
                    .ToArray());

        var activeCountsByTeacher = countRows.ToDictionary(r => r.teacher_id, r => r.active_count);

        var items = teacherRows
            .Select(t => new TeacherDto(
                t.id,
                t.full_name,
                t.nickname,
                t.nip,
                t.nik,
                DateOnly.FromDateTime(t.date_of_birth),
                t.gender,
                t.phone,
                t.email,
                t.address,
                t.status,
                activeCountsByTeacher.GetValueOrDefault(t.id, 0),
                DateTime.SpecifyKind(t.created_at, DateTimeKind.Utc),
                DateTime.SpecifyKind(t.updated_at, DateTimeKind.Utc),
                capabilitiesByTeacher.TryGetValue(t.id, out var caps) ? caps : []))
            .ToArray();

        return new PaginatedResponse<TeacherDto>(items, page, pageSize, totalCount);
    }

    private static string ResolveOrderBy(string? sortField, string? sortOrder)
    {
        var direction = TchQueryHelper.ResolveSortDirection(sortOrder);

        return sortField?.Trim().ToLowerInvariant() switch
        {
            "fullname" or "full_name" or "name" => $"t.full_name {direction ?? "ASC"}, t.created_at ASC",
            "nip" => $"t.nip {direction ?? "ASC"}, t.full_name ASC",
            "nik" => $"t.nik {direction ?? "ASC"}, t.full_name ASC",
            "status" => $"t.status {direction ?? "ASC"}, t.full_name ASC",
            "createdat" or "created_at" => $"t.created_at {direction ?? "DESC"}, t.full_name ASC",
            _ => "t.full_name ASC, t.created_at ASC"
        };
    }

    private sealed class TeacherRow
    {
        public Guid id { get; init; }
        public string full_name { get; init; } = string.Empty;
        public string? nickname { get; init; }
        public string? nip { get; init; }
        public string? nik { get; init; }
        public DateTime date_of_birth { get; init; }
        public string gender { get; init; } = string.Empty;
        public string? phone { get; init; }
        public string? email { get; init; }
        public string? address { get; init; }
        public string status { get; init; } = string.Empty;
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }

    private sealed class CapabilityRow
    {
        public Guid id { get; init; }
        public Guid teacher_id { get; init; }
        public Guid subject_id { get; init; }
        public string subject_name { get; init; } = string.Empty;
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }

    private sealed class ActiveAssignmentCountRow
    {
        public Guid teacher_id { get; init; }
        public int active_count { get; init; }
    }
}
