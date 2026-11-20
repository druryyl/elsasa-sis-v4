using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Domain;

public static class ParticipationErrors
{
    public static DomainError StudentIdRequired() =>
        DomainError.Validation(
            "Acd.Participation.StudentIdRequired",
            "Student identifier is required.",
            "studentId");

    public static DomainError SubjectStageIdRequired() =>
        DomainError.Validation(
            "Acd.Participation.SubjectStageIdRequired",
            "Subject stage identifier is required.",
            "subjectStageId");

    public static DomainError SubjectIdRequired() =>
        DomainError.Validation(
            "Acd.Participation.SubjectIdRequired",
            "Subject identifier is required.",
            "subjectId");

    public static DomainError OperationalPeriodIdRequired() =>
        DomainError.Validation(
            "Acd.Participation.OperationalPeriodIdRequired",
            "Operational period identifier is required.",
            "operationalPeriodId");

    public static DomainError AcademicYearIdRequired() =>
        DomainError.Validation(
            "Acd.Participation.AcademicYearIdRequired",
            "Academic year identifier is required.",
            "academicYearId");

    public static DomainError StudyGroupIdRequired() =>
        DomainError.Validation(
            "Acd.Participation.StudyGroupIdRequired",
            "Study group identifier is required.",
            "studyGroupId");

    public static DomainError SnapshotRequired(string field, string name) =>
        DomainError.Validation(
            $"Acd.Participation.{field}Required",
            $"{name} is required.",
            field);

    public static DomainError InvalidStatus(string? raw, string field = "status") =>
        DomainError.Validation(
            "Acd.Participation.InvalidStatus",
            string.IsNullOrWhiteSpace(raw)
                ? "Academic participation status must be one of: ACTIVE, COMPLETED."
                : $"'{raw}' is not a valid academic participation status. Allowed values are ACTIVE, COMPLETED.",
            field);

    public static DomainError ParticipationNotFound(Guid id) =>
        DomainError.NotFound("Acd.Participation.NotFound", $"Academic participation '{id}' was not found.");

    public static DomainError SubjectStageNotFound(Guid id) =>
        DomainError.NotFound("Acd.Participation.SubjectStageNotFound", $"Subject stage '{id}' was not found.");

    public static DomainError OperationalPeriodNotFound(Guid id) =>
        DomainError.NotFound("Acd.Participation.OperationalPeriodNotFound", $"Operational period '{id}' was not found.");

    public static DomainError OperationalPeriodNotActive(Guid id) =>
        DomainError.Conflict("Acd.Participation.OperationalPeriodNotActive", $"Operational period '{id}' is not currently active.");

    public static DomainError StudyGroupNotFound(Guid id) =>
        DomainError.NotFound("Acd.Participation.StudyGroupNotFound", $"Study group '{id}' was not found.");

    public static DomainError DuplicateParticipation(Guid studentId, Guid subjectStageId, Guid operationalPeriodId) =>
        DomainError.Conflict("Acd.Participation.Duplicate", $"Student '{studentId}' is already enrolled in subject stage '{subjectStageId}' for operational period '{operationalPeriodId}'.");
}
