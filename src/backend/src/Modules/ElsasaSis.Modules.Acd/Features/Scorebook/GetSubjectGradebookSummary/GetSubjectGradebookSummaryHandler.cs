using Dapper;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Acd.Features.Scorebook.GetSubjectGradebookSummary;

public sealed class GetSubjectGradebookSummaryHandler(ISqlConnectionFactory connectionFactory)
{
    private const string GradebookQuerySql = """
        -- Result 1: Metadata
        SELECT TOP (1)
            ss.id AS SubjectStageId,
            ss.name AS SubjectStageName,
            s.id AS SubjectId,
            s.name AS SubjectName,
            sg.id AS StudyGroupId,
            sg.name AS StudyGroupName,
            op.id AS OperationalPeriodId,
            op.name AS OperationalPeriodName
        FROM acd.subject_stages ss
        INNER JOIN acd.subjects s ON s.id = ss.subject_id
        INNER JOIN std.study_groups sg ON sg.id = @StudyGroupId
        INNER JOIN opr.operational_periods op ON op.id = @OperationalPeriodId
        WHERE ss.id = @SubjectStageId;

        -- Result 2: Assignments
        SELECT 
            id,
            title,
            assignment_type,
            max_score,
            weight,
            assigned_date,
            due_date,
            learning_unit_id,
            learning_unit_name
        FROM acd.assignments
        WHERE subject_stage_id = @SubjectStageId
          AND study_group_id = @StudyGroupId
          AND operational_period_id = @OperationalPeriodId
        ORDER BY assigned_date ASC, created_at ASC;

        -- Result 3: Students Roster with Running Score & Attendance Metrics
        WITH AssessmentWeights AS (
            SELECT 
                COUNT(CASE WHEN weight IS NULL THEN 1 END) AS null_weight_count
            FROM acd.assignments
            WHERE subject_stage_id = @SubjectStageId
              AND study_group_id = @StudyGroupId
              AND operational_period_id = @OperationalPeriodId
        ),
        StudentScores AS (
            SELECT 
                sc.student_id,
                CASE 
                    WHEN aw.null_weight_count > 0 THEN CAST(ROUND(AVG(sc.score), 2) AS DECIMAL(5,2))
                    ELSE CAST(ROUND(SUM(sc.score * a.weight) / NULLIF(SUM(a.weight), 0), 2) AS DECIMAL(5,2))
                END AS running_score
            FROM acd.assignment_scores sc
            INNER JOIN acd.assignments a 
                ON a.id = sc.assignment_id
            CROSS JOIN AssessmentWeights aw
            WHERE a.subject_stage_id = @SubjectStageId
              AND a.study_group_id = @StudyGroupId
              AND a.operational_period_id = @OperationalPeriodId
            GROUP BY sc.student_id, aw.null_weight_count
        ),
        StudentAttendance AS (
            SELECT 
                ar.student_id,
                COUNT(CASE WHEN ar.status = 'PRESENT' THEN 1 END) AS present_count,
                COUNT(CASE WHEN ar.status = 'ABSENT' THEN 1 END) AS absent_count,
                COUNT(CASE WHEN ar.status = 'LATE' THEN 1 END) AS late_count,
                COUNT(CASE WHEN ar.status = 'EXCUSED' THEN 1 END) AS excused_count,
                COUNT(CASE WHEN ar.status = 'SICK' THEN 1 END) AS sick_count,
                COUNT(ar.id) AS total_count
            FROM acd.attendance_records ar
            INNER JOIN acd.academic_participations ap
                ON ap.student_id = ar.student_id
               AND ap.subject_id = ar.subject_id
               AND ap.operational_period_id = ar.operational_period_id
            WHERE ap.subject_stage_id = @SubjectStageId
              AND ap.study_group_id = @StudyGroupId
              AND ap.operational_period_id = @OperationalPeriodId
              AND ap.status = 'ACTIVE'
            GROUP BY ar.student_id
        )
        SELECT 
            ap.student_id AS StudentId,
            ap.student_name AS StudentName,
            ap.student_nis AS StudentNis,
            ss.running_score AS RunningScore,
            COALESCE(sa.present_count, 0) AS PresentCount,
            COALESCE(sa.absent_count, 0) AS AbsentCount,
            COALESCE(sa.late_count, 0) AS LateCount,
            COALESCE(sa.excused_count, 0) AS ExcusedCount,
            COALESCE(sa.sick_count, 0) AS SickCount,
            COALESCE(sa.total_count, 0) AS TotalRecorded
        FROM acd.academic_participations ap
        LEFT JOIN StudentScores ss ON ss.student_id = ap.student_id
        LEFT JOIN StudentAttendance sa ON sa.student_id = ap.student_id
        WHERE ap.subject_stage_id = @SubjectStageId
          AND ap.study_group_id = @StudyGroupId
          AND ap.operational_period_id = @OperationalPeriodId
          AND ap.status = 'ACTIVE'
        ORDER BY ap.student_name ASC;

        -- Result 4: Student Scores
        SELECT 
            sc.student_id AS StudentId,
            sc.assignment_id AS AssignmentId,
            sc.score AS Score,
            sc.feedback AS Feedback,
            sc.graded_at AS GradedAt
        FROM acd.assignment_scores sc
        INNER JOIN acd.assignments a ON a.id = sc.assignment_id
        INNER JOIN acd.academic_participations ap 
            ON ap.student_id = sc.student_id 
           AND ap.subject_stage_id = a.subject_stage_id 
           AND ap.study_group_id = a.study_group_id 
           AND ap.operational_period_id = a.operational_period_id
        WHERE a.subject_stage_id = @SubjectStageId
          AND a.study_group_id = @StudyGroupId
          AND a.operational_period_id = @OperationalPeriodId
          AND ap.status = 'ACTIVE';
        """;

    public async Task<Result<GradebookSummaryResponse>> HandleAsync(
        GetSubjectGradebookSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.SubjectStageId == Guid.Empty)
        {
            return Result<GradebookSummaryResponse>.Failure(ScorebookErrors.SubjectStageIdRequired());
        }

        if (query.StudyGroupId == Guid.Empty)
        {
            return Result<GradebookSummaryResponse>.Failure(ScorebookErrors.StudyGroupIdRequired());
        }

        if (query.OperationalPeriodId == Guid.Empty)
        {
            return Result<GradebookSummaryResponse>.Failure(ScorebookErrors.OperationalPeriodIdRequired());
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            GradebookQuerySql,
            new
            {
                query.SubjectStageId,
                query.StudyGroupId,
                query.OperationalPeriodId
            },
            cancellationToken: cancellationToken));

        var meta = await multi.ReadSingleOrDefaultAsync<MetadataRow>();
        if (meta is null)
        {
            return Result<GradebookSummaryResponse>.Failure(ScorebookErrors.SubjectStageNotFound(query.SubjectStageId));
        }

        var assignmentRows = (await multi.ReadAsync<AssignmentRow>()).ToList();
        var studentRows = (await multi.ReadAsync<StudentRow>()).ToList();
        var scoreRows = (await multi.ReadAsync<ScoreRow>()).ToList();

        var assignments = assignmentRows.Select(a => new GradebookAssignmentDto(
            a.id,
            a.title,
            a.assignment_type,
            a.max_score,
            a.weight,
            DateOnly.FromDateTime(a.assigned_date),
            a.due_date.HasValue ? DateOnly.FromDateTime(a.due_date.Value) : null,
            a.learning_unit_id,
            a.learning_unit_name)).ToList();

        var scoreMap = scoreRows
            .GroupBy(s => s.StudentId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => new GradebookStudentScoreDto(s.AssignmentId, s.Score, s.Feedback, s.GradedAt)).ToList());

        var students = studentRows.Select(s =>
        {
            var studentScores = scoreMap.TryGetValue(s.StudentId, out var list) ? list : new List<GradebookStudentScoreDto>();
            var attendance = new GradebookAttendanceMetricsDto(
                s.PresentCount,
                s.AbsentCount,
                s.LateCount,
                s.ExcusedCount,
                s.SickCount,
                s.TotalRecorded);

            return new GradebookStudentRowDto(
                s.StudentId,
                s.StudentName,
                s.StudentNis,
                s.RunningScore,
                attendance,
                studentScores);
        }).ToList();

        return Result<GradebookSummaryResponse>.Success(new GradebookSummaryResponse(
            meta.SubjectStageId,
            meta.SubjectStageName,
            meta.SubjectId,
            meta.SubjectName,
            meta.StudyGroupId,
            meta.StudyGroupName,
            meta.OperationalPeriodId,
            meta.OperationalPeriodName,
            assignments,
            students));
    }

    private sealed record MetadataRow(
        Guid SubjectStageId,
        string SubjectStageName,
        Guid SubjectId,
        string SubjectName,
        Guid StudyGroupId,
        string StudyGroupName,
        Guid OperationalPeriodId,
        string OperationalPeriodName);

    private sealed record AssignmentRow(
        Guid id,
        string title,
        string assignment_type,
        decimal max_score,
        decimal? weight,
        DateTime assigned_date,
        DateTime? due_date,
        Guid? learning_unit_id,
        string? learning_unit_name);

    private sealed record StudentRow(
        Guid StudentId,
        string StudentName,
        string StudentNis,
        decimal? RunningScore,
        int PresentCount,
        int AbsentCount,
        int LateCount,
        int ExcusedCount,
        int SickCount,
        int TotalRecorded);

    private sealed record ScoreRow(
        Guid StudentId,
        Guid AssignmentId,
        decimal Score,
        string? Feedback,
        DateTime GradedAt);
}
