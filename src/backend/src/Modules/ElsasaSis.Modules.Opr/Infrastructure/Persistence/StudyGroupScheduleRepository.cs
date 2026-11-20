using Dapper;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.Modules.Opr.Domain.Model;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Opr.Infrastructure.Persistence;

public sealed class StudyGroupScheduleRepository(ISqlConnectionFactory connectionFactory) : IStudyGroupScheduleRepository
{
    private const string SelectByIdSql = """
        SELECT id, operational_period_id, study_group_id, study_group_name, schedule_type,
               day_of_week, period_slot, start_time, end_time, subject_id, subject_name,
               teacher_id, teacher_name, room_id, room_name, created_at, updated_at
        FROM opr.study_group_schedules
        WHERE id = @Id;
        """;

    private const string SelectBySlotSql = """
        SELECT id, operational_period_id, study_group_id, study_group_name, schedule_type,
               day_of_week, period_slot, start_time, end_time, subject_id, subject_name,
               teacher_id, teacher_name, room_id, room_name, created_at, updated_at
        FROM opr.study_group_schedules
        WHERE operational_period_id = @OperationalPeriodId
          AND study_group_id = @StudyGroupId
          AND schedule_type = @ScheduleType
          AND day_of_week = @DayOfWeek
          AND period_slot = @PeriodSlot;
        """;

    private const string InsertSql = """
        INSERT INTO opr.study_group_schedules (
            id, operational_period_id, study_group_id, study_group_name,
            schedule_type, day_of_week, period_slot, start_time, end_time,
            subject_id, subject_name, teacher_id, teacher_name, room_id, room_name,
            created_at, updated_at
        ) VALUES (
            @Id, @OperationalPeriodId, @StudyGroupId, @StudyGroupName,
            @ScheduleType, @DayOfWeek, @PeriodSlot, @StartTime, @EndTime,
            @SubjectId, @SubjectName, @TeacherId, @TeacherName, @RoomId, @RoomName,
            @CreatedAt, @UpdatedAt
        );
        """;

    private const string UpdateSql = """
        UPDATE opr.study_group_schedules SET
            study_group_name = @StudyGroupName,
            start_time = @StartTime,
            end_time = @EndTime,
            subject_id = @SubjectId,
            subject_name = @SubjectName,
            teacher_id = @TeacherId,
            teacher_name = @TeacherName,
            room_id = @RoomId,
            room_name = @RoomName,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string UpsertSql = """
        MERGE opr.study_group_schedules AS target
        USING (VALUES (@OperationalPeriodId, @StudyGroupId, @ScheduleType, @DayOfWeek, @PeriodSlot))
            AS source (operational_period_id, study_group_id, schedule_type, day_of_week, period_slot)
        ON target.operational_period_id = source.operational_period_id
           AND target.study_group_id = source.study_group_id
           AND target.schedule_type = source.schedule_type
           AND target.day_of_week = source.day_of_week
           AND target.period_slot = source.period_slot
        WHEN MATCHED THEN
            UPDATE SET
                study_group_name = @StudyGroupName,
                start_time = @StartTime,
                end_time = @EndTime,
                subject_id = @SubjectId,
                subject_name = @SubjectName,
                teacher_id = @TeacherId,
                teacher_name = @TeacherName,
                room_id = @RoomId,
                room_name = @RoomName,
                updated_at = @UpdatedAt
        WHEN NOT MATCHED THEN
            INSERT (
                id, operational_period_id, study_group_id, study_group_name,
                schedule_type, day_of_week, period_slot, start_time, end_time,
                subject_id, subject_name, teacher_id, teacher_name, room_id, room_name,
                created_at, updated_at
            ) VALUES (
                @Id, @OperationalPeriodId, @StudyGroupId, @StudyGroupName,
                @ScheduleType, @DayOfWeek, @PeriodSlot, @StartTime, @EndTime,
                @SubjectId, @SubjectName, @TeacherId, @TeacherName, @RoomId, @RoomName,
                @CreatedAt, @UpdatedAt
            );
        """;

    private const string DeleteSql = """
        DELETE FROM opr.study_group_schedules
        WHERE id = @Id;
        """;

    public async Task<StudyGroupSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<StudyGroupScheduleRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : MapToEntity(row);
    }

    public async Task<StudyGroupSchedule?> GetBySlotAsync(
        Guid operationalPeriodId,
        Guid studyGroupId,
        string scheduleType,
        int dayOfWeek,
        int periodSlot,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<StudyGroupScheduleRow>(
            new CommandDefinition(SelectBySlotSql, new
            {
                OperationalPeriodId = operationalPeriodId,
                StudyGroupId = studyGroupId,
                ScheduleType = scheduleType,
                DayOfWeek = dayOfWeek,
                PeriodSlot = periodSlot
            }, cancellationToken: cancellationToken));

        return row is null ? null : MapToEntity(row);
    }

    public async Task AddAsync(StudyGroupSchedule schedule, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            InsertSql,
            MapToParameters(schedule),
            cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(StudyGroupSchedule schedule, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            UpdateSql,
            MapToParameters(schedule),
            cancellationToken: cancellationToken));
    }

    public async Task UpsertAsync(StudyGroupSchedule schedule, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            UpsertSql,
            MapToParameters(schedule),
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

    private static object MapToParameters(StudyGroupSchedule schedule) => new
    {
        schedule.Id,
        schedule.OperationalPeriodId,
        schedule.StudyGroupId,
        schedule.StudyGroupName,
        schedule.ScheduleType,
        schedule.DayOfWeek,
        schedule.PeriodSlot,
        StartTime = schedule.StartTime.ToTimeSpan(),
        EndTime = schedule.EndTime.ToTimeSpan(),
        schedule.SubjectId,
        schedule.SubjectName,
        schedule.TeacherId,
        schedule.TeacherName,
        schedule.RoomId,
        schedule.RoomName,
        schedule.CreatedAt,
        schedule.UpdatedAt
    };

    private static StudyGroupSchedule MapToEntity(StudyGroupScheduleRow row)
    {
        return StudyGroupSchedule.Rehydrate(
            row.id,
            row.operational_period_id,
            row.study_group_id,
            row.study_group_name,
            row.schedule_type,
            row.day_of_week,
            row.period_slot,
            TimeOnly.FromTimeSpan(row.start_time),
            TimeOnly.FromTimeSpan(row.end_time),
            row.subject_id,
            row.subject_name,
            row.teacher_id,
            row.teacher_name,
            row.room_id,
            row.room_name,
            DateTime.SpecifyKind(row.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(row.updated_at, DateTimeKind.Utc));
    }

    private sealed class StudyGroupScheduleRow
    {
        public Guid id { get; init; }
        public Guid operational_period_id { get; init; }
        public Guid study_group_id { get; init; }
        public string study_group_name { get; init; } = string.Empty;
        public string schedule_type { get; init; } = string.Empty;
        public int day_of_week { get; init; }
        public int period_slot { get; init; }
        public TimeSpan start_time { get; init; }
        public TimeSpan end_time { get; init; }
        public Guid subject_id { get; init; }
        public string subject_name { get; init; } = string.Empty;
        public Guid teacher_id { get; init; }
        public string teacher_name { get; init; } = string.Empty;
        public Guid? room_id { get; init; }
        public string? room_name { get; init; }
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
