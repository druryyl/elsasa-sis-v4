using Dapper;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Pagination;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Std.Features.Students.GetStudents;

public sealed class GetStudentsHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    public async Task<Result<PaginatedResponse<StudentListItemDto>>> HandleAsync(
        GetStudentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var scopedTeacherResult = StudentDirectoryAccess.ResolveScopedTeacherId(currentUser);
        if (scopedTeacherResult.IsFailure)
        {
            return Result<PaginatedResponse<StudentListItemDto>>.Failure(scopedTeacherResult.Error);
        }

        var homeroomTeacherId = scopedTeacherResult.Value;
        var (page, pageSize, offset) = StudentReadQueryHelper.NormalizePagination(query.Page, query.PageSize);
        var searchPattern = StudentReadQueryHelper.ToSearchPattern(query.Search);
        var normalizedSchoolLevel = StudentReadQueryHelper.NormalizeCodeFilter(query.SchoolLevel);
        var normalizedGrade = StudentReadQueryHelper.NormalizeTextFilter(query.Grade);
        var normalizedStatus = StudentReadQueryHelper.NormalizeCodeFilter(query.Status);
        var orderBy = ResolveOrderBy(query.SortField, query.SortOrder);

        var placementConditions = new List<string> { "sp.student_id = s.id" };
        if (homeroomTeacherId.HasValue)
        {
            placementConditions.Add("sg.homeroom_teacher_id = @HomeroomTeacherId");
        }

        if (query.AcademicYearId.HasValue)
        {
            placementConditions.Add("sp.academic_year_id = @AcademicYearId");
        }

        if (normalizedSchoolLevel is not null)
        {
            placementConditions.Add("sp.school_level = @SchoolLevel");
        }

        if (normalizedGrade is not null)
        {
            placementConditions.Add("sp.grade = @Grade");
        }

        if (query.StudyGroupId.HasValue)
        {
            placementConditions.Add("sp.study_group_id = @StudyGroupId");
        }

        var hasPlacementFilter = homeroomTeacherId.HasValue ||
                                 query.AcademicYearId.HasValue ||
                                 normalizedSchoolLevel is not null ||
                                 normalizedGrade is not null ||
                                 query.StudyGroupId.HasValue;

        var applyKeyword = hasPlacementFilter ? "CROSS APPLY" : "OUTER APPLY";
        var studyGroupJoin = homeroomTeacherId.HasValue
            ? "INNER JOIN std.study_groups sg ON sg.id = sp.study_group_id"
            : "LEFT JOIN std.study_groups sg ON sg.id = sp.study_group_id";
        var placementWhereClause = $"WHERE {string.Join(" AND ", placementConditions)}";

        var studentConditions = new List<string>();
        if (normalizedStatus is not null)
        {
            studentConditions.Add("s.status = @Status");
        }

        if (searchPattern is not null)
        {
            studentConditions.Add(
                "(s.nis LIKE @SearchPattern ESCAPE '\\' OR s.nisn LIKE @SearchPattern ESCAPE '\\' OR s.full_name LIKE @SearchPattern ESCAPE '\\' OR s.nickname LIKE @SearchPattern ESCAPE '\\')");
        }

        var studentWhereClause = studentConditions.Count == 0
            ? string.Empty
            : $"WHERE {string.Join(" AND ", studentConditions)}";

        var fromAndApplyClause = $"""
            FROM std.students s
            {applyKeyword} (
                SELECT TOP (1)
                    sp.academic_year_id,
                    sp.academic_year_name,
                    sp.school_level,
                    sp.grade,
                    sp.study_group_id,
                    sp.study_group_name
                FROM std.student_placements sp
                {studyGroupJoin}
                LEFT JOIN opr.academic_years ay ON ay.id = sp.academic_year_id
                {placementWhereClause}
                ORDER BY CASE WHEN ay.status = 'ACTIVE' THEN 0 ELSE 1 END ASC,
                         sp.updated_at DESC,
                         sp.created_at DESC
            ) sp
            {studentWhereClause}
            """;

        var countSql = $"""
            SELECT COUNT(1)
            {fromAndApplyClause};
            """;

        var itemsSql = $"""
            SELECT
                s.id,
                s.candidate_id,
                s.nis,
                s.nisn,
                s.full_name,
                s.nickname,
                s.date_of_birth,
                s.gender,
                s.phone,
                s.email,
                s.address,
                s.enrollment_date,
                s.status,
                s.created_at,
                s.updated_at,
                sp.academic_year_id,
                sp.academic_year_name,
                sp.school_level,
                sp.grade,
                sp.study_group_id,
                sp.study_group_name
            {fromAndApplyClause}
            ORDER BY {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            HomeroomTeacherId = homeroomTeacherId,
            query.AcademicYearId,
            SchoolLevel = normalizedSchoolLevel,
            Grade = normalizedGrade,
            query.StudyGroupId,
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
            return Result<PaginatedResponse<StudentListItemDto>>.Success(
                new PaginatedResponse<StudentListItemDto>([], page, pageSize, 0));
        }

        var rows = (await connection.QueryAsync<StudentRow>(
            new CommandDefinition(itemsSql, parameters, cancellationToken: cancellationToken))).ToList();

        if (rows.Count == 0)
        {
            return Result<PaginatedResponse<StudentListItemDto>>.Success(
                new PaginatedResponse<StudentListItemDto>([], page, pageSize, totalCount));
        }

        var studentIds = rows.Select(r => r.id).ToArray();
        var guardiansByStudent = await StudentReadQueryHelper.LoadGuardiansByStudentIdsAsync(
            connection,
            studentIds,
            cancellationToken);

        var items = rows
            .Select(r =>
            {
                var guardians = guardiansByStudent.TryGetValue(r.id, out var list)
                    ? list
                    : [];

                var contactSummaries = guardians
                    .Select(g => new StudentGuardianContactDto(
                        g.Id,
                        g.FullName,
                        g.RelationshipType,
                        g.Phone,
                        g.Email))
                    .ToArray();

                var primaryGuardian = guardians.FirstOrDefault();

                return new StudentListItemDto(
                    r.id,
                    r.candidate_id,
                    r.nis,
                    r.nisn,
                    r.full_name,
                    r.nickname,
                    DateOnly.FromDateTime(r.date_of_birth),
                    r.gender,
                    r.phone,
                    r.email,
                    r.address,
                    DateOnly.FromDateTime(r.enrollment_date),
                    r.status,
                    r.academic_year_id,
                    r.academic_year_name,
                    r.school_level,
                    r.grade,
                    r.study_group_id,
                    r.study_group_name,
                    primaryGuardian?.FullName,
                    primaryGuardian?.Phone,
                    contactSummaries,
                    StudentReadQueryHelper.SpecifyUtc(r.created_at),
                    StudentReadQueryHelper.SpecifyUtc(r.updated_at));
            })
            .ToArray();

        return Result<PaginatedResponse<StudentListItemDto>>.Success(
            new PaginatedResponse<StudentListItemDto>(items, page, pageSize, totalCount));
    }

    private static string ResolveOrderBy(string? sortField, string? sortOrder)
    {
        var direction = StudentReadQueryHelper.ResolveSortDirection(sortOrder);

        return sortField?.Trim().ToLowerInvariant() switch
        {
            "nis" => $"s.nis {direction ?? "ASC"}, s.full_name ASC",
            "nisn" => $"s.nisn {direction ?? "ASC"}, s.full_name ASC",
            "fullname" or "full_name" or "name" => $"s.full_name {direction ?? "ASC"}, s.nis ASC",
            "enrollmentdate" or "enrollment_date" => $"s.enrollment_date {direction ?? "DESC"}, s.full_name ASC",
            "status" => $"s.status {direction ?? "ASC"}, s.full_name ASC",
            "grade" => $"TRY_CAST(sp.grade AS INT) {direction ?? "ASC"}, sp.grade {direction ?? "ASC"}, s.full_name ASC",
            "studygroupname" or "study_group_name" => $"sp.study_group_name {direction ?? "ASC"}, s.full_name ASC",
            "createdat" or "created_at" => $"s.created_at {direction ?? "DESC"}",
            _ => "s.full_name ASC, s.nis ASC"
        };
    }

    private sealed class StudentRow
    {
        public Guid id { get; init; }
        public Guid? candidate_id { get; init; }
        public string nis { get; init; } = string.Empty;
        public string? nisn { get; init; }
        public string full_name { get; init; } = string.Empty;
        public string? nickname { get; init; }
        public DateTime date_of_birth { get; init; }
        public string gender { get; init; } = string.Empty;
        public string? phone { get; init; }
        public string? email { get; init; }
        public string? address { get; init; }
        public DateTime enrollment_date { get; init; }
        public string status { get; init; } = string.Empty;
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
        public Guid? academic_year_id { get; init; }
        public string? academic_year_name { get; init; }
        public string? school_level { get; init; }
        public string? grade { get; init; }
        public Guid? study_group_id { get; init; }
        public string? study_group_name { get; init; }
    }
}
