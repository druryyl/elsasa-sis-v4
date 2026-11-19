using Dapper;
using ElsasaSis.Modules.Std.Features.Students;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Pagination;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Std.Features.StudyGroups.GetStudyGroups;

public sealed class GetStudyGroupsHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    public async Task<Result<PaginatedResponse<StudyGroupListItemDto>>> HandleAsync(
        GetStudyGroupsQuery query,
        CancellationToken cancellationToken = default)
    {
        var scopedTeacherResult = StudentDirectoryAccess.ResolveScopedTeacherId(currentUser);
        if (scopedTeacherResult.IsFailure)
        {
            return Result<PaginatedResponse<StudyGroupListItemDto>>.Failure(scopedTeacherResult.Error);
        }

        var (page, pageSize, offset) = StudentReadQueryHelper.NormalizePagination(query.Page, query.PageSize);
        var searchPattern = StudentReadQueryHelper.ToSearchPattern(query.Search);
        var normalizedSchoolLevel = StudentReadQueryHelper.NormalizeCodeFilter(query.SchoolLevel);
        var normalizedGrade = StudentReadQueryHelper.NormalizeTextFilter(query.Grade);
        var orderBy = ResolveOrderBy(query.SortField, query.SortOrder);

        var conditions = new List<string>();

        if (scopedTeacherResult.Value.HasValue)
        {
            conditions.Add("sg.homeroom_teacher_id = @HomeroomTeacherId");
        }

        if (query.AcademicYearId.HasValue)
        {
            conditions.Add("sg.academic_year_id = @AcademicYearId");
        }

        if (normalizedSchoolLevel is not null)
        {
            conditions.Add("sg.school_level = @SchoolLevel");
        }

        if (normalizedGrade is not null)
        {
            conditions.Add("sg.grade = @Grade");
        }

        if (searchPattern is not null)
        {
            conditions.Add("(sg.name LIKE @SearchPattern ESCAPE '\\' OR sg.homeroom_teacher_name LIKE @SearchPattern ESCAPE '\\')");
        }

        var whereClause = conditions.Count == 0
            ? string.Empty
            : $"WHERE {string.Join(" AND ", conditions)}";

        var countSql = $"""
            SELECT COUNT(1)
            FROM std.study_groups sg
            {whereClause};
            """;

        var dataSql = $"""
            SELECT
                sg.id,
                sg.academic_year_id,
                sg.academic_year_name,
                sg.school_level,
                sg.grade,
                sg.name,
                sg.homeroom_teacher_id,
                sg.homeroom_teacher_name,
                sg.created_at,
                sg.updated_at,
                (SELECT COUNT(1) FROM std.student_placements sp WHERE sp.study_group_id = sg.id) AS student_count
            FROM std.study_groups sg
            {whereClause}
            ORDER BY {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new DynamicParameters();
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        if (scopedTeacherResult.Value.HasValue)
        {
            parameters.Add("HomeroomTeacherId", scopedTeacherResult.Value.Value);
        }

        if (query.AcademicYearId.HasValue)
        {
            parameters.Add("AcademicYearId", query.AcademicYearId.Value);
        }

        if (normalizedSchoolLevel is not null)
        {
            parameters.Add("SchoolLevel", normalizedSchoolLevel);
        }

        if (normalizedGrade is not null)
        {
            parameters.Add("Grade", normalizedGrade);
        }

        if (searchPattern is not null)
        {
            parameters.Add("SearchPattern", searchPattern);
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            parameters,
            cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<StudyGroupListRow>(new CommandDefinition(
            dataSql,
            parameters,
            cancellationToken: cancellationToken));

        var items = rows.Select(r => new StudyGroupListItemDto(
            r.id,
            r.academic_year_id,
            r.academic_year_name,
            r.school_level,
            r.grade,
            r.name,
            r.homeroom_teacher_id,
            r.homeroom_teacher_name,
            r.student_count,
            StudentReadQueryHelper.SpecifyUtc(r.created_at),
            StudentReadQueryHelper.SpecifyUtc(r.updated_at)))
            .ToArray();

        return Result<PaginatedResponse<StudyGroupListItemDto>>.Success(new PaginatedResponse<StudyGroupListItemDto>(
            items,
            page,
            pageSize,
            totalCount));
    }

    private static string ResolveOrderBy(string? sortField, string? sortOrder)
    {
        var direction = StudentReadQueryHelper.ResolveSortDirection(sortOrder) ?? "ASC";

        return sortField?.Trim().ToLowerInvariant() switch
        {
            "name" => $"sg.name {direction}, sg.school_level ASC, sg.grade ASC",
            "grade" => $"sg.grade {direction}, sg.name ASC",
            "schoollevel" => $"sg.school_level {direction}, sg.grade ASC, sg.name ASC",
            "academicyearname" => $"sg.academic_year_name {direction}, sg.name ASC",
            "homeroomteachername" => $"sg.homeroom_teacher_name {direction}, sg.name ASC",
            "studentcount" => $"student_count {direction}, sg.name ASC",
            "createdat" => $"sg.created_at {direction}",
            "updatedat" => $"sg.updated_at {direction}",
            _ => "sg.school_level ASC, sg.grade ASC, sg.name ASC"
        };
    }

    private sealed class StudyGroupListRow
    {
        public Guid id { get; set; }
        public Guid academic_year_id { get; set; }
        public string academic_year_name { get; set; } = string.Empty;
        public string school_level { get; set; } = string.Empty;
        public string grade { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public Guid? homeroom_teacher_id { get; set; }
        public string? homeroom_teacher_name { get; set; }
        public int student_count { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
