using Dapper;
using ElsasaSis.Contracts.Student;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Std.Infrastructure.Contracts;

public sealed class StudentContract(ISqlConnectionFactory connectionFactory) : IStudentContract
{
    private const string SelectStudyGroupByIdSql = """
        SELECT id,
               academic_year_id,
               academic_year_name,
               school_level,
               grade,
               name,
               homeroom_teacher_id,
               homeroom_teacher_name
        FROM std.study_groups
        WHERE id = @Id;
        """;

    private const string SelectActiveStudentsByScopeSql = """
        WITH RankedPlacements AS (
            SELECT sp.student_id AS StudentId,
                   s.full_name AS StudentName,
                   s.nis AS StudentNis,
                   ISNULL(sp.study_group_id, '00000000-0000-0000-0000-000000000000') AS StudyGroupId,
                   ISNULL(sp.study_group_name, '') AS StudyGroupName,
                   sp.school_level AS SchoolLevel,
                   sp.grade AS Grade,
                   ROW_NUMBER() OVER (
                       PARTITION BY sp.student_id
                       ORDER BY sp.created_at DESC
                   ) AS rn
            FROM std.student_placements sp
            INNER JOIN std.students s ON s.id = sp.student_id
            WHERE s.status = 'ACTIVE'
              AND (@StudyGroupId IS NULL OR sp.study_group_id = @StudyGroupId)
              AND (@SchoolLevel IS NULL OR sp.school_level = @SchoolLevel)
              AND (@Grade IS NULL OR sp.grade = @Grade)
        )
        SELECT StudentId,
               StudentName,
               StudentNis,
               StudyGroupId,
               StudyGroupName,
               SchoolLevel,
               Grade
        FROM RankedPlacements
        WHERE rn = 1
        ORDER BY StudentName ASC;
        """;

    public async Task<StudyGroupSummaryDto?> GetStudyGroupByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<StudyGroupSummaryRow>(
            new CommandDefinition(SelectStudyGroupByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null
            ? null
            : new StudyGroupSummaryDto(
                row.id,
                row.academic_year_id,
                row.academic_year_name,
                row.school_level,
                row.grade,
                row.name,
                row.homeroom_teacher_id,
                row.homeroom_teacher_name);
    }

    public async Task<IReadOnlyList<StudentPlacementScopeDto>> GetActiveStudentsByScopeAsync(
        Guid? studyGroupId,
        string? schoolLevel,
        string? grade,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<StudentPlacementScopeRow>(
            new CommandDefinition(
                SelectActiveStudentsByScopeSql,
                new
                {
                    StudyGroupId = studyGroupId,
                    SchoolLevel = schoolLevel,
                    Grade = grade
                },
                cancellationToken: cancellationToken));

        return rows
            .Select(r => new StudentPlacementScopeDto(
                r.StudentId,
                r.StudentName,
                r.StudentNis,
                r.StudyGroupId,
                r.StudyGroupName,
                r.SchoolLevel,
                r.Grade))
            .ToList();
    }

    private sealed class StudentPlacementScopeRow
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentNis { get; set; } = string.Empty;
        public Guid StudyGroupId { get; set; }
        public string StudyGroupName { get; set; } = string.Empty;
        public string SchoolLevel { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
    }

    private sealed class StudyGroupSummaryRow
    {
        public Guid id { get; set; }
        public Guid academic_year_id { get; set; }
        public string academic_year_name { get; set; } = string.Empty;
        public string school_level { get; set; } = string.Empty;
        public string grade { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public Guid? homeroom_teacher_id { get; set; }
        public string? homeroom_teacher_name { get; set; }
    }
}
