using Dapper;
using ElsasaSis.Modules.Dev.Features.Achievements;
using ElsasaSis.Modules.Dev.Features.Behaviour;
using ElsasaSis.Modules.Dev.Features.Discipline;
using ElsasaSis.Modules.Dev.Features.Extracurricular;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Dev.Features.Dossier;

public sealed class GetStudentDossierHandler(ISqlConnectionFactory connectionFactory)
{
    private const string SelectBehaviourNotesSql = """
        SELECT 
            id, student_id, student_name, student_nis,
            academic_year_id, academic_year_name,
            behaviour_type, observed_date, description,
            recorder_id, recorder_name, created_at
        FROM dev.behaviour_notes
        WHERE student_id = @StudentId
        ORDER BY observed_date DESC, created_at DESC;
        """;

    private const string SelectDisciplineRecordsSql = """
        SELECT 
            id, student_id, student_name, student_nis,
            academic_year_id, academic_year_name,
            incident_date, violation_type, description,
            sanction, recorder_id, recorder_name, created_at
        FROM dev.discipline_records
        WHERE student_id = @StudentId
        ORDER BY incident_date DESC, created_at DESC;
        """;

    private const string SelectExtracurricularParticipationsSql = """
        SELECT 
            id, activity_id, activity_name, student_id, student_name, student_nis,
            academic_year_id, academic_year_name, status, joined_date,
            completed_at, withdrawn_at, last_session_log, last_session_date, created_at, updated_at
        FROM dev.extracurricular_participations
        WHERE student_id = @StudentId
        ORDER BY joined_date DESC, created_at DESC;
        """;

    private const string SelectAchievementsSql = """
        SELECT 
            id, student_id, student_name, student_nis,
            academic_year_id, academic_year_name,
            achievement_type, event_name, recognition_level,
            achievement_date, description, recorder_id, recorder_name, created_at
        FROM dev.achievements
        WHERE student_id = @StudentId
        ORDER BY achievement_date DESC, created_at DESC;
        """;

    public async Task<Result<StudentDossierDto>> HandleAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var param = new { StudentId = studentId };

        var behaviourRows = (await connection.QueryAsync<BehaviourNoteRow>(
            new CommandDefinition(SelectBehaviourNotesSql, param, cancellationToken: cancellationToken))).ToList();

        var disciplineRows = (await connection.QueryAsync<DisciplineRecordRow>(
            new CommandDefinition(SelectDisciplineRecordsSql, param, cancellationToken: cancellationToken))).ToList();

        var participationRows = (await connection.QueryAsync<ParticipationRow>(
            new CommandDefinition(SelectExtracurricularParticipationsSql, param, cancellationToken: cancellationToken))).ToList();

        var achievementRows = (await connection.QueryAsync<AchievementRow>(
            new CommandDefinition(SelectAchievementsSql, param, cancellationToken: cancellationToken))).ToList();

        var studentName = behaviourRows.FirstOrDefault()?.student_name
            ?? disciplineRows.FirstOrDefault()?.student_name
            ?? achievementRows.FirstOrDefault()?.student_name
            ?? participationRows.FirstOrDefault()?.student_name
            ?? string.Empty;

        var studentNis = behaviourRows.FirstOrDefault()?.student_nis
            ?? disciplineRows.FirstOrDefault()?.student_nis
            ?? achievementRows.FirstOrDefault()?.student_nis
            ?? participationRows.FirstOrDefault()?.student_nis
            ?? string.Empty;

        var behaviourNotes = behaviourRows.Select(r => new BehaviourNoteDto(
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

        var disciplineRecords = disciplineRows.Select(r => new DisciplineRecordDto(
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

        var participations = participationRows.Select(r => new ExtracurricularParticipationDto(
            r.id,
            r.activity_id,
            r.activity_name,
            r.student_id,
            r.student_name,
            r.student_nis,
            r.academic_year_id,
            r.academic_year_name,
            r.status,
            DateOnly.FromDateTime(r.joined_date),
            r.completed_at.HasValue ? DateTime.SpecifyKind(r.completed_at.Value, DateTimeKind.Utc) : null,
            r.withdrawn_at.HasValue ? DateTime.SpecifyKind(r.withdrawn_at.Value, DateTimeKind.Utc) : null,
            r.last_session_log,
            r.last_session_date.HasValue ? DateOnly.FromDateTime(r.last_session_date.Value) : null,
            DateTime.SpecifyKind(r.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(r.updated_at, DateTimeKind.Utc))).ToList();

        var achievements = achievementRows.Select(r => new AchievementDto(
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

        var timeline = new List<DossierTimelineItemDto>();

        foreach (var note in behaviourNotes)
        {
            timeline.Add(new DossierTimelineItemDto(
                note.Id,
                "BEHAVIOUR",
                note.ObservedDate,
                $"Catatan Perilaku ({note.BehaviourType})",
                note.Description,
                note.BehaviourType,
                note.RecorderName,
                note.CreatedAt));
        }

        foreach (var disc in disciplineRecords)
        {
            timeline.Add(new DossierTimelineItemDto(
                disc.Id,
                "DISCIPLINE",
                disc.IncidentDate,
                $"Pelanggaran Disiplin ({disc.ViolationType})",
                disc.Description,
                disc.Sanction,
                disc.RecorderName,
                disc.CreatedAt));
        }

        foreach (var part in participations)
        {
            timeline.Add(new DossierTimelineItemDto(
                part.Id,
                "EXTRACURRICULAR",
                part.JoinedDate,
                $"Ekstrakurikuler: {part.ActivityName}",
                part.LastSessionLog ?? $"Terdaftar pada kegiatan {part.ActivityName}",
                part.Status,
                string.Empty,
                part.CreatedAt));
        }

        foreach (var ach in achievements)
        {
            timeline.Add(new DossierTimelineItemDto(
                ach.Id,
                "ACHIEVEMENT",
                ach.AchievementDate,
                ach.EventName,
                ach.Description ?? string.Empty,
                $"{ach.AchievementType} - {ach.RecognitionLevel}",
                ach.RecorderName,
                ach.CreatedAt));
        }

        var sortedTimeline = timeline
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .ToList();

        return Result<StudentDossierDto>.Success(new StudentDossierDto(
            studentId,
            studentName,
            studentNis,
            behaviourNotes,
            disciplineRecords,
            participations,
            achievements,
            sortedTimeline));
    }

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
    }

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
    }
}
