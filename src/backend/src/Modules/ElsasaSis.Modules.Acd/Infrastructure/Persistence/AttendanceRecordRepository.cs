using System.Data.Common;
using Dapper;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Acd.Infrastructure.Persistence;

public sealed class AttendanceRecordRepository(ISqlConnectionFactory connectionFactory) : IAttendanceRecordRepository
{
    private const string SelectColumns = """
        id, student_id, student_name, student_nis, subject_id, subject_name,
        study_group_id, study_group_name, operational_period_id, operational_period_name,
        attendance_date, status, notes, recorded_by_teacher_id, recorded_by_teacher_name,
        created_at, updated_at
        """;

    private const string SelectByStudentSubjectAndDateSql = $"""
        SELECT {SelectColumns}
        FROM acd.attendance_records
        WHERE student_id = @StudentId
          AND subject_id = @SubjectId
          AND attendance_date = @AttendanceDate;
        """;

    private const string SelectBySubjectGroupAndDateSql = $"""
        SELECT {SelectColumns}
        FROM acd.attendance_records
        WHERE subject_id = @SubjectId
          AND study_group_id = @StudyGroupId
          AND attendance_date = @AttendanceDate;
        """;

    private const string SelectEnrolledStudentsSql = """
        SELECT
            ap.student_id AS StudentId,
            COALESCE(s.full_name, ap.student_name) AS StudentName,
            COALESCE(s.nis, ap.student_nis) AS StudentNis
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
        WHERE s.id IS NULL OR s.status = 'ACTIVE'
        ORDER BY COALESCE(s.full_name, ap.student_name) ASC;
        """;

    private const string UpsertAttendanceRecordSql = """
        UPDATE acd.attendance_records WITH (UPDLOCK, HOLDLOCK)
        SET student_name = @StudentName,
            student_nis = @StudentNis,
            subject_name = @SubjectName,
            study_group_id = @StudyGroupId,
            study_group_name = @StudyGroupName,
            operational_period_id = @OperationalPeriodId,
            operational_period_name = @OperationalPeriodName,
            status = @Status,
            notes = @Notes,
            recorded_by_teacher_id = @RecordedByTeacherId,
            recorded_by_teacher_name = @RecordedByTeacherName,
            updated_at = @UpdatedAt
        WHERE student_id = @StudentId
          AND subject_id = @SubjectId
          AND attendance_date = @AttendanceDate;

        IF @@ROWCOUNT = 0
        BEGIN
            INSERT INTO acd.attendance_records (
                id, student_id, student_name, student_nis,
                subject_id, subject_name,
                study_group_id, study_group_name,
                operational_period_id, operational_period_name,
                attendance_date, status, notes,
                recorded_by_teacher_id, recorded_by_teacher_name,
                created_at, updated_at
            )
            VALUES (
                @Id, @StudentId, @StudentName, @StudentNis,
                @SubjectId, @SubjectName,
                @StudyGroupId, @StudyGroupName,
                @OperationalPeriodId, @OperationalPeriodName,
                @AttendanceDate, @Status, @Notes,
                @RecordedByTeacherId, @RecordedByTeacherName,
                @CreatedAt, @UpdatedAt
            );
        END;
        """;

    public async Task<AttendanceRecord?> GetByStudentSubjectAndDateAsync(
        Guid studentId,
        Guid subjectId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<AttendanceRecordRow>(
            new CommandDefinition(
                SelectByStudentSubjectAndDateSql,
                new
                {
                    StudentId = studentId,
                    SubjectId = subjectId,
                    AttendanceDate = attendanceDate.ToDateTime(TimeOnly.MinValue)
                },
                cancellationToken: cancellationToken));

        return row is null ? null : MapRow(row);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> GetBySubjectGroupAndDateAsync(
        Guid subjectId,
        Guid studyGroupId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<AttendanceRecordRow>(
            new CommandDefinition(
                SelectBySubjectGroupAndDateSql,
                new
                {
                    SubjectId = subjectId,
                    StudyGroupId = studyGroupId,
                    AttendanceDate = attendanceDate.ToDateTime(TimeOnly.MinValue)
                },
                cancellationToken: cancellationToken));

        return rows.Select(MapRow).ToList();
    }

    public async Task<IReadOnlyList<EnrolledStudentSnapshotDto>> GetEnrolledStudentsAsync(
        Guid operationalPeriodId,
        Guid subjectId,
        Guid studyGroupId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<EnrolledStudentSnapshotDto>(
            new CommandDefinition(
                SelectEnrolledStudentsSql,
                new
                {
                    OperationalPeriodId = operationalPeriodId,
                    SubjectId = subjectId,
                    StudyGroupId = studyGroupId
                },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public Task<Result> UpsertBatchAsync(
        IReadOnlyList<AttendanceRecord> records,
        CancellationToken cancellationToken = default) =>
        SqlPersistenceHelper.ExecuteResultInTransactionAsync(
            connectionFactory,
            async (connection, transaction, ct) =>
            {
                foreach (var record in records)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        UpsertAttendanceRecordSql,
                        new
                        {
                            record.Id,
                            record.StudentId,
                            record.StudentName,
                            record.StudentNis,
                            record.SubjectId,
                            record.SubjectName,
                            record.StudyGroupId,
                            record.StudyGroupName,
                            record.OperationalPeriodId,
                            record.OperationalPeriodName,
                            AttendanceDate = record.AttendanceDate.ToDateTime(TimeOnly.MinValue),
                            Status = record.Status.ToCode(),
                            record.Notes,
                            record.RecordedByTeacherId,
                            record.RecordedByTeacherName,
                            record.CreatedAt,
                            record.UpdatedAt
                        },
                        transaction,
                        cancellationToken: ct));
                }

                return Result.Success();
            },
            ex => MapUniqueConstraint(ex, records),
            cancellationToken);

    private static Result? MapUniqueConstraint(DbException ex, IReadOnlyList<AttendanceRecord> records)
    {
        if (SqlPersistenceHelper.MentionsConstraint(ex, "UQ_attendance_records_student_subject_date"))
        {
            var first = records.FirstOrDefault();
            return Result.Failure(first is not null
                ? AttendanceErrors.DuplicateAttendanceRecord(first.StudentId, first.SubjectId, first.AttendanceDate)
                : DomainError.Conflict("Acd.Attendance.DuplicateRecord", "Duplicate attendance record conflict detected."));
        }

        return null;
    }

    private static AttendanceRecord MapRow(AttendanceRecordRow row) =>
        AttendanceRecord.Rehydrate(
            row.id,
            row.student_id,
            row.student_name,
            row.student_nis,
            row.subject_id,
            row.subject_name,
            row.study_group_id,
            row.study_group_name,
            row.operational_period_id,
            row.operational_period_name,
            DateOnly.FromDateTime(row.attendance_date),
            AttendanceStatusExtensions.ParseAttendanceStatus(row.status).Value,
            row.notes,
            row.recorded_by_teacher_id,
            row.recorded_by_teacher_name,
            SqlPersistenceHelper.SpecifyUtc(row.created_at),
            SqlPersistenceHelper.SpecifyUtc(row.updated_at));

    private sealed class AttendanceRecordRow
    {
        public Guid id { get; set; }
        public Guid student_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string student_nis { get; set; } = string.Empty;
        public Guid subject_id { get; set; }
        public string subject_name { get; set; } = string.Empty;
        public Guid? study_group_id { get; set; }
        public string? study_group_name { get; set; }
        public Guid operational_period_id { get; set; }
        public string operational_period_name { get; set; } = string.Empty;
        public DateTime attendance_date { get; set; }
        public string status { get; set; } = string.Empty;
        public string? notes { get; set; }
        public Guid recorded_by_teacher_id { get; set; }
        public string recorded_by_teacher_name { get; set; } = string.Empty;
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
