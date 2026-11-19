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
