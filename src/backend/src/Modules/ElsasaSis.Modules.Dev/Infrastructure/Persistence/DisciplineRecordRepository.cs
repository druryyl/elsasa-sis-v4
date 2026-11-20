using Dapper;
using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Dev.Infrastructure.Persistence;

public sealed class DisciplineRecordRepository(ISqlConnectionFactory connectionFactory) : IDisciplineRecordRepository
{
    private const string SelectColumns = """
        id, student_id, student_name, student_nis,
        academic_year_id, academic_year_name,
        incident_date, violation_type, description, sanction,
        recorder_id, recorder_name,
        created_at, updated_at
        """;

    private const string SelectByIdSql = $"""
        SELECT {SelectColumns}
        FROM dev.discipline_records
        WHERE id = @Id;
        """;

    private const string SelectByStudentIdSql = $"""
        SELECT {SelectColumns}
        FROM dev.discipline_records
        WHERE student_id = @StudentId
        ORDER BY incident_date DESC, created_at DESC;
        """;

    private const string SelectAllSql = $"""
        SELECT {SelectColumns}
        FROM dev.discipline_records
        WHERE (@StudentId IS NULL OR student_id = @StudentId)
          AND (@AcademicYearId IS NULL OR academic_year_id = @AcademicYearId)
        ORDER BY incident_date DESC, created_at DESC;
        """;

    private const string InsertSql = """
        INSERT INTO dev.discipline_records (
            id, student_id, student_name, student_nis,
            academic_year_id, academic_year_name,
            incident_date, violation_type, description, sanction,
            recorder_id, recorder_name,
            created_at, updated_at
        )
        VALUES (
            @Id, @StudentId, @StudentName, @StudentNis,
            @AcademicYearId, @AcademicYearName,
            @IncidentDate, @ViolationType, @Description, @Sanction,
            @RecorderId, @RecorderName,
            @CreatedAt, @UpdatedAt
        );
        """;

    private const string DeleteSql = """
        DELETE FROM dev.discipline_records
        WHERE id = @Id;
        """;

    public async Task<DisciplineRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<DisciplineRecordRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<DisciplineRecord>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<DisciplineRecordRow>(
            new CommandDefinition(SelectByStudentIdSql, new { StudentId = studentId }, cancellationToken: cancellationToken));

        return rows.Select(ToDomain).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<DisciplineRecord>> GetAllAsync(Guid? studentId = null, Guid? academicYearId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<DisciplineRecordRow>(
            new CommandDefinition(SelectAllSql, new { StudentId = studentId, AcademicYearId = academicYearId }, cancellationToken: cancellationToken));

        return rows.Select(ToDomain).ToList().AsReadOnly();
    }

    public async Task InsertAsync(DisciplineRecord record, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            InsertSql,
            new
            {
                record.Id,
                record.StudentId,
                record.StudentName,
                record.StudentNis,
                record.AcademicYearId,
                record.AcademicYearName,
                IncidentDate = record.IncidentDate.ToDateTime(TimeOnly.MinValue),
                record.ViolationType,
                record.Description,
                record.Sanction,
                record.RecorderId,
                record.RecorderName,
                record.CreatedAt,
                record.UpdatedAt
            },
            cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(DeleteSql, new { Id = id }, cancellationToken: cancellationToken));
    }

    private static DisciplineRecord ToDomain(DisciplineRecordRow row) =>
        DisciplineRecord.Rehydrate(
            row.id,
            row.student_id,
            row.student_name,
            row.student_nis,
            row.academic_year_id,
            row.academic_year_name,
            DateOnly.FromDateTime(row.incident_date),
            row.violation_type,
            row.description,
            row.sanction,
            row.recorder_id,
            row.recorder_name,
            DateTime.SpecifyKind(row.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(row.updated_at, DateTimeKind.Utc));

    private sealed class DisciplineRecordRow
    {
        public Guid id { get; set; }
        public Guid student_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string student_nis { get; set; } = string.Empty;
        public Guid? academic_year_id { get; set; }
        public string? academic_year_name { get; set; }
        public DateTime incident_date { get; set; }
        public string violation_type { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string sanction { get; set; } = string.Empty;
        public Guid recorder_id { get; set; }
        public string recorder_name { get; set; } = string.Empty;
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
