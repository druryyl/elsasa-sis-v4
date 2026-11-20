using Dapper;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.GetReportCardById;

public sealed class GetReportCardByIdHandler(ISqlConnectionFactory connectionFactory)
{
    private const string SelectReportCardHeaderSql = """
        SELECT 
            id AS Id,
            student_id AS StudentId,
            student_name AS StudentName,
            student_nis AS StudentNis,
            student_nisn AS StudentNisn,
            academic_year_id AS AcademicYearId,
            academic_year_name AS AcademicYearName,
            operational_period_id AS OperationalPeriodId,
            operational_period_name AS OperationalPeriodName,
            study_group_id AS StudyGroupId,
            study_group_name AS StudyGroupName,
            homeroom_teacher_id AS HomeroomTeacherId,
            homeroom_teacher_name AS HomeroomTeacherName,
            status AS Status,
            override_reason AS OverrideReason,
            general_remarks AS GeneralRemarks,
            finalized_at AS FinalizedAt,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt
        FROM acd.report_cards
        WHERE id = @Id;
        """;

    private const string SelectReportCardEntriesSql = """
        SELECT 
            id AS Id,
            subject_id AS SubjectId,
            subject_name AS SubjectName,
            subject_code AS SubjectCode,
            subject_stage_id AS SubjectStageId,
            subject_stage_name AS SubjectStageName,
            teacher_id AS TeacherId,
            teacher_name AS TeacherName,
            numeric_score AS NumericScore,
            letter_grade AS LetterGrade,
            teacher_notes AS TeacherNotes
        FROM acd.report_card_entries
        WHERE report_card_id = @ReportCardId
        ORDER BY subject_name;
        """;

    private const string SelectAttendanceSummarySql = """
        SELECT 
            COUNT(CASE WHEN status = 'PRESENT' THEN 1 END) AS PresentCount,
            COUNT(CASE WHEN status = 'ABSENT' THEN 1 END) AS AbsentCount,
            COUNT(CASE WHEN status = 'SICK' THEN 1 END) AS SickCount,
            COUNT(CASE WHEN status = 'LATE' THEN 1 END) AS LateCount,
            COUNT(CASE WHEN status = 'EXCUSED' THEN 1 END) AS ExcusedCount
        FROM acd.attendance_records
        WHERE student_id = @StudentId
          AND operational_period_id = @OperationalPeriodId;
        """;

    public async Task<Result<ReportCardDetailDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var header = await connection.QuerySingleOrDefaultAsync<ReportCardHeaderRow>(
            new CommandDefinition(SelectReportCardHeaderSql, new { Id = id }, cancellationToken: cancellationToken));

        if (header is null)
        {
            return Result<ReportCardDetailDto>.Failure(AcdErrors.ReportCards.ReportCardNotFound(id));
        }

        var entries = (await connection.QueryAsync<ReportCardEntryDetailDto>(
            new CommandDefinition(SelectReportCardEntriesSql, new { ReportCardId = id }, cancellationToken: cancellationToken))).ToList();

        var attRow = await connection.QuerySingleOrDefaultAsync<AttendanceRow>(
            new CommandDefinition(
                SelectAttendanceSummarySql,
                new { StudentId = header.StudentId, OperationalPeriodId = header.OperationalPeriodId },
                cancellationToken: cancellationToken));

        var present = attRow?.PresentCount ?? 0;
        var absent = attRow?.AbsentCount ?? 0;
        var sick = attRow?.SickCount ?? 0;
        var late = attRow?.LateCount ?? 0;
        var excused = attRow?.ExcusedCount ?? 0;
        var totalDays = present + absent + sick + late + excused;

        var attendance = new AttendanceSummaryDto(present, absent, sick, late, excused, totalDays);

        var avgScore = entries.Count > 0 ? Math.Round(entries.Average(e => e.NumericScore), 2) : 0.00m;

        var detail = new ReportCardDetailDto(
            header.Id,
            header.StudentId,
            header.StudentName,
            header.StudentNis,
            header.StudentNisn,
            header.AcademicYearId,
            header.AcademicYearName,
            header.OperationalPeriodId,
            header.OperationalPeriodName,
            header.StudyGroupId,
            header.StudyGroupName,
            header.HomeroomTeacherId,
            header.HomeroomTeacherName,
            header.Status,
            header.OverrideReason,
            header.GeneralRemarks,
            header.FinalizedAt,
            header.CreatedAt,
            header.UpdatedAt,
            avgScore,
            entries.Count,
            attendance,
            entries);

        return Result<ReportCardDetailDto>.Success(detail);
    }

    private sealed record ReportCardHeaderRow(
        Guid Id,
        Guid StudentId,
        string StudentName,
        string StudentNis,
        string? StudentNisn,
        Guid AcademicYearId,
        string AcademicYearName,
        Guid OperationalPeriodId,
        string OperationalPeriodName,
        Guid? StudyGroupId,
        string? StudyGroupName,
        Guid? HomeroomTeacherId,
        string? HomeroomTeacherName,
        string Status,
        string? OverrideReason,
        string? GeneralRemarks,
        DateTime? FinalizedAt,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record AttendanceRow(
        int PresentCount,
        int AbsentCount,
        int SickCount,
        int LateCount,
        int ExcusedCount);
}
