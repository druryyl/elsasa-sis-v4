using Dapper;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Std.Features.Students.GetStudentById;

public sealed class GetStudentByIdHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string SelectStudentSql = """
        SELECT id, candidate_id, nis, nisn, full_name, nickname, date_of_birth, gender,
               phone, email, address, enrollment_date, status, created_at, updated_at
        FROM std.students
        WHERE id = @Id;
        """;

    private const string CheckHomeroomStudentSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM std.student_placements sp
            INNER JOIN std.study_groups sg ON sg.id = sp.study_group_id
            WHERE sp.student_id = @StudentId
              AND sg.homeroom_teacher_id = @HomeroomTeacherId
        ) THEN 1 ELSE 0 END;
        """;

    private const string SelectPlacementsSql = """
        SELECT
            sp.id,
            sp.student_id,
            sp.academic_year_id,
            sp.academic_year_name,
            sp.school_level,
            sp.grade,
            sp.study_group_id,
            sp.study_group_name,
            sg.homeroom_teacher_id,
            sg.homeroom_teacher_name,
            sp.created_at,
            sp.updated_at
        FROM std.student_placements sp
        LEFT JOIN std.study_groups sg ON sg.id = sp.study_group_id
        LEFT JOIN opr.academic_years ay ON ay.id = sp.academic_year_id
        WHERE sp.student_id = @StudentId
        ORDER BY CASE WHEN ay.status = 'ACTIVE' THEN 0 ELSE 1 END ASC,
                 COALESCE(ay.start_date, '1900-01-01') DESC,
                 sp.created_at DESC;
        """;

    public async Task<Result<StudentDetailDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var scopedTeacherResult = StudentDirectoryAccess.ResolveScopedTeacherId(currentUser);
        if (scopedTeacherResult.IsFailure)
        {
            return Result<StudentDetailDto>.Failure(scopedTeacherResult.Error);
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<StudentRow>(
            new CommandDefinition(SelectStudentSql, new { Id = id }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result<StudentDetailDto>.Failure(StdErrors.StudentNotFound(id));
        }

        if (scopedTeacherResult.Value.HasValue)
        {
            var isAuthorized = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                CheckHomeroomStudentSql,
                new { StudentId = id, HomeroomTeacherId = scopedTeacherResult.Value.Value },
                cancellationToken: cancellationToken));

            if (!isAuthorized)
            {
                return Result<StudentDetailDto>.Failure(StudentDirectoryAccess.StudentRosterForbidden(id));
            }
        }

        var guardiansByStudent = await StudentReadQueryHelper.LoadGuardiansByStudentIdsAsync(
            connection,
            [id],
            cancellationToken);

        var placementRows = await connection.QueryAsync<StudentPlacementRow>(new CommandDefinition(
            SelectPlacementsSql,
            new { StudentId = id },
            cancellationToken: cancellationToken));

        var guardians = guardiansByStudent.TryGetValue(id, out var guardianList)
            ? guardianList
            : [];

        var placements = placementRows
            .Select(p => new StudentPlacementDto(
                p.id,
                p.student_id,
                p.academic_year_id,
                p.academic_year_name,
                p.school_level,
                p.grade,
                p.study_group_id,
                p.study_group_name,
                p.homeroom_teacher_id,
                p.homeroom_teacher_name,
                StudentReadQueryHelper.SpecifyUtc(p.created_at),
                StudentReadQueryHelper.SpecifyUtc(p.updated_at)))
            .ToArray();

        return Result<StudentDetailDto>.Success(new StudentDetailDto(
            row.id,
            row.candidate_id,
            row.nis,
            row.nisn,
            row.full_name,
            row.nickname,
            DateOnly.FromDateTime(row.date_of_birth),
            row.gender,
            row.phone,
            row.email,
            row.address,
            DateOnly.FromDateTime(row.enrollment_date),
            row.status,
            StudentReadQueryHelper.SpecifyUtc(row.created_at),
            StudentReadQueryHelper.SpecifyUtc(row.updated_at),
            guardians,
            placements));
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
    }

    private sealed class StudentPlacementRow
    {
        public Guid id { get; init; }
        public Guid student_id { get; init; }
        public Guid academic_year_id { get; init; }
        public string academic_year_name { get; init; } = string.Empty;
        public string school_level { get; init; } = string.Empty;
        public string grade { get; init; } = string.Empty;
        public Guid? study_group_id { get; init; }
        public string? study_group_name { get; init; }
        public Guid? homeroom_teacher_id { get; init; }
        public string? homeroom_teacher_name { get; init; }
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
