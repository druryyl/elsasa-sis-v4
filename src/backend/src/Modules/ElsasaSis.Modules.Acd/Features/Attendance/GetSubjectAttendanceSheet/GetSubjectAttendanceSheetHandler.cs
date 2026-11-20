using Dapper;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Acd.Features.Attendance.GetSubjectAttendanceSheet;

public sealed class GetSubjectAttendanceSheetHandler(ISqlConnectionFactory connectionFactory)
{
    private const string SelectAttendanceSheetSql = """
        SELECT
            ap.student_id AS StudentId,
            COALESCE(s.full_name, ap.student_name) AS StudentName,
            COALESCE(s.nis, ap.student_nis) AS StudentNis,
            ar.id AS AttendanceRecordId,
            ar.status AS Status,
            ar.notes AS Notes,
            ar.recorded_by_teacher_id AS RecordedByTeacherId,
            ar.recorded_by_teacher_name AS RecordedByTeacherName,
            ar.updated_at AS UpdatedAt
        FROM (
            SELECT
                student_id,
                MIN(student_name) AS student_name,
                MIN(student_nis) AS student_nis
            FROM acd.academic_participations
            WHERE operational_period_id = @OperationalPeriodId
              AND subject_id = @SubjectId
              AND study_group_id = @StudyGroupId
              AND status = 'ACTIVE'
            GROUP BY student_id
        ) ap
        LEFT JOIN std.students s ON s.id = ap.student_id
        LEFT JOIN acd.attendance_records ar
            ON ar.student_id = ap.student_id
           AND ar.subject_id = @SubjectId
           AND ar.attendance_date = @AttendanceDate
        WHERE s.id IS NULL OR s.status = 'ACTIVE'
        ORDER BY COALESCE(s.full_name, ap.student_name) ASC;
        """;

    public async Task<Result<SubjectAttendanceSheetResponse>> HandleAsync(
        GetSubjectAttendanceSheetQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.SubjectId == Guid.Empty)
        {
            return Result<SubjectAttendanceSheetResponse>.Failure(AttendanceErrors.SubjectIdRequired());
        }

        if (query.StudyGroupId == Guid.Empty)
        {
            return Result<SubjectAttendanceSheetResponse>.Failure(AttendanceErrors.StudyGroupIdRequired());
        }

        if (query.OperationalPeriodId == Guid.Empty)
        {
            return Result<SubjectAttendanceSheetResponse>.Failure(AttendanceErrors.OperationalPeriodIdRequired());
        }

        if (query.AttendanceDate == default)
        {
            return Result<SubjectAttendanceSheetResponse>.Failure(AttendanceErrors.AttendanceDateRequired());
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<AttendanceSheetRow>(
            new CommandDefinition(
                SelectAttendanceSheetSql,
                new
                {
                    query.OperationalPeriodId,
                    query.SubjectId,
                    query.StudyGroupId,
                    AttendanceDate = query.AttendanceDate.ToDateTime(TimeOnly.MinValue)
                },
                cancellationToken: cancellationToken));

        var items = rows.Select(r => new AttendanceSheetItemDto(
            r.StudentId,
            r.StudentName,
            r.StudentNis,
            r.AttendanceRecordId,
            r.Status,
            r.Notes,
            r.RecordedByTeacherId,
            r.RecordedByTeacherName,
            r.UpdatedAt.HasValue ? DateTime.SpecifyKind(r.UpdatedAt.Value, DateTimeKind.Utc) : null
        )).ToList();

        var response = new SubjectAttendanceSheetResponse(
            query.SubjectId,
            query.StudyGroupId,
            query.AttendanceDate,
            query.OperationalPeriodId,
            items);

        return Result<SubjectAttendanceSheetResponse>.Success(response);
    }

    private sealed class AttendanceSheetRow
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentNis { get; set; } = string.Empty;
        public Guid? AttendanceRecordId { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public Guid? RecordedByTeacherId { get; set; }
        public string? RecordedByTeacherName { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
