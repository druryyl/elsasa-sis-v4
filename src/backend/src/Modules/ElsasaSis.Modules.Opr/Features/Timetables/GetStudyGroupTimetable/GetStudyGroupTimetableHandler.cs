using Dapper;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Opr.Features.Timetables.GetStudyGroupTimetable;

public sealed class GetStudyGroupTimetableHandler(ISqlConnectionFactory connectionFactory)
{
    private const string QuerySql = """
        SELECT 
            s.id,
            s.operational_period_id,
            s.study_group_id,
            s.study_group_name,
            s.schedule_type,
            s.day_of_week,
            s.period_slot,
            s.start_time,
            s.end_time,
            s.subject_id,
            s.subject_name,
            s.teacher_id,
            s.teacher_name,
            s.room_id,
            s.room_name,
            s.created_at,
            s.updated_at,
            CASE WHEN EXISTS (
                SELECT 1 
                FROM opr.study_group_schedules other
                WHERE other.operational_period_id = s.operational_period_id
                  AND other.schedule_type = s.schedule_type
                  AND other.day_of_week = s.day_of_week
                  AND other.period_slot = s.period_slot
                  AND other.teacher_id = s.teacher_id
                  AND other.study_group_id <> s.study_group_id
            ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS teacher_conflict,
            CASE WHEN s.room_id IS NOT NULL AND EXISTS (
                SELECT 1 
                FROM opr.study_group_schedules other
                WHERE other.operational_period_id = s.operational_period_id
                  AND other.schedule_type = s.schedule_type
                  AND other.day_of_week = s.day_of_week
                  AND other.period_slot = s.period_slot
                  AND other.room_id = s.room_id
                  AND other.study_group_id <> s.study_group_id
            ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS room_conflict
        FROM opr.study_group_schedules s
        WHERE s.study_group_id = @StudyGroupId
          AND s.operational_period_id = @OperationalPeriodId
          AND (@ScheduleType IS NULL OR s.schedule_type = @ScheduleType)
        ORDER BY s.day_of_week, s.period_slot;
        """;

    public async Task<Result<IReadOnlyList<StudyGroupTimetableSlotDto>>> HandleAsync(
        GetStudyGroupTimetableQuery query,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<StudyGroupTimetableSlotRow>(
            new CommandDefinition(QuerySql, new
            {
                StudyGroupId = query.StudyGroupId,
                OperationalPeriodId = query.PeriodId,
                ScheduleType = string.IsNullOrWhiteSpace(query.ScheduleType) ? null : query.ScheduleType.Trim().ToUpperInvariant()
            }, cancellationToken: cancellationToken));

        var dtos = rows.Select(r => new StudyGroupTimetableSlotDto(
            r.id,
            r.operational_period_id,
            r.study_group_id,
            r.study_group_name,
            r.schedule_type,
            r.day_of_week,
            r.period_slot,
            TimeOnly.FromTimeSpan(r.start_time),
            TimeOnly.FromTimeSpan(r.end_time),
            r.subject_id,
            r.subject_name,
            r.teacher_id,
            r.teacher_name,
            r.room_id,
            r.room_name,
            r.teacher_conflict,
            r.room_conflict,
            DateTime.SpecifyKind(r.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(r.updated_at, DateTimeKind.Utc))).ToList();

        return Result<IReadOnlyList<StudyGroupTimetableSlotDto>>.Success(dtos);
    }

    private sealed class StudyGroupTimetableSlotRow
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
        public bool teacher_conflict { get; init; }
        public bool room_conflict { get; init; }
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
