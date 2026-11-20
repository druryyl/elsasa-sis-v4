using Dapper;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;
using MediatR;

namespace ElsasaSis.Modules.Opr.Features.Timetables.GetPersonalStudentTimetable;

public sealed class GetPersonalStudentTimetableHandler(
    ICurrentUser currentUser,
    ISqlConnectionFactory connectionFactory,
    IOperationalContract operationalContract)
    : IRequestHandler<GetPersonalStudentTimetableQuery, Result<StudentPersonalScheduleResponse>>
{
    private const string StudentInfoSql = """
        SELECT 
            s.id AS StudentId,
            s.full_name AS FullName,
            s.nis AS Nis,
            sp.study_group_id AS StudyGroupId,
            sp.study_group_name AS StudyGroupName,
            sp.school_level AS SchoolLevel,
            sp.grade AS Grade
        FROM std.students s
        LEFT JOIN std.student_placements sp 
               ON sp.student_id = s.id 
              AND sp.academic_year_id = @AcademicYearId
        WHERE s.id = @StudentId;
        """;

    private const string SmkCycleSql = """
        SELECT schedule_type, start_date, end_date
        FROM opr.study_group_timetables
        WHERE study_group_id = @StudyGroupId
          AND operational_period_id = @PeriodId
          AND @QueryDate BETWEEN start_date AND end_date;
        """;

    private const string SchedulesSql = """
        SELECT 
            s.id,
            s.day_of_week,
            s.period_slot,
            s.start_time,
            s.end_time,
            s.subject_id,
            s.subject_name,
            COALESCE(sub.code, N'') AS subject_code,
            s.teacher_id,
            s.teacher_name,
            s.room_id,
            s.room_name,
            s.schedule_type
        FROM opr.study_group_schedules s
        LEFT JOIN acd.subjects sub ON sub.id = s.subject_id
        WHERE s.study_group_id = @StudyGroupId
          AND s.operational_period_id = @PeriodId
        ORDER BY s.day_of_week, s.period_slot;
        """;

    public async Task<Result<StudentPersonalScheduleResponse>> Handle(
        GetPersonalStudentTimetableQuery request,
        CancellationToken cancellationToken)
    {
        return await HandleAsync(request, cancellationToken);
    }

    public async Task<Result<StudentPersonalScheduleResponse>> HandleAsync(
        GetPersonalStudentTimetableQuery query,
        CancellationToken cancellationToken = default)
    {
        var studentId = currentUser.StudentId;
        if (!studentId.HasValue || studentId.Value == Guid.Empty)
        {
            return Result<StudentPersonalScheduleResponse>.Failure(
                DomainError.Forbidden("Auth.Forbidden", "The authenticated user is not associated with an active student profile."));
        }

        var activePeriod = await operationalContract.GetActiveOperationalPeriodAsync(cancellationToken);
        if (activePeriod is null)
        {
            return Result<StudentPersonalScheduleResponse>.Failure(
                DomainError.NotFound("Opr.Timetable.ActivePeriodNotFound", "No active operational period found."));
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var studentRow = await connection.QuerySingleOrDefaultAsync<StudentPlacementRow>(
            new CommandDefinition(StudentInfoSql, new
            {
                StudentId = studentId.Value,
                AcademicYearId = activePeriod.AcademicYearId
            }, cancellationToken: cancellationToken));

        if (studentRow is null)
        {
            return Result<StudentPersonalScheduleResponse>.Failure(
                DomainError.NotFound("Opr.Timetable.StudentNotFound", $"Student '{studentId.Value}' was not found."));
        }

        // If unplaced in a study group, return empty timetable with placement pending info
        if (!studentRow.StudyGroupId.HasValue || studentRow.StudyGroupId.Value == Guid.Empty)
        {
            var unplacedInfo = new StudentTimetableInfoDto(
                studentRow.StudentId,
                studentRow.FullName,
                studentRow.Nis,
                null,
                null,
                studentRow.SchoolLevel,
                studentRow.Grade);

            var emptyCycle = new ActiveSmkCycleDto("REGULAR", false, null, null);

            return Result<StudentPersonalScheduleResponse>.Success(
                new StudentPersonalScheduleResponse(
                    unplacedInfo,
                    emptyCycle,
                    Array.Empty<ScheduleSlotDto>(),
                    Array.Empty<ScheduleSlotDto>()));
        }

        var studyGroupId = studentRow.StudyGroupId.Value;
        var targetDate = query.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var cycleRow = await connection.QuerySingleOrDefaultAsync<SmkCycleRow>(
            new CommandDefinition(SmkCycleSql, new
            {
                StudyGroupId = studyGroupId,
                PeriodId = activePeriod.Id,
                QueryDate = targetDate.ToDateTime(TimeOnly.MinValue)
            }, cancellationToken: cancellationToken));

        string activeScheduleType;
        bool isSmkRotation;
        DateOnly? startDate = null;
        DateOnly? endDate = null;

        if (cycleRow is not null)
        {
            activeScheduleType = cycleRow.schedule_type;
            isSmkRotation = true;
            startDate = DateOnly.FromDateTime(cycleRow.start_date);
            endDate = DateOnly.FromDateTime(cycleRow.end_date);
        }
        else
        {
            activeScheduleType = "REGULAR";
            isSmkRotation = false;
        }

        var activeCycleDto = new ActiveSmkCycleDto(activeScheduleType, isSmkRotation, startDate, endDate);

        var studentInfo = new StudentTimetableInfoDto(
            studentRow.StudentId,
            studentRow.FullName,
            studentRow.Nis,
            studentRow.StudyGroupId,
            studentRow.StudyGroupName,
            studentRow.SchoolLevel,
            studentRow.Grade);

        var scheduleRows = await connection.QueryAsync<ScheduleSlotRow>(
            new CommandDefinition(SchedulesSql, new
            {
                StudyGroupId = studyGroupId,
                PeriodId = activePeriod.Id
            }, cancellationToken: cancellationToken));

        // Filter slots for the active cycle (activeScheduleType OR 'REGULAR')
        var activeSlots = scheduleRows
            .Where(s => string.Equals(s.schedule_type, activeScheduleType, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(s.schedule_type, "REGULAR", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var weeklySchedule = activeSlots
            .OrderBy(s => s.day_of_week)
            .ThenBy(s => s.period_slot)
            .Select(MapToSlotDto)
            .ToList();

        var targetDayOfWeek = targetDate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)targetDate.DayOfWeek;

        var todayAgenda = activeSlots
            .Where(s => s.day_of_week == targetDayOfWeek)
            .OrderBy(s => s.period_slot)
            .Select(MapToSlotDto)
            .ToList();

        return Result<StudentPersonalScheduleResponse>.Success(
            new StudentPersonalScheduleResponse(
                studentInfo,
                activeCycleDto,
                todayAgenda,
                weeklySchedule));
    }

    private static ScheduleSlotDto MapToSlotDto(ScheduleSlotRow r) =>
        new(
            r.id,
            FormatDayOfWeek(r.day_of_week),
            r.period_slot,
            r.start_time.ToString(@"hh\:mm"),
            r.end_time.ToString(@"hh\:mm"),
            r.subject_id,
            r.subject_name,
            r.subject_code,
            r.teacher_id,
            r.teacher_name,
            r.room_id,
            r.room_name,
            r.schedule_type);

    private static string FormatDayOfWeek(int day) => day switch
    {
        1 => "Monday",
        2 => "Tuesday",
        3 => "Wednesday",
        4 => "Thursday",
        5 => "Friday",
        6 => "Saturday",
        7 => "Sunday",
        _ => day.ToString()
    };

    private sealed class StudentPlacementRow
    {
        public Guid StudentId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Nis { get; init; } = string.Empty;
        public Guid? StudyGroupId { get; init; }
        public string? StudyGroupName { get; init; }
        public string? SchoolLevel { get; init; }
        public string? Grade { get; init; }
    }

    private sealed class SmkCycleRow
    {
        public string schedule_type { get; init; } = string.Empty;
        public DateTime start_date { get; init; }
        public DateTime end_date { get; init; }
    }

    private sealed class ScheduleSlotRow
    {
        public Guid id { get; init; }
        public int day_of_week { get; init; }
        public int period_slot { get; init; }
        public TimeSpan start_time { get; init; }
        public TimeSpan end_time { get; init; }
        public Guid subject_id { get; init; }
        public string subject_name { get; init; } = string.Empty;
        public string subject_code { get; init; } = string.Empty;
        public Guid teacher_id { get; init; }
        public string teacher_name { get; init; } = string.Empty;
        public Guid? room_id { get; init; }
        public string? room_name { get; init; }
        public string schedule_type { get; init; } = string.Empty;
    }
}
