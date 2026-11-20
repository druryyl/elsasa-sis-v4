using Dapper;
using ElsasaSis.Modules.Dev.Features.Achievements;
using ElsasaSis.Modules.Dev.Features.Behaviour;
using ElsasaSis.Modules.Dev.Features.Discipline;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Dev.Features.Overview;

public sealed class GetDevelopmentOverviewHandler(ISqlConnectionFactory connectionFactory)
{
    private const string SelectMetricsSql = """
        SELECT 
            (SELECT COUNT(1) FROM dev.behaviour_notes) AS TotalBehaviourNotes,
            (SELECT COUNT(1) FROM dev.behaviour_notes WHERE UPPER(behaviour_type) = 'POSITIVE') AS PositiveBehaviourNotes,
            (SELECT COUNT(1) FROM dev.behaviour_notes WHERE UPPER(behaviour_type) = 'NEGATIVE') AS NegativeBehaviourNotes,
            (SELECT COUNT(1) FROM dev.discipline_records) AS TotalDisciplineIncidents,
            (SELECT COUNT(1) FROM dev.discipline_records WHERE UPPER(sanction) = 'VERBAL_WARNING') AS VerbalWarnings,
            (SELECT COUNT(1) FROM dev.discipline_records WHERE UPPER(sanction) = 'WRITTEN_WARNING') AS WrittenWarnings,
            (SELECT COUNT(1) FROM dev.discipline_records WHERE UPPER(sanction) = 'PARENT_MEETING') AS ParentMeetings,
            (SELECT COUNT(1) FROM dev.discipline_records WHERE UPPER(sanction) = 'SUSPENSION') AS Suspensions,
            (SELECT COUNT(1) FROM dev.achievements) AS TotalAchievements,
            (SELECT COUNT(1) FROM dev.extracurricular_activities WHERE UPPER(status) = 'ACTIVE') AS ActiveExtracurricularActivities,
            (SELECT COUNT(1) FROM dev.extracurricular_participations WHERE UPPER(status) = 'ACTIVE') AS TotalExtracurricularParticipants;
        """;

    private const string SelectRecentBehaviourNotesSql = """
        SELECT TOP (5)
            id, student_id, student_name, student_nis,
            academic_year_id, academic_year_name,
            behaviour_type, observed_date, description,
            recorder_id, recorder_name, created_at
        FROM dev.behaviour_notes
        ORDER BY observed_date DESC, created_at DESC;
        """;

    private const string SelectRecentDisciplineRecordsSql = """
        SELECT TOP (5)
            id, student_id, student_name, student_nis,
            academic_year_id, academic_year_name,
            incident_date, violation_type, description,
            sanction, recorder_id, recorder_name, created_at
        FROM dev.discipline_records
        ORDER BY incident_date DESC, created_at DESC;
        """;

    private const string SelectRecentAchievementsSql = """
        SELECT TOP (5)
            id, student_id, student_name, student_nis,
            academic_year_id, academic_year_name,
            achievement_type, event_name, recognition_level,
            achievement_date, description, recorder_id, recorder_name, created_at
        FROM dev.achievements
        ORDER BY achievement_date DESC, created_at DESC;
        """;

    public async Task<Result<DevelopmentOverviewDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var metrics = await connection.QuerySingleAsync<OverviewMetricsRow>(
            new CommandDefinition(SelectMetricsSql, cancellationToken: cancellationToken));

        var behaviourRows = (await connection.QueryAsync<RecentBehaviourRow>(
            new CommandDefinition(SelectRecentBehaviourNotesSql, cancellationToken: cancellationToken))).ToList();

        var disciplineRows = (await connection.QueryAsync<RecentDisciplineRow>(
            new CommandDefinition(SelectRecentDisciplineRecordsSql, cancellationToken: cancellationToken))).ToList();

        var achievementRows = (await connection.QueryAsync<RecentAchievementRow>(
            new CommandDefinition(SelectRecentAchievementsSql, cancellationToken: cancellationToken))).ToList();

        var recentBehaviourNotes = behaviourRows.Select(r => new BehaviourNoteDto(
            r.id,
            r.student_id,
            r.student_name,
            r.student_nis,
            r.academic_year_id,
            r.academic_year_name,
            r.behaviour_type,
            DateOnly.FromDateTime(r.observed_date),
            r.description,
            r.recorder_id,
            r.recorder_name,
            DateTime.SpecifyKind(r.created_at, DateTimeKind.Utc))).ToList();

        var recentDisciplineRecords = disciplineRows.Select(r => new DisciplineRecordDto(
            r.id,
            r.student_id,
            r.student_name,
            r.student_nis,
            r.academic_year_id,
            r.academic_year_name,
            DateOnly.FromDateTime(r.incident_date),
            r.violation_type,
            r.description,
            r.sanction,
            r.recorder_id,
            r.recorder_name,
            DateTime.SpecifyKind(r.created_at, DateTimeKind.Utc))).ToList();

        var recentAchievements = achievementRows.Select(r => new AchievementDto(
            r.id,
            r.student_id,
            r.student_name,
            r.student_nis,
            r.academic_year_id,
            r.academic_year_name,
            r.achievement_type,
            r.event_name,
            r.recognition_level,
            DateOnly.FromDateTime(r.achievement_date),
            r.description,
            r.recorder_id,
            r.recorder_name,
            DateTime.SpecifyKind(r.created_at, DateTimeKind.Utc))).ToList();

        return Result<DevelopmentOverviewDto>.Success(new DevelopmentOverviewDto(
            metrics.TotalBehaviourNotes,
            metrics.PositiveBehaviourNotes,
            metrics.NegativeBehaviourNotes,
            metrics.TotalDisciplineIncidents,
            metrics.VerbalWarnings,
            metrics.WrittenWarnings,
            metrics.ParentMeetings,
            metrics.Suspensions,
            metrics.TotalAchievements,
            metrics.ActiveExtracurricularActivities,
            metrics.TotalExtracurricularParticipants,
            recentBehaviourNotes,
            recentDisciplineRecords,
            recentAchievements));
    }

    private sealed class OverviewMetricsRow
    {
        public int TotalBehaviourNotes { get; set; }
        public int PositiveBehaviourNotes { get; set; }
        public int NegativeBehaviourNotes { get; set; }
        public int TotalDisciplineIncidents { get; set; }
        public int VerbalWarnings { get; set; }
        public int WrittenWarnings { get; set; }
        public int ParentMeetings { get; set; }
        public int Suspensions { get; set; }
        public int TotalAchievements { get; set; }
        public int ActiveExtracurricularActivities { get; set; }
        public int TotalExtracurricularParticipants { get; set; }
    }

    private sealed class RecentBehaviourRow
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
    }

    private sealed class RecentDisciplineRow
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
    }

    private sealed class RecentAchievementRow
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
    }
}
