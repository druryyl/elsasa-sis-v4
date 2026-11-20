using Dapper;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using MediatR;

namespace ElsasaSis.Modules.Acd.Features.Monitoring.GetAcademicMonitoringProgress;

public sealed class GetAcademicMonitoringProgressHandler(ISqlConnectionFactory connectionFactory)
    : IRequestHandler<GetAcademicMonitoringProgressQuery, Result<AcademicMonitoringProgressDto>>
{
    private const string SelectActivePeriodSql = """
        SELECT TOP (1) 
            id AS Id,
            academic_year_id AS AcademicYearId,
            end_date AS EndDate
        FROM opr.operational_periods
        WHERE status = 'ACTIVE'
        ORDER BY start_date DESC;
        """;

    private const string SelectPeriodByIdSql = """
        SELECT 
            id AS Id,
            academic_year_id AS AcademicYearId,
            end_date AS EndDate
        FROM opr.operational_periods
        WHERE id = @PeriodId;
        """;

    private const string SelectActiveAcademicYearSql = """
        SELECT TOP (1) 
            id AS Id
        FROM opr.academic_years
        WHERE status = 'ACTIVE'
        ORDER BY start_date DESC;
        """;

    private const string SelectStudyGroupsSql = """
        SELECT 
            id AS StudyGroupId,
            name AS StudyGroupName,
            school_level AS SchoolLevel,
            grade AS Grade,
            homeroom_teacher_name AS HomeroomTeacherName
        FROM std.study_groups
        WHERE academic_year_id = @AcademicYearId
        ORDER BY school_level, grade, name;
        """;

    private const string SelectStudentPlacementsSql = """
        SELECT 
            study_group_id AS StudyGroupId,
            COUNT(DISTINCT student_id) AS EnrolledCount
        FROM std.student_placements
        WHERE academic_year_id = @AcademicYearId
          AND study_group_id IS NOT NULL
        GROUP BY study_group_id;
        """;

    private const string SelectAttendanceSummarySql = """
        SELECT 
            study_group_id AS StudyGroupId,
            COUNT(CASE WHEN status = 'PRESENT' THEN 1 END) AS PresentCount,
            COUNT(CASE WHEN status = 'ABSENT' THEN 1 END) AS AbsentCount,
            COUNT(CASE WHEN status = 'SICK' THEN 1 END) AS SickCount,
            COUNT(CASE WHEN status = 'LATE' THEN 1 END) AS LateCount,
            COUNT(CASE WHEN status = 'EXCUSED' THEN 1 END) AS ExcusedCount,
            COUNT(*) AS TotalCount
        FROM acd.attendance_records
        WHERE operational_period_id = @OperationalPeriodId
          AND attendance_date = @AttendanceDate
        GROUP BY study_group_id;
        """;

    private const string SelectAssignmentsWithScoresSql = """
        SELECT 
            a.id AS AssignmentId,
            a.title AS Title,
            a.subject_name AS SubjectName,
            a.teacher_name AS TeacherName,
            a.study_group_id AS StudyGroupId,
            a.study_group_name AS StudyGroupName,
            a.assigned_date AS AssignedDate,
            a.created_at AS CreatedAt,
            COUNT(s.id) AS ActualScoresCount
        FROM acd.assignments a
        LEFT JOIN acd.assignment_scores s ON s.assignment_id = a.id
        WHERE a.operational_period_id = @OperationalPeriodId
        GROUP BY 
            a.id, a.title, a.subject_name, a.teacher_name, a.study_group_id, 
            a.study_group_name, a.assigned_date, a.created_at;
        """;

    private const string SelectReportCardsSummarySql = """
        SELECT 
            study_group_id AS StudyGroupId,
            COUNT(CASE WHEN status = 'DRAFT' THEN 1 END) AS DraftCount,
            COUNT(CASE WHEN status = 'FINALIZED' THEN 1 END) AS FinalizedCount,
            COUNT(*) AS TotalCount
        FROM acd.report_cards
        WHERE operational_period_id = @OperationalPeriodId
        GROUP BY study_group_id;
        """;

    public async Task<Result<AcademicMonitoringProgressDto>> Handle(
        GetAcademicMonitoringProgressQuery request,
        CancellationToken cancellationToken) =>
        await HandleAsync(request, cancellationToken);

    public async Task<Result<AcademicMonitoringProgressDto>> HandleAsync(
        GetAcademicMonitoringProgressQuery query,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var academicYearId = query.AcademicYearId;
        var operationalPeriodId = query.OperationalPeriodId;
        var targetDate = query.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        DateTime? periodEndDate = null;

        if (!operationalPeriodId.HasValue)
        {
            var activePeriod = await connection.QuerySingleOrDefaultAsync<PeriodRow>(
                new CommandDefinition(SelectActivePeriodSql, cancellationToken: cancellationToken));

            if (activePeriod is not null)
            {
                operationalPeriodId = activePeriod.Id;
                academicYearId ??= activePeriod.AcademicYearId;
                periodEndDate = activePeriod.EndDate;
            }
        }
        else
        {
            var period = await connection.QuerySingleOrDefaultAsync<PeriodRow>(
                new CommandDefinition(SelectPeriodByIdSql, new { PeriodId = operationalPeriodId.Value }, cancellationToken: cancellationToken));

            if (period is not null)
            {
                academicYearId ??= period.AcademicYearId;
                periodEndDate = period.EndDate;
            }
        }

        if (!academicYearId.HasValue)
        {
            academicYearId = await connection.ExecuteScalarAsync<Guid?>(
                new CommandDefinition(SelectActiveAcademicYearSql, cancellationToken: cancellationToken));
        }

        if (!academicYearId.HasValue || !operationalPeriodId.HasValue)
        {
            return Result<AcademicMonitoringProgressDto>.Success(new AcademicMonitoringProgressDto(
                new AcademicMonitoringOverviewDto(0m, 0, 0, 0, 0, 0, 0, 0m, 0, 0, 0, 0m, 0, 0, 0),
                new AcademicMonitoringExceptionsDto([], [], []),
                []));
        }

        // 1. Fetch study groups for the academic year
        var studyGroups = (await connection.QueryAsync<StudyGroupRow>(
            new CommandDefinition(SelectStudyGroupsSql, new { AcademicYearId = academicYearId.Value }, cancellationToken: cancellationToken))).ToList();

        // 2. Fetch student placements per study group
        var placements = (await connection.QueryAsync<PlacementRow>(
            new CommandDefinition(SelectStudentPlacementsSql, new { AcademicYearId = academicYearId.Value }, cancellationToken: cancellationToken))).ToList();
        var enrolledMap = placements
            .Where(p => p.StudyGroupId.HasValue)
            .ToDictionary(p => p.StudyGroupId!.Value, p => p.EnrolledCount);
        var totalEnrolledStudents = enrolledMap.Values.Sum();

        // 3. Fetch attendance summary for today
        var attendanceRows = (await connection.QueryAsync<AttendanceSummaryRow>(
            new CommandDefinition(
                SelectAttendanceSummarySql,
                new
                {
                    OperationalPeriodId = operationalPeriodId.Value,
                    AttendanceDate = targetDate.ToDateTime(TimeOnly.MinValue)
                },
                cancellationToken: cancellationToken))).ToList();

        var attendanceMap = attendanceRows
            .Where(a => a.StudyGroupId.HasValue)
            .ToDictionary(a => a.StudyGroupId!.Value, a => a);

        var presentCount = attendanceRows.Sum(a => a.PresentCount);
        var absentCount = attendanceRows.Sum(a => a.AbsentCount);
        var sickCount = attendanceRows.Sum(a => a.SickCount);
        var lateCount = attendanceRows.Sum(a => a.LateCount);
        var excusedCount = attendanceRows.Sum(a => a.ExcusedCount);
        var attendedCount = presentCount + lateCount;

        var attendanceRateTodayPercent = totalEnrolledStudents > 0
            ? Math.Min(100.00m, Math.Round((decimal)attendedCount / totalEnrolledStudents * 100m, 2))
            : 0.00m;

        // Missing attendance alert: study groups with 0 attendance records logged today
        var missingAttendanceStudyGroups = new List<MissingAttendanceAlertDto>();
        foreach (var sg in studyGroups)
        {
            if (!attendanceMap.TryGetValue(sg.StudyGroupId, out var att) || att.TotalCount == 0)
            {
                missingAttendanceStudyGroups.Add(new MissingAttendanceAlertDto(
                    sg.StudyGroupId,
                    sg.StudyGroupName,
                    sg.SchoolLevel,
                    sg.Grade,
                    sg.HomeroomTeacherName));
            }
        }

        // 4. Fetch assignments and scores
        var assignments = (await connection.QueryAsync<AssignmentRow>(
            new CommandDefinition(SelectAssignmentsWithScoresSql, new { OperationalPeriodId = operationalPeriodId.Value }, cancellationToken: cancellationToken))).ToList();

        var totalAssignmentsCount = assignments.Count;
        var totalExpectedScores = 0;
        var totalActualScores = 0;
        var overdueGradingAssignments = new List<OverdueGradingAlertDto>();
        var overdueAssignmentIds = new HashSet<Guid>();

        foreach (var assignment in assignments)
        {
            var enrolledInGroup = assignment.StudyGroupId.HasValue && enrolledMap.TryGetValue(assignment.StudyGroupId.Value, out var count)
                ? count
                : 0;

            totalExpectedScores += enrolledInGroup;
            totalActualScores += assignment.ActualScoresCount;

            // Check overdue grading: created > 7 days ago and ActualScoresCount < enrolledInGroup
            var assignmentCreatedDate = DateOnly.FromDateTime(assignment.CreatedAt);
            var daysSinceCreation = targetDate.DayNumber - assignmentCreatedDate.DayNumber;

            if (daysSinceCreation > 7 && assignment.ActualScoresCount < enrolledInGroup && enrolledInGroup > 0)
            {
                var daysOverdue = Math.Max(1, daysSinceCreation - 7);
                var missingScores = enrolledInGroup - assignment.ActualScoresCount;

                overdueGradingAssignments.Add(new OverdueGradingAlertDto(
                    assignment.AssignmentId,
                    assignment.Title,
                    assignment.SubjectName,
                    assignment.TeacherName,
                    assignment.StudyGroupName ?? string.Empty,
                    daysOverdue,
                    missingScores));

                overdueAssignmentIds.Add(assignment.AssignmentId);
            }
        }

        var gradingCompletenessPercent = totalExpectedScores > 0
            ? Math.Min(100.00m, Math.Round((decimal)totalActualScores / totalExpectedScores * 100m, 2))
            : 0.00m;

        // 5. Fetch report cards summary
        var reportCardRows = (await connection.QueryAsync<ReportCardSummaryRow>(
            new CommandDefinition(SelectReportCardsSummarySql, new { OperationalPeriodId = operationalPeriodId.Value }, cancellationToken: cancellationToken))).ToList();

        var reportCardMap = reportCardRows
            .Where(r => r.StudyGroupId.HasValue)
            .ToDictionary(r => r.StudyGroupId!.Value, r => r);

        var draftReportCardsCount = reportCardRows.Sum(r => r.DraftCount);
        var finalizedReportCardsCount = reportCardRows.Sum(r => r.FinalizedCount);

        var reportCardsFinalizedPercent = totalEnrolledStudents > 0
            ? Math.Min(100.00m, Math.Round((decimal)finalizedReportCardsCount / totalEnrolledStudents * 100m, 2))
            : 0.00m;

        // Pending report cards alert: unfinalized draft report cards within 14 days of period end (or past)
        var pendingReportCards = new List<PendingReportCardAlertDto>();
        var isNearPeriodEnd = !periodEndDate.HasValue ||
            targetDate >= DateOnly.FromDateTime(periodEndDate.Value).AddDays(-14);

        if (isNearPeriodEnd)
        {
            foreach (var sg in studyGroups)
            {
                if (reportCardMap.TryGetValue(sg.StudyGroupId, out var rcRow) && rcRow.DraftCount > 0)
                {
                    var groupEnrolled = enrolledMap.GetValueOrDefault(sg.StudyGroupId, 0);
                    var totalCount = Math.Max(groupEnrolled, rcRow.TotalCount);

                    pendingReportCards.Add(new PendingReportCardAlertDto(
                        sg.StudyGroupId,
                        sg.StudyGroupName,
                        sg.SchoolLevel,
                        sg.Grade,
                        sg.HomeroomTeacherName,
                        rcRow.DraftCount,
                        totalCount));
                }
            }
        }

        // 6. Build study group monitoring rows
        var studyGroupRows = new List<StudyGroupMonitoringRowDto>();
        foreach (var sg in studyGroups)
        {
            var groupEnrolled = enrolledMap.GetValueOrDefault(sg.StudyGroupId, 0);

            // Attendance
            var groupAtt = attendanceMap.GetValueOrDefault(sg.StudyGroupId);
            var isAttendanceMissing = groupAtt is null || groupAtt.TotalCount == 0;
            var groupAttended = groupAtt is not null ? groupAtt.PresentCount + groupAtt.LateCount : 0;
            var groupAttendancePercent = groupEnrolled > 0
                ? Math.Min(100.00m, Math.Round((decimal)groupAttended / groupEnrolled * 100m, 2))
                : 0.00m;

            // Assignments
            var groupAssignments = assignments.Where(a => a.StudyGroupId == sg.StudyGroupId).ToList();
            var assignmentsCount = groupAssignments.Count;
            var groupExpected = assignmentsCount * groupEnrolled;
            var groupActual = groupAssignments.Sum(a => a.ActualScoresCount);
            var groupGradingPercent = groupExpected > 0
                ? Math.Min(100.00m, Math.Round((decimal)groupActual / groupExpected * 100m, 2))
                : 0.00m;

            var hasOverdueGrading = groupAssignments.Any(a => overdueAssignmentIds.Contains(a.AssignmentId));

            // Report Cards
            var groupRc = reportCardMap.GetValueOrDefault(sg.StudyGroupId);
            var draftCount = groupRc?.DraftCount ?? 0;
            var finalizedCount = groupRc?.FinalizedCount ?? 0;
            var totalCount = Math.Max(groupEnrolled, groupRc?.TotalCount ?? 0);
            var reportCardsRatio = $"{finalizedCount}/{totalCount}";
            var hasPendingReportCards = draftCount > 0;

            studyGroupRows.Add(new StudyGroupMonitoringRowDto(
                sg.StudyGroupId,
                sg.StudyGroupName,
                sg.SchoolLevel,
                sg.Grade,
                sg.HomeroomTeacherName,
                groupAttendancePercent,
                assignmentsCount,
                groupGradingPercent,
                reportCardsRatio,
                isAttendanceMissing,
                hasOverdueGrading,
                hasPendingReportCards));
        }

        var overview = new AcademicMonitoringOverviewDto(
            attendanceRateTodayPercent,
            totalEnrolledStudents,
            presentCount,
            absentCount,
            sickCount,
            lateCount,
            excusedCount,
            gradingCompletenessPercent,
            totalAssignmentsCount,
            totalExpectedScores,
            totalActualScores,
            reportCardsFinalizedPercent,
            totalEnrolledStudents,
            draftReportCardsCount,
            finalizedReportCardsCount);

        var exceptions = new AcademicMonitoringExceptionsDto(
            missingAttendanceStudyGroups,
            overdueGradingAssignments,
            pendingReportCards);

        return Result<AcademicMonitoringProgressDto>.Success(
            new AcademicMonitoringProgressDto(overview, exceptions, studyGroupRows));
    }

    private sealed record PeriodRow(Guid Id, Guid AcademicYearId, DateTime? EndDate);
    private sealed record StudyGroupRow(Guid StudyGroupId, string StudyGroupName, string SchoolLevel, string Grade, string? HomeroomTeacherName);
    private sealed record PlacementRow(Guid? StudyGroupId, int EnrolledCount);
    private sealed record AttendanceSummaryRow(
        Guid? StudyGroupId,
        int PresentCount,
        int AbsentCount,
        int SickCount,
        int LateCount,
        int ExcusedCount,
        int TotalCount);
    private sealed record AssignmentRow(
        Guid AssignmentId,
        string Title,
        string SubjectName,
        string? TeacherName,
        Guid? StudyGroupId,
        string? StudyGroupName,
        DateTime AssignedDate,
        DateTime CreatedAt,
        int ActualScoresCount);
    private sealed record ReportCardSummaryRow(
        Guid? StudyGroupId,
        int DraftCount,
        int FinalizedCount,
        int TotalCount);
}
