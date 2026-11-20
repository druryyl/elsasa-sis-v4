using Dapper;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.Modules.Opr.Domain.Model;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Opr.Infrastructure.Persistence;

public sealed class StudyGroupTimetableRepository(ISqlConnectionFactory connectionFactory) : IStudyGroupTimetableRepository
{
    private const string SelectByIdSql = """
        SELECT id, operational_period_id, study_group_id, study_group_name, schedule_type,
               start_date, end_date, created_at, updated_at
        FROM opr.study_group_timetables
        WHERE id = @Id;
        """;

    private const string SelectByPeriodGroupDateSql = """
        SELECT id, operational_period_id, study_group_id, study_group_name, schedule_type,
               start_date, end_date, created_at, updated_at
        FROM opr.study_group_timetables
        WHERE operational_period_id = @OperationalPeriodId
          AND study_group_id = @StudyGroupId
          AND start_date = @StartDate;
        """;

    private const string SelectByStudyGroupIdSql = """
        SELECT id, operational_period_id, study_group_id, study_group_name, schedule_type,
               start_date, end_date, created_at, updated_at
        FROM opr.study_group_timetables
        WHERE study_group_id = @StudyGroupId
          AND operational_period_id = @OperationalPeriodId
        ORDER BY start_date;
        """;

    private const string InsertSql = """
        INSERT INTO opr.study_group_timetables (
            id, operational_period_id, study_group_id, study_group_name,
            schedule_type, start_date, end_date, created_at, updated_at
        ) VALUES (
            @Id, @OperationalPeriodId, @StudyGroupId, @StudyGroupName,
            @ScheduleType, @StartDate, @EndDate, @CreatedAt, @UpdatedAt
        );
        """;

    private const string UpdateSql = """
        UPDATE opr.study_group_timetables SET
            study_group_name = @StudyGroupName,
            schedule_type = @ScheduleType,
            end_date = @EndDate,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string UpsertSql = """
        MERGE opr.study_group_timetables AS target
        USING (VALUES (@OperationalPeriodId, @StudyGroupId, @StartDate))
            AS source (operational_period_id, study_group_id, start_date)
        ON target.operational_period_id = source.operational_period_id
           AND target.study_group_id = source.study_group_id
           AND target.start_date = source.start_date
        WHEN MATCHED THEN
            UPDATE SET
                study_group_name = @StudyGroupName,
                schedule_type = @ScheduleType,
                end_date = @EndDate,
                updated_at = @UpdatedAt
        WHEN NOT MATCHED THEN
            INSERT (
                id, operational_period_id, study_group_id, study_group_name,
                schedule_type, start_date, end_date, created_at, updated_at
            ) VALUES (
                @Id, @OperationalPeriodId, @StudyGroupId, @StudyGroupName,
                @ScheduleType, @StartDate, @EndDate, @CreatedAt, @UpdatedAt
            );
        """;

    private const string DeleteSql = """
        DELETE FROM opr.study_group_timetables
        WHERE id = @Id;
        """;

    public async Task<StudyGroupTimetable?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<StudyGroupTimetableRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : MapToEntity(row);
    }

    public async Task<StudyGroupTimetable?> GetByPeriodGroupDateAsync(
        Guid operationalPeriodId,
        Guid studyGroupId,
        DateOnly startDate,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<StudyGroupTimetableRow>(
            new CommandDefinition(SelectByPeriodGroupDateSql, new
            {
                OperationalPeriodId = operationalPeriodId,
                StudyGroupId = studyGroupId,
                StartDate = startDate.ToDateTime(TimeOnly.MinValue)
            }, cancellationToken: cancellationToken));

        return row is null ? null : MapToEntity(row);
    }

    public async Task<IReadOnlyList<StudyGroupTimetable>> GetByStudyGroupIdAsync(
        Guid studyGroupId,
        Guid operationalPeriodId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<StudyGroupTimetableRow>(
            new CommandDefinition(SelectByStudyGroupIdSql, new
            {
                StudyGroupId = studyGroupId,
                OperationalPeriodId = operationalPeriodId
            }, cancellationToken: cancellationToken));

        return rows.Select(MapToEntity).ToList();
    }

    public async Task AddAsync(StudyGroupTimetable timetable, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            InsertSql,
            MapToParameters(timetable),
            cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(StudyGroupTimetable timetable, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            UpdateSql,
            MapToParameters(timetable),
            cancellationToken: cancellationToken));
    }

    public async Task UpsertAsync(StudyGroupTimetable timetable, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            UpsertSql,
            MapToParameters(timetable),
            cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            DeleteSql,
            new { Id = id },
            cancellationToken: cancellationToken));
    }

    private static object MapToParameters(StudyGroupTimetable timetable) => new
    {
        timetable.Id,
        timetable.OperationalPeriodId,
        timetable.StudyGroupId,
        timetable.StudyGroupName,
        timetable.ScheduleType,
        StartDate = timetable.StartDate.ToDateTime(TimeOnly.MinValue),
        EndDate = timetable.EndDate.ToDateTime(TimeOnly.MinValue),
        timetable.CreatedAt,
        timetable.UpdatedAt
    };

    private static StudyGroupTimetable MapToEntity(StudyGroupTimetableRow row)
    {
        return StudyGroupTimetable.Rehydrate(
            row.id,
            row.operational_period_id,
            row.study_group_id,
            row.study_group_name,
            row.schedule_type,
            DateOnly.FromDateTime(row.start_date),
            DateOnly.FromDateTime(row.end_date),
            DateTime.SpecifyKind(row.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(row.updated_at, DateTimeKind.Utc));
    }

    private sealed class StudyGroupTimetableRow
    {
        public Guid id { get; init; }
        public Guid operational_period_id { get; init; }
        public Guid study_group_id { get; init; }
        public string study_group_name { get; init; } = string.Empty;
        public string schedule_type { get; init; } = string.Empty;
        public DateTime start_date { get; init; }
        public DateTime end_date { get; init; }
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
