using Dapper;
using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Dev.Infrastructure.Persistence;

public sealed class AchievementRepository(ISqlConnectionFactory connectionFactory) : IAchievementRepository
{
    private const string SelectColumns = """
        id, student_id, student_name, student_nis,
        academic_year_id, academic_year_name,
        achievement_type, event_name, recognition_level,
        achievement_date, description,
        recorder_id, recorder_name,
        created_at, updated_at
        """;

    private const string SelectByIdSql = $"""
        SELECT {SelectColumns}
        FROM dev.achievements
        WHERE id = @Id;
        """;

    private const string SelectByStudentIdSql = $"""
        SELECT {SelectColumns}
        FROM dev.achievements
        WHERE student_id = @StudentId
        ORDER BY achievement_date DESC, created_at DESC;
        """;

    private const string SelectAllSql = $"""
        SELECT {SelectColumns}
        FROM dev.achievements
        WHERE (@StudentId IS NULL OR student_id = @StudentId)
          AND (@AcademicYearId IS NULL OR academic_year_id = @AcademicYearId)
        ORDER BY achievement_date DESC, created_at DESC;
        """;

    private const string InsertSql = """
        INSERT INTO dev.achievements (
            id, student_id, student_name, student_nis,
            academic_year_id, academic_year_name,
            achievement_type, event_name, recognition_level,
            achievement_date, description,
            recorder_id, recorder_name,
            created_at, updated_at
        )
        VALUES (
            @Id, @StudentId, @StudentName, @StudentNis,
            @AcademicYearId, @AcademicYearName,
            @AchievementType, @EventName, @RecognitionLevel,
            @AchievementDate, @Description,
            @RecorderId, @RecorderName,
            @CreatedAt, @UpdatedAt
        );
        """;

    private const string DeleteSql = """
        DELETE FROM dev.achievements
        WHERE id = @Id;
        """;

    public async Task<Achievement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<AchievementRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<Achievement>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<AchievementRow>(
            new CommandDefinition(SelectByStudentIdSql, new { StudentId = studentId }, cancellationToken: cancellationToken));

        return rows.Select(ToDomain).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<Achievement>> GetAllAsync(
        Guid? studentId = null,
        Guid? academicYearId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<AchievementRow>(
            new CommandDefinition(SelectAllSql, new { StudentId = studentId, AcademicYearId = academicYearId }, cancellationToken: cancellationToken));

        return rows.Select(ToDomain).ToList().AsReadOnly();
    }

    public async Task InsertAsync(Achievement achievement, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            InsertSql,
            new
            {
                achievement.Id,
                achievement.StudentId,
                achievement.StudentName,
                achievement.StudentNis,
                achievement.AcademicYearId,
                achievement.AcademicYearName,
                achievement.AchievementType,
                achievement.EventName,
                achievement.RecognitionLevel,
                AchievementDate = achievement.AchievementDate.ToDateTime(TimeOnly.MinValue),
                achievement.Description,
                achievement.RecorderId,
                achievement.RecorderName,
                achievement.CreatedAt,
                achievement.UpdatedAt
            },
            cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(DeleteSql, new { Id = id }, cancellationToken: cancellationToken));
    }

    private static Achievement ToDomain(AchievementRow row) =>
        Achievement.Rehydrate(
            row.id,
            row.student_id,
            row.student_name,
            row.student_nis,
            row.academic_year_id,
            row.academic_year_name,
            row.achievement_type,
            row.event_name,
            row.recognition_level,
            DateOnly.FromDateTime(row.achievement_date),
            row.description,
            row.recorder_id,
            row.recorder_name,
            DateTime.SpecifyKind(row.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(row.updated_at, DateTimeKind.Utc));

    private sealed class AchievementRow
    {
        public Guid id { get; set; }
        public Guid student_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string student_nis { get; set; } = string.Empty;
        public Guid? academic_year_id { get; set; }
        public string? academic_year_name { get; set; }
        public string achievement_type { get; set; } = string.Empty;
        public string event_name { get; set; } = string.Empty;
        public string recognition_level { get; set; } = string.Empty;
        public DateTime achievement_date { get; set; }
        public string? description { get; set; }
        public Guid recorder_id { get; set; }
        public string recorder_name { get; set; } = string.Empty;
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
