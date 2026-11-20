using Dapper;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Opr.Features.Timetables.GetTeacherPersonalSchedule;

public sealed class GetTeacherPersonalScheduleHandler(
    ICurrentUser currentUser,
    ISqlConnectionFactory connectionFactory)
{
    private const string QuerySql = """
        SELECT 
            id,
            operational_period_id,
            study_group_id,
            study_group_name,
            schedule_type,
            day_of_week,
            period_slot,
            start_time,
            end_time,
            subject_id,
            subject_name,
            teacher_id,
            teacher_name,
            room_id,
            room_name,
            created_at,
            updated_at
        FROM opr.study_group_schedules
        WHERE operational_period_id = @OperationalPeriodId
          AND teacher_id = @TeacherId
        ORDER BY schedule_type, day_of_week, period_slot;
        """;

    public async Task<Result<TeacherPersonalScheduleResponse>> HandleAsync(
        GetTeacherPersonalScheduleQuery query,
        CancellationToken cancellationToken = default)
    {
        var teacherId = currentUser.TeacherId;
        if (teacherId is null || teacherId == Guid.Empty)
        {
            return Result<TeacherPersonalScheduleResponse>.Failure(TimetableErrors.TeacherNotAssignedToUser());
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<TeacherScheduleSlotRow>(
            new CommandDefinition(QuerySql, new
            {
                OperationalPeriodId = query.PeriodId,
                TeacherId = teacherId.Value
            }, cancellationToken: cancellationToken));

        var groups = rows
            .GroupBy(r => r.schedule_type)
            .Select(g => new TeacherScheduleGroupDto(
                g.Key,
                g.Select(r => new TeacherPersonalScheduleSlotDto(
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
                    DateTime.SpecifyKind(r.created_at, DateTimeKind.Utc),
                    DateTime.SpecifyKind(r.updated_at, DateTimeKind.Utc))).ToList()))
            .ToList();

        return Result<TeacherPersonalScheduleResponse>.Success(
            new TeacherPersonalScheduleResponse(teacherId.Value, query.PeriodId, groups));
    }

    private sealed class TeacherScheduleSlotRow
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
