using Dapper;
using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Dev.Infrastructure.Persistence;

public sealed class ExtracurricularActivityRepository(ISqlConnectionFactory connectionFactory) : IExtracurricularActivityRepository
{
    private const string SelectActivityColumns = """
        id, name, description, supervisor_id, supervisor_name, status, created_at, updated_at
        """;

    private const string SelectParticipationColumns = """
        id, activity_id, activity_name, student_id, student_name, student_nis,
        academic_year_id, academic_year_name, status, joined_date, completed_at, withdrawn_at,
        last_session_log, last_session_date, created_at, updated_at
        """;

    private const string SelectByIdSql = $"""
        SELECT {SelectActivityColumns}
        FROM dev.extracurricular_activities
        WHERE id = @Id;

        SELECT {SelectParticipationColumns}
        FROM dev.extracurricular_participations
        WHERE activity_id = @Id
        ORDER BY joined_date ASC, created_at ASC;
        """;

    private const string SelectByNameSql = $"""
        SELECT {SelectActivityColumns}
        FROM dev.extracurricular_activities
        WHERE name = @Name;
        """;

    private const string SelectParticipationsByActivityIdSql = $"""
        SELECT {SelectParticipationColumns}
        FROM dev.extracurricular_participations
        WHERE activity_id = @ActivityId
        ORDER BY joined_date ASC, created_at ASC;
        """;

    private const string SelectAllActivitiesSql = $"""
        SELECT {SelectActivityColumns}
        FROM dev.extracurricular_activities
        WHERE (@Status IS NULL OR status = @Status)
          AND (@SupervisorId IS NULL OR supervisor_id = @SupervisorId)
        ORDER BY name ASC;
        """;

    private const string SelectParticipationsByStudentIdSql = $"""
        SELECT {SelectParticipationColumns}
        FROM dev.extracurricular_participations
        WHERE student_id = @StudentId
        ORDER BY joined_date DESC, created_at DESC;
        """;

    private const string SelectParticipationsByActivityAndYearSql = $"""
        SELECT {SelectParticipationColumns}
        FROM dev.extracurricular_participations
        WHERE activity_id = @ActivityId
          AND (@AcademicYearId IS NULL OR academic_year_id = @AcademicYearId)
        ORDER BY student_name ASC, joined_date ASC;
        """;

    private const string SelectParticipationByIdSql = $"""
        SELECT {SelectParticipationColumns}
        FROM dev.extracurricular_participations
        WHERE id = @Id;
        """;

    private const string InsertActivitySql = """
        INSERT INTO dev.extracurricular_activities (
            id, name, description, supervisor_id, supervisor_name, status, created_at, updated_at
        )
        VALUES (
            @Id, @Name, @Description, @SupervisorId, @SupervisorName, @Status, @CreatedAt, @UpdatedAt
        );
        """;

    private const string UpdateActivitySql = """
        UPDATE dev.extracurricular_activities
        SET name = @Name,
            description = @Description,
            supervisor_id = @SupervisorId,
            supervisor_name = @SupervisorName,
            status = @Status,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string InsertParticipationSql = """
        INSERT INTO dev.extracurricular_participations (
            id, activity_id, activity_name, student_id, student_name, student_nis,
            academic_year_id, academic_year_name, status, joined_date,
            completed_at, withdrawn_at, last_session_log, last_session_date,
            created_at, updated_at
        )
        VALUES (
            @Id, @ActivityId, @ActivityName, @StudentId, @StudentName, @StudentNis,
            @AcademicYearId, @AcademicYearName, @Status, @JoinedDate,
            @CompletedAt, @WithdrawnAt, @LastSessionLog, @LastSessionDate,
            @CreatedAt, @UpdatedAt
        );
        """;

    private const string UpdateParticipationSql = """
        UPDATE dev.extracurricular_participations
        SET status = @Status,
            completed_at = @CompletedAt,
            withdrawn_at = @WithdrawnAt,
            last_session_log = @LastSessionLog,
            last_session_date = @LastSessionDate,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string ExistsByNameSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM dev.extracurricular_activities
            WHERE name = @Name
              AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        ) THEN 1 ELSE 0 END;
        """;

    public async Task<ExtracurricularActivity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            SelectByIdSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        var activityRow = await multi.ReadSingleOrDefaultAsync<ActivityRow>();
        if (activityRow is null)
        {
            return null;
        }

        var participationRows = await multi.ReadAsync<ParticipationRow>();
        var participations = participationRows.Select(ToDomainParticipation).ToList();

        return ToDomainActivity(activityRow, participations);
    }

    public async Task<ExtracurricularActivity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var activityRow = await connection.QuerySingleOrDefaultAsync<ActivityRow>(
            new CommandDefinition(SelectByNameSql, new { Name = name.Trim() }, cancellationToken: cancellationToken));

        if (activityRow is null)
        {
            return null;
        }

        var participationRows = await connection.QueryAsync<ParticipationRow>(
            new CommandDefinition(SelectParticipationsByActivityIdSql, new { ActivityId = activityRow.id }, cancellationToken: cancellationToken));

        var participations = participationRows.Select(ToDomainParticipation).ToList();

        return ToDomainActivity(activityRow, participations);
    }

    public async Task<IReadOnlyList<ExtracurricularActivity>> GetAllAsync(
        string? status = null,
        Guid? supervisorId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var activityRows = (await connection.QueryAsync<ActivityRow>(
            new CommandDefinition(
                SelectAllActivitiesSql,
                new { Status = status, SupervisorId = supervisorId },
                cancellationToken: cancellationToken))).ToList();

        if (activityRows.Count == 0)
        {
            return [];
        }

        var activityIds = activityRows.Select(a => a.id).ToArray();
        var participationRows = await connection.QueryAsync<ParticipationRow>(
            new CommandDefinition(
                $"SELECT {SelectParticipationColumns} FROM dev.extracurricular_participations WHERE activity_id IN @ActivityIds",
                new { ActivityIds = activityIds },
                cancellationToken: cancellationToken));

        var participationsByActivity = participationRows
            .Select(ToDomainParticipation)
            .GroupBy(p => p.ActivityId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<ExtracurricularActivity>();
        foreach (var act in activityRows)
        {
            participationsByActivity.TryGetValue(act.id, out var parts);
            result.Add(ToDomainActivity(act, parts ?? []));
        }

        return result.AsReadOnly();
    }

    public async Task<IReadOnlyList<ExtracurricularParticipation>> GetParticipationsByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<ParticipationRow>(
            new CommandDefinition(SelectParticipationsByStudentIdSql, new { StudentId = studentId }, cancellationToken: cancellationToken));

        return rows.Select(ToDomainParticipation).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<ExtracurricularParticipation>> GetParticipationsByActivityIdAsync(
        Guid activityId,
        Guid? academicYearId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<ParticipationRow>(
            new CommandDefinition(
                SelectParticipationsByActivityAndYearSql,
                new { ActivityId = activityId, AcademicYearId = academicYearId },
                cancellationToken: cancellationToken));

        return rows.Select(ToDomainParticipation).ToList().AsReadOnly();
    }

    public async Task<ExtracurricularParticipation?> GetParticipationByIdAsync(
        Guid participationId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ParticipationRow>(
            new CommandDefinition(SelectParticipationByIdSql, new { Id = participationId }, cancellationToken: cancellationToken));

        return row is null ? null : ToDomainParticipation(row);
    }

    public async Task InsertAsync(ExtracurricularActivity activity, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            InsertActivitySql,
            new
            {
                activity.Id,
                activity.Name,
                activity.Description,
                activity.SupervisorId,
                activity.SupervisorName,
                activity.Status,
                activity.CreatedAt,
                activity.UpdatedAt
            },
            cancellationToken: cancellationToken));

        foreach (var p in activity.Participations)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                InsertParticipationSql,
                ToParticipationParams(p),
                cancellationToken: cancellationToken));
        }
    }

    public async Task UpdateAsync(ExtracurricularActivity activity, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            UpdateActivitySql,
            new
            {
                activity.Id,
                activity.Name,
                activity.Description,
                activity.SupervisorId,
                activity.SupervisorName,
                activity.Status,
                activity.UpdatedAt
            },
            cancellationToken: cancellationToken));

        foreach (var p in activity.Participations)
        {
            var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
                UpdateParticipationSql,
                new
                {
                    p.Id,
                    p.Status,
                    p.CompletedAt,
                    p.WithdrawnAt,
                    p.LastSessionLog,
                    LastSessionDate = p.LastSessionDate.HasValue ? p.LastSessionDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    p.UpdatedAt
                },
                cancellationToken: cancellationToken));

            if (rowsAffected == 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    InsertParticipationSql,
                    ToParticipationParams(p),
                    cancellationToken: cancellationToken));
            }
        }
    }

    public async Task InsertParticipationAsync(ExtracurricularParticipation participation, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            InsertParticipationSql,
            ToParticipationParams(participation),
            cancellationToken: cancellationToken));
    }

    public async Task UpdateParticipationAsync(ExtracurricularParticipation participation, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            UpdateParticipationSql,
            new
            {
                participation.Id,
                participation.Status,
                participation.CompletedAt,
                participation.WithdrawnAt,
                participation.LastSessionLog,
                LastSessionDate = participation.LastSessionDate.HasValue
                    ? participation.LastSessionDate.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,
                participation.UpdatedAt
            },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            ExistsByNameSql,
            new { Name = name.Trim(), ExcludeId = excludeId },
            cancellationToken: cancellationToken));
    }

    private static object ToParticipationParams(ExtracurricularParticipation p) =>
        new
        {
            p.Id,
            p.ActivityId,
            p.ActivityName,
            p.StudentId,
            p.StudentName,
            p.StudentNis,
            p.AcademicYearId,
            p.AcademicYearName,
            p.Status,
            JoinedDate = p.JoinedDate.ToDateTime(TimeOnly.MinValue),
            p.CompletedAt,
            p.WithdrawnAt,
            p.LastSessionLog,
            LastSessionDate = p.LastSessionDate.HasValue ? p.LastSessionDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            p.CreatedAt,
            p.UpdatedAt
        };

    private static ExtracurricularActivity ToDomainActivity(ActivityRow row, IEnumerable<ExtracurricularParticipation> participations) =>
        ExtracurricularActivity.Rehydrate(
            row.id,
            row.name,
            row.description,
            row.supervisor_id,
            row.supervisor_name,
            row.status,
            DateTime.SpecifyKind(row.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(row.updated_at, DateTimeKind.Utc),
            participations);

    private static ExtracurricularParticipation ToDomainParticipation(ParticipationRow row) =>
        ExtracurricularParticipation.Rehydrate(
            row.id,
            row.activity_id,
            row.activity_name,
            row.student_id,
            row.student_name,
            row.student_nis,
            row.academic_year_id,
            row.academic_year_name,
            row.status,
            DateOnly.FromDateTime(row.joined_date),
            row.completed_at.HasValue ? DateTime.SpecifyKind(row.completed_at.Value, DateTimeKind.Utc) : null,
            row.withdrawn_at.HasValue ? DateTime.SpecifyKind(row.withdrawn_at.Value, DateTimeKind.Utc) : null,
            row.last_session_log,
            row.last_session_date.HasValue ? DateOnly.FromDateTime(row.last_session_date.Value) : null,
            DateTime.SpecifyKind(row.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(row.updated_at, DateTimeKind.Utc));

    private sealed class ActivityRow
    {
        public Guid id { get; set; }
        public string name { get; set; } = string.Empty;
        public string? description { get; set; }
        public Guid supervisor_id { get; set; }
        public string supervisor_name { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    private sealed class ParticipationRow
    {
        public Guid id { get; set; }
        public Guid activity_id { get; set; }
        public string activity_name { get; set; } = string.Empty;
        public Guid student_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string student_nis { get; set; } = string.Empty;
        public Guid academic_year_id { get; set; }
        public string academic_year_name { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public DateTime joined_date { get; set; }
        public DateTime? completed_at { get; set; }
        public DateTime? withdrawn_at { get; set; }
        public string? last_session_log { get; set; }
        public DateTime? last_session_date { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
