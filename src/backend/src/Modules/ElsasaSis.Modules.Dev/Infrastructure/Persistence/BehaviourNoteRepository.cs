using Dapper;
using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Dev.Infrastructure.Persistence;

public sealed class BehaviourNoteRepository(ISqlConnectionFactory connectionFactory) : IBehaviourNoteRepository
{
    private const string SelectColumns = """
        id, student_id, student_name, student_nis,
        academic_year_id, academic_year_name,
        behaviour_type, observed_date, description,
        recorder_id, recorder_name,
        created_at, updated_at
        """;

    private const string SelectByIdSql = $"""
        SELECT {SelectColumns}
        FROM dev.behaviour_notes
        WHERE id = @Id;
        """;

    private const string SelectByStudentIdSql = $"""
        SELECT {SelectColumns}
        FROM dev.behaviour_notes
        WHERE student_id = @StudentId
        ORDER BY observed_date DESC, created_at DESC;
        """;

    private const string SelectAllSql = $"""
        SELECT {SelectColumns}
        FROM dev.behaviour_notes
        WHERE (@StudentId IS NULL OR student_id = @StudentId)
          AND (@AcademicYearId IS NULL OR academic_year_id = @AcademicYearId)
        ORDER BY observed_date DESC, created_at DESC;
        """;

    private const string InsertSql = """
        INSERT INTO dev.behaviour_notes (
            id, student_id, student_name, student_nis,
            academic_year_id, academic_year_name,
            behaviour_type, observed_date, description,
            recorder_id, recorder_name,
            created_at, updated_at
        )
        VALUES (
            @Id, @StudentId, @StudentName, @StudentNis,
            @AcademicYearId, @AcademicYearName,
            @BehaviourType, @ObservedDate, @Description,
            @RecorderId, @RecorderName,
            @CreatedAt, @UpdatedAt
        );
        """;

    private const string DeleteSql = """
        DELETE FROM dev.behaviour_notes
        WHERE id = @Id;
        """;

    public async Task<BehaviourNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<BehaviourNoteRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<BehaviourNote>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<BehaviourNoteRow>(
            new CommandDefinition(SelectByStudentIdSql, new { StudentId = studentId }, cancellationToken: cancellationToken));

        return rows.Select(ToDomain).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<BehaviourNote>> GetAllAsync(Guid? studentId = null, Guid? academicYearId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<BehaviourNoteRow>(
            new CommandDefinition(SelectAllSql, new { StudentId = studentId, AcademicYearId = academicYearId }, cancellationToken: cancellationToken));

        return rows.Select(ToDomain).ToList().AsReadOnly();
    }

    public async Task InsertAsync(BehaviourNote note, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            InsertSql,
            new
            {
                note.Id,
                note.StudentId,
                note.StudentName,
                note.StudentNis,
                note.AcademicYearId,
                note.AcademicYearName,
                note.BehaviourType,
                ObservedDate = note.ObservedDate.ToDateTime(TimeOnly.MinValue),
                note.Description,
                note.RecorderId,
                note.RecorderName,
                note.CreatedAt,
                note.UpdatedAt
            },
            cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(DeleteSql, new { Id = id }, cancellationToken: cancellationToken));
    }

    private static BehaviourNote ToDomain(BehaviourNoteRow row) =>
        BehaviourNote.Rehydrate(
            row.id,
            row.student_id,
            row.student_name,
            row.student_nis,
            row.academic_year_id,
            row.academic_year_name,
            row.behaviour_type,
            DateOnly.FromDateTime(row.observed_date),
            row.description,
            row.recorder_id,
            row.recorder_name,
            DateTime.SpecifyKind(row.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(row.updated_at, DateTimeKind.Utc));

    private sealed class BehaviourNoteRow
    {
        public Guid id { get; set; }
        public Guid student_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string student_nis { get; set; } = string.Empty;
        public Guid? academic_year_id { get; set; }
        public string? academic_year_name { get; set; }
        public string behaviour_type { get; set; } = string.Empty;
        public DateTime observed_date { get; set; }
        public string description { get; set; } = string.Empty;
        public Guid recorder_id { get; set; }
        public string recorder_name { get; set; } = string.Empty;
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
