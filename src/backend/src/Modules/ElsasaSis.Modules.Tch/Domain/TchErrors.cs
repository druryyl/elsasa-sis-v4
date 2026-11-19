using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Tch.Domain;

public static class TchErrors
{
    public static DomainError TeacherNotFound(Guid id) =>
        DomainError.NotFound(
            "Tch.Teacher.NotFound",
            $"Teacher '{id}' was not found.");

    public static DomainError FullNameRequired() =>
        DomainError.Validation(
            "Tch.Teacher.FullNameRequired",
            "Teacher full name is required.",
            "fullName");

    public static DomainError DateOfBirthRequired() =>
        DomainError.Validation(
            "Tch.Teacher.DateOfBirthRequired",
            "Teacher date of birth is required.",
            "dateOfBirth");

    public static DomainError DateOfBirthInFuture() =>
        DomainError.Validation(
            "Tch.Teacher.DateOfBirthInFuture",
            "Teacher date of birth cannot be in the future.",
            "dateOfBirth");

    public static DomainError InvalidGender(string? raw = null) =>
        DomainError.Validation(
            "Tch.Teacher.InvalidGender",
            string.IsNullOrWhiteSpace(raw)
                ? "Teacher gender must be MALE or FEMALE."
                : $"'{raw}' is not a valid gender. Allowed values are MALE or FEMALE.",
            "gender");

    public static DomainError ContactMethodRequired()
    {
        const string message = "At least one contact method (phone or email) is required.";
        return DomainError.Validation(
            "Tch.Teacher.ContactMethodRequired",
            message,
            new Dictionary<string, string[]>
            {
                ["phone"] = [message],
                ["email"] = [message]
            });
    }

    public static DomainError DuplicateNip(string nip) =>
        DomainError.Conflict(
            "Tch.Teacher.DuplicateNip",
            $"A teacher with NIP '{nip}' already exists.");

    public static DomainError DuplicateNik(string nik) =>
        DomainError.Conflict(
            "Tch.Teacher.DuplicateNik",
            $"A teacher with NIK '{nik}' already exists.");

    public static DomainError SubjectIdRequired() =>
        DomainError.Validation(
            "Tch.Capability.SubjectIdRequired",
            "Subject identifier is required.",
            "subjectId");

    public static DomainError SubjectNameRequired() =>
        DomainError.Validation(
            "Tch.Capability.SubjectNameRequired",
            "Subject name is required.",
            "subjectName");

    public static DomainError DuplicateCapability(Guid teacherId, Guid subjectId) =>
        DomainError.Conflict(
            "Tch.Capability.Duplicate",
            $"Teacher '{teacherId}' already has teaching capability for subject '{subjectId}'.");

    public static DomainError CapabilityNotFound(Guid teacherId, Guid subjectId) =>
        DomainError.NotFound(
            "Tch.Capability.NotFound",
            $"Teaching capability for subject '{subjectId}' was not found for teacher '{teacherId}'.");

    public static DomainError AssignmentNotFound(Guid id) =>
        DomainError.NotFound(
            "Tch.Assignment.NotFound",
            $"Teacher assignment '{id}' was not found.");

    public static DomainError TeacherInactiveForAssignment(Guid teacherId) =>
        DomainError.Conflict(
            "Tch.Assignment.TeacherInactive",
            $"Teacher '{teacherId}' is inactive and cannot be assigned.");

    public static DomainError TeacherLacksCapability(Guid teacherId, Guid subjectId) =>
        DomainError.Conflict(
            "Tch.Assignment.MissingCapability",
            $"Teacher '{teacherId}' does not have a recorded teaching capability for subject '{subjectId}'.");

    public static DomainError DuplicateAssignment(
        Guid teacherId,
        Guid subjectId,
        Guid studyGroupId,
        Guid operationalPeriodId) =>
        DomainError.Conflict(
            "Tch.Assignment.Duplicate",
            $"Teacher '{teacherId}' is already assigned to subject '{subjectId}' in study group '{studyGroupId}' for operational period '{operationalPeriodId}'.");

    public static DomainError AssignmentAlreadyRemoved(Guid assignmentId) =>
        DomainError.Conflict(
            "Tch.Assignment.AlreadyRemoved",
            $"Teacher assignment '{assignmentId}' has already been removed.");

    public static DomainError AssignmentTeacherMismatch(Guid assignmentId, Guid teacherId) =>
        DomainError.Conflict(
            "Tch.Assignment.TeacherMismatch",
            $"Teacher '{teacherId}' does not match assignment '{assignmentId}'.");

    public static DomainError ReferenceIdRequired(string field, string displayName) =>
        DomainError.Validation(
            "Tch.Assignment.ReferenceIdRequired",
            $"{displayName} identifier is required.",
            field);

    public static DomainError SnapshotNameRequired(string field, string displayName) =>
        DomainError.Validation(
            "Tch.Assignment.SnapshotNameRequired",
            $"{displayName} name is required.",
            field);

    public static DomainError SubjectNotFound(Guid subjectId) =>
        DomainError.NotFound(
            "Tch.Subject.NotFound",
            $"Subject '{subjectId}' was not found.");

    public static DomainError OperationalPeriodNotFound(Guid operationalPeriodId) =>
        DomainError.NotFound(
            "Tch.OperationalPeriod.NotFound",
            $"Operational period '{operationalPeriodId}' was not found.");

    public static DomainError StudyGroupNotFound(Guid studyGroupId) =>
        DomainError.NotFound(
            "Tch.StudyGroup.NotFound",
            $"Study group '{studyGroupId}' was not found.");

    public static DomainError StudyGroupAcademicYearMismatch(Guid studyGroupId, Guid academicYearId) =>
        DomainError.Conflict(
            "Tch.StudyGroup.AcademicYearMismatch",
            $"Study group '{studyGroupId}' does not belong to academic year '{academicYearId}'.");
}
