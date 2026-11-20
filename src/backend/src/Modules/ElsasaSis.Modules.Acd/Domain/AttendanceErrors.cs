using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Domain;

public static class AttendanceErrors
{
    public static DomainError StudentIdRequired() =>
        DomainError.Validation(
            "Acd.Attendance.StudentIdRequired",
            "Student identifier is required.",
            "studentId");

    public static DomainError SubjectIdRequired() =>
        DomainError.Validation(
            "Acd.Attendance.SubjectIdRequired",
            "Subject identifier is required.",
            "subjectId");

    public static DomainError StudyGroupIdRequired() =>
        DomainError.Validation(
            "Acd.Attendance.StudyGroupIdRequired",
            "Study group identifier is required.",
            "studyGroupId");

    public static DomainError OperationalPeriodIdRequired() =>
        DomainError.Validation(
            "Acd.Attendance.OperationalPeriodIdRequired",
            "Operational period identifier is required.",
            "operationalPeriodId");

    public static DomainError AttendanceDateRequired() =>
        DomainError.Validation(
            "Acd.Attendance.AttendanceDateRequired",
            "Attendance date is required.",
            "attendanceDate");

    public static DomainError RecordedByTeacherIdRequired() =>
        DomainError.Validation(
            "Acd.Attendance.RecordedByTeacherIdRequired",
            "Recorded by teacher identifier is required.",
            "recordedByTeacherId");

    public static DomainError SnapshotRequired(string field, string name) =>
        DomainError.Validation(
            $"Acd.Attendance.{field}Required",
            $"{name} is required.",
            field);

    public static DomainError InvalidStatus(string? raw, string field = "status") =>
        DomainError.Validation(
            "Acd.Attendance.InvalidStatus",
            string.IsNullOrWhiteSpace(raw)
                ? "Attendance status must be one of: PRESENT, ABSENT, EXCUSED, SICK, LATE."
                : $"'{raw}' is not a valid attendance status. Allowed values are PRESENT, ABSENT, EXCUSED, SICK, LATE.",
            field);

    public static DomainError NotesTooLong(int max = 500) =>
        DomainError.Validation(
            "Acd.Attendance.NotesTooLong",
            $"Attendance notes cannot exceed {max} characters.",
            "notes");

    public static DomainError ItemsRequired() =>
        DomainError.Validation(
            "Acd.Attendance.ItemsRequired",
            "At least one attendance item must be provided.",
            "items");

    public static DomainError DuplicateStudentInBatch(Guid studentId) =>
        DomainError.Validation(
            "Acd.Attendance.DuplicateStudentInBatch",
            $"Student '{studentId}' is included more than once in the attendance request.",
            "items");

    public static DomainError StudentNotEnrolled(Guid studentId) =>
        DomainError.Conflict(
            "Acd.Attendance.StudentNotEnrolled",
            $"Student '{studentId}' is not actively enrolled in this subject and study group.");

    public static DomainError OperationalPeriodNotFound(Guid periodId) =>
        DomainError.NotFound(
            "Acd.Attendance.OperationalPeriodNotFound",
            $"Operational period '{periodId}' was not found.");

    public static DomainError SubjectNotFound(Guid subjectId) =>
        DomainError.NotFound(
            "Acd.Attendance.SubjectNotFound",
            $"Subject '{subjectId}' was not found.");

    public static DomainError StudyGroupNotFound(Guid studyGroupId) =>
        DomainError.NotFound(
            "Acd.Attendance.StudyGroupNotFound",
            $"Study group '{studyGroupId}' was not found.");

    public static DomainError DuplicateAttendanceRecord(Guid studentId, Guid subjectId, DateOnly attendanceDate) =>
        DomainError.Conflict(
            "Acd.Attendance.DuplicateRecord",
            $"An attendance record already exists for student '{studentId}', subject '{subjectId}' on {attendanceDate:yyyy-MM-dd}.");
}
