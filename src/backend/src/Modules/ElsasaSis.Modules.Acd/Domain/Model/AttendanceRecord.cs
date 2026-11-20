using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Acd.Domain.Model;

public sealed class AttendanceRecord : AggregateRoot<Guid>
{
    public Guid StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string StudentNis { get; private set; } = string.Empty;
    public Guid SubjectId { get; private set; }
    public string SubjectName { get; private set; } = string.Empty;
    public Guid? StudyGroupId { get; private set; }
    public string? StudyGroupName { get; private set; }
    public Guid OperationalPeriodId { get; private set; }
    public string OperationalPeriodName { get; private set; } = string.Empty;
    public DateOnly AttendanceDate { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public Guid RecordedByTeacherId { get; private set; }
    public string RecordedByTeacherName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private AttendanceRecord()
    {
    }

    private AttendanceRecord(
        Guid id,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid subjectId,
        string subjectName,
        Guid? studyGroupId,
        string? studyGroupName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        DateOnly attendanceDate,
        AttendanceStatus status,
        string? notes,
        Guid recordedByTeacherId,
        string recordedByTeacherName,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        StudentId = studentId;
        StudentName = studentName;
        StudentNis = studentNis;
        SubjectId = subjectId;
        SubjectName = subjectName;
        StudyGroupId = studyGroupId;
        StudyGroupName = studyGroupName;
        OperationalPeriodId = operationalPeriodId;
        OperationalPeriodName = operationalPeriodName;
        AttendanceDate = attendanceDate;
        Status = status;
        Notes = notes;
        RecordedByTeacherId = recordedByTeacherId;
        RecordedByTeacherName = recordedByTeacherName;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<AttendanceRecord> Create(
        Guid studentId,
        string studentName,
        string studentNis,
        Guid subjectId,
        string subjectName,
        Guid? studyGroupId,
        string? studyGroupName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        DateOnly attendanceDate,
        AttendanceStatus status,
        string? notes,
        Guid recordedByTeacherId,
        string recordedByTeacherName,
        DateTime? utcNow = null)
    {
        if (studentId == Guid.Empty)
        {
            return Result<AttendanceRecord>.Failure(AttendanceErrors.StudentIdRequired());
        }

        if (string.IsNullOrWhiteSpace(studentName))
        {
            return Result<AttendanceRecord>.Failure(AttendanceErrors.SnapshotRequired("studentName", "Student name"));
        }

        if (string.IsNullOrWhiteSpace(studentNis))
        {
            return Result<AttendanceRecord>.Failure(AttendanceErrors.SnapshotRequired("studentNis", "Student NIS"));
        }

        if (subjectId == Guid.Empty)
        {
            return Result<AttendanceRecord>.Failure(AttendanceErrors.SubjectIdRequired());
        }

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            return Result<AttendanceRecord>.Failure(AttendanceErrors.SnapshotRequired("subjectName", "Subject name"));
        }

        if (operationalPeriodId == Guid.Empty)
        {
            return Result<AttendanceRecord>.Failure(AttendanceErrors.OperationalPeriodIdRequired());
        }

        if (string.IsNullOrWhiteSpace(operationalPeriodName))
        {
            return Result<AttendanceRecord>.Failure(AttendanceErrors.SnapshotRequired("operationalPeriodName", "Operational period name"));
        }

        if (attendanceDate == default)
        {
            return Result<AttendanceRecord>.Failure(AttendanceErrors.AttendanceDateRequired());
        }

        if (!Enum.IsDefined(status))
        {
            return Result<AttendanceRecord>.Failure(AttendanceErrors.InvalidStatus(status.ToString()));
        }

        if (notes is { Length: > 500 })
        {
            return Result<AttendanceRecord>.Failure(AttendanceErrors.NotesTooLong(500));
        }

        if (recordedByTeacherId == Guid.Empty)
        {
            return Result<AttendanceRecord>.Failure(AttendanceErrors.RecordedByTeacherIdRequired());
        }

        if (string.IsNullOrWhiteSpace(recordedByTeacherName))
        {
            return Result<AttendanceRecord>.Failure(AttendanceErrors.SnapshotRequired("recordedByTeacherName", "Teacher name"));
        }

        var now = utcNow ?? DateTime.UtcNow;
        var record = new AttendanceRecord(
            UuidV7.NewGuid(),
            studentId,
            studentName.Trim(),
            studentNis.Trim(),
            subjectId,
            subjectName.Trim(),
            studyGroupId,
            string.IsNullOrWhiteSpace(studyGroupName) ? null : studyGroupName.Trim(),
            operationalPeriodId,
            operationalPeriodName.Trim(),
            attendanceDate,
            status,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            recordedByTeacherId,
            recordedByTeacherName.Trim(),
            now,
            now);

        return Result<AttendanceRecord>.Success(record);
    }

    public Result UpdateStatus(
        AttendanceStatus newStatus,
        string? newNotes,
        Guid recordedByTeacherId,
        string recordedByTeacherName,
        DateTime? utcNow = null)
    {
        if (!Enum.IsDefined(newStatus))
        {
            return Result.Failure(AttendanceErrors.InvalidStatus(newStatus.ToString()));
        }

        if (newNotes is { Length: > 500 })
        {
            return Result.Failure(AttendanceErrors.NotesTooLong(500));
        }

        if (recordedByTeacherId == Guid.Empty)
        {
            return Result.Failure(AttendanceErrors.RecordedByTeacherIdRequired());
        }

        if (string.IsNullOrWhiteSpace(recordedByTeacherName))
        {
            return Result.Failure(AttendanceErrors.SnapshotRequired("recordedByTeacherName", "Teacher name"));
        }

        Status = newStatus;
        Notes = string.IsNullOrWhiteSpace(newNotes) ? null : newNotes.Trim();
        RecordedByTeacherId = recordedByTeacherId;
        RecordedByTeacherName = recordedByTeacherName.Trim();
        UpdatedAt = utcNow ?? DateTime.UtcNow;

        return Result.Success();
    }

    public static AttendanceRecord Rehydrate(
        Guid id,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid subjectId,
        string subjectName,
        Guid? studyGroupId,
        string? studyGroupName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        DateOnly attendanceDate,
        AttendanceStatus status,
        string? notes,
        Guid recordedByTeacherId,
        string recordedByTeacherName,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(
            id,
            studentId,
            studentName,
            studentNis,
            subjectId,
            subjectName,
            studyGroupId,
            studyGroupName,
            operationalPeriodId,
            operationalPeriodName,
            attendanceDate,
            status,
            notes,
            recordedByTeacherId,
            recordedByTeacherName,
            createdAt,
            updatedAt);
}
