using Dapper;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.Modules.Std.Features.Students;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Std.Features.StudyGroups.GetStudyGroupRoster;

public sealed class GetStudyGroupRosterHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string CheckHomeroomOwnershipSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM std.study_groups
            WHERE id = @StudyGroupId
              AND homeroom_teacher_id = @CurrentUserTeacherId
        ) THEN 1 ELSE 0 END;
        """;

    private const string SelectStudyGroupSql = """
        SELECT id, academic_year_id, academic_year_name, school_level, grade, name,
               homeroom_teacher_id, homeroom_teacher_name, created_at, updated_at
        FROM std.study_groups
        WHERE id = @StudyGroupId;
        """;

    private const string SelectRosterStudentsSql = """
        SELECT
            s.id,
            sp.id AS placement_id,
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
            s.status
        FROM std.student_placements sp
        INNER JOIN std.students s ON s.id = sp.student_id
        WHERE sp.study_group_id = @StudyGroupId
          AND s.status = 'ACTIVE'
        ORDER BY s.full_name ASC, s.id ASC;
        """;

    public async Task<Result<StudyGroupRosterDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var scopedTeacherResult = StudentDirectoryAccess.ResolveScopedTeacherId(currentUser);
        if (scopedTeacherResult.IsFailure)
        {
            return Result<StudyGroupRosterDto>.Failure(scopedTeacherResult.Error);
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (scopedTeacherResult.Value.HasValue)
        {
            var isAuthorized = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                CheckHomeroomOwnershipSql,
                new { StudyGroupId = id, CurrentUserTeacherId = scopedTeacherResult.Value.Value },
                cancellationToken: cancellationToken));

            if (!isAuthorized)
            {
                return Result<StudyGroupRosterDto>.Failure(StudentDirectoryAccess.StudyGroupRosterForbidden(id));
            }
        }

        var groupRow = await connection.QuerySingleOrDefaultAsync<StudyGroupRow>(new CommandDefinition(
            SelectStudyGroupSql,
            new { StudyGroupId = id },
            cancellationToken: cancellationToken));

        if (groupRow is null)
        {
            return Result<StudyGroupRosterDto>.Failure(StdErrors.StudyGroupNotFound(id));
        }

        var studentRows = (await connection.QueryAsync<RosterStudentRow>(new CommandDefinition(
            SelectRosterStudentsSql,
            new { StudyGroupId = id },
            cancellationToken: cancellationToken))).ToArray();

        var studentIds = studentRows.Select(r => r.id).ToArray();
        var guardiansByStudent = await StudentReadQueryHelper.LoadGuardiansByStudentIdsAsync(
            connection,
            studentIds,
            cancellationToken);

        var students = studentRows.Select(s => new StudyGroupRosterStudentDto(
            s.id,
            s.placement_id,
            s.nis,
            s.nisn,
            s.full_name,
            s.nickname,
            DateOnly.FromDateTime(s.date_of_birth),
            s.gender,
            s.phone,
            s.email,
            s.address,
            DateOnly.FromDateTime(s.enrollment_date),
            s.status,
            guardiansByStudent.TryGetValue(s.id, out var gList) ? gList : []))
            .ToArray();

        return Result<StudyGroupRosterDto>.Success(new StudyGroupRosterDto(
            groupRow.id,
            groupRow.academic_year_id,
            groupRow.academic_year_name,
            groupRow.school_level,
            groupRow.grade,
            groupRow.name,
            groupRow.homeroom_teacher_id,
            groupRow.homeroom_teacher_name,
            students.Length,
            students));
    }

    private sealed class StudyGroupRow
    {
        public Guid id { get; set; }
        public Guid academic_year_id { get; set; }
        public string academic_year_name { get; set; } = string.Empty;
        public string school_level { get; set; } = string.Empty;
        public string grade { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public Guid? homeroom_teacher_id { get; set; }
        public string? homeroom_teacher_name { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    private sealed class RosterStudentRow
    {
        public Guid id { get; set; }
        public Guid placement_id { get; set; }
        public string nis { get; set; } = string.Empty;
        public string? nisn { get; set; }
        public string full_name { get; set; } = string.Empty;
        public string? nickname { get; set; }
        public DateTime date_of_birth { get; set; }
        public string gender { get; set; } = string.Empty;
        public string? phone { get; set; }
        public string? email { get; set; }
        public string? address { get; set; }
        public DateTime enrollment_date { get; set; }
        public string status { get; set; } = string.Empty;
    }
}
