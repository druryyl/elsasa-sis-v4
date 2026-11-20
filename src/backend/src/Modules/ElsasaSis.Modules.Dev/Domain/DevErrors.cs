using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Dev.Domain;

public static class DevErrors
{
    public static DomainError StudentNotFound(Guid studentId) =>
        DomainError.NotFound(
            "Dev.StudentNotFound",
            $"Student with ID '{studentId}' was not found.");

    public static DomainError StudentRequired() =>
        DomainError.Validation(
            "Dev.StudentRequired",
            "Student identifier is required.");

    public static DomainError StudentNameRequired() =>
        DomainError.Validation(
            "Dev.StudentNameRequired",
            "Student name is required.");

    public static DomainError StudentNisRequired() =>
        DomainError.Validation(
            "Dev.StudentNisRequired",
            "Student NIS is required.");

    public static DomainError AcademicYearNotFound(Guid academicYearId) =>
        DomainError.NotFound(
            "Dev.AcademicYearNotFound",
            $"Academic year '{academicYearId}' was not found.");

    public static DomainError NoActiveAcademicYear() =>
        DomainError.Validation(
            "Dev.NoActiveAcademicYear",
            "No active academic year found.");

    public static DomainError BehaviourNoteNotFound(Guid id) =>
        DomainError.NotFound(
            "Dev.BehaviourNote.NotFound",
            $"Behaviour note '{id}' was not found.");

    public static DomainError InvalidBehaviourType(string? behaviourType) =>
        DomainError.Validation(
            "Dev.BehaviourNote.InvalidType",
            $"Behaviour type '{behaviourType}' is invalid. Allowed values: POSITIVE, NEGATIVE.");

    public static DomainError EmptyDescription() =>
        DomainError.Validation(
            "Dev.DescriptionRequired",
            "Description is required and cannot be empty.");

    public static DomainError DescriptionTooLong(int maxLength = 1000) =>
        DomainError.Validation(
            "Dev.DescriptionTooLong",
            $"Description cannot exceed {maxLength} characters.");

    public static DomainError RecorderRequired() =>
        DomainError.Validation(
            "Dev.RecorderRequired",
            "Recorder identifier and name are required.");

    public static DomainError DisciplineRecordNotFound(Guid id) =>
        DomainError.NotFound(
            "Dev.DisciplineRecord.NotFound",
            $"Discipline record '{id}' was not found.");

    public static DomainError EmptyViolationType() =>
        DomainError.Validation(
            "Dev.DisciplineRecord.ViolationTypeRequired",
            "Violation type is required.");

    public static DomainError ViolationTypeTooLong(int maxLength = 50) =>
        DomainError.Validation(
            "Dev.DisciplineRecord.ViolationTypeTooLong",
            $"Violation type cannot exceed {maxLength} characters.");

    public static DomainError InvalidSanction(string? sanction) =>
        DomainError.Validation(
            "Dev.DisciplineRecord.InvalidSanction",
            $"Sanction '{sanction}' is invalid. Allowed values: VERBAL_WARNING, WRITTEN_WARNING, PARENT_MEETING, SUSPENSION.");

    public static DomainError SanctionNotAllowedForRole(string sanction, string role) =>
        DomainError.Forbidden(
            "Dev.DisciplineRecord.SanctionNotAllowedForRole",
            $"Sanction '{sanction}' is not permitted for role '{role}'.");

    public static DomainError ExtracurricularNotFound(Guid id) =>
        DomainError.NotFound(
            "Dev.Extracurricular.NotFound",
            $"Extracurricular activity '{id}' was not found.");

    public static DomainError ActivityNameRequired() =>
        DomainError.Validation(
            "Dev.Extracurricular.NameRequired",
            "Activity name is required.");

    public static DomainError ActivityNameTooLong(int maxLength = 100) =>
        DomainError.Validation(
            "Dev.Extracurricular.NameTooLong",
            $"Activity name cannot exceed {maxLength} characters.");

    public static DomainError DuplicateActivityName(string name) =>
        DomainError.Conflict(
            "Dev.Extracurricular.DuplicateName",
            $"An extracurricular activity with name '{name}' already exists.");

    public static DomainError SupervisorRequired() =>
        DomainError.Validation(
            "Dev.Extracurricular.SupervisorRequired",
            "Supervisor identifier and name are required.");

    public static DomainError SupervisorNotFound(Guid supervisorId) =>
        DomainError.NotFound(
            "Dev.Extracurricular.SupervisorNotFound",
            $"Supervisor with ID '{supervisorId}' was not found.");

    public static DomainError ActivityNotActive(Guid activityId) =>
        DomainError.Conflict(
            "Dev.Extracurricular.NotActive",
            $"Extracurricular activity '{activityId}' is not active.");

    public static DomainError StudentAlreadyEnrolledInActivity(Guid studentId, Guid activityId) =>
        DomainError.Conflict(
            "Dev.Extracurricular.AlreadyEnrolled",
            $"Student '{studentId}' is already actively enrolled in activity '{activityId}' for this academic year.");

    public static DomainError ParticipationNotFound(Guid participationId) =>
        DomainError.NotFound(
            "Dev.Extracurricular.ParticipationNotFound",
            $"Participation '{participationId}' was not found.");

    public static DomainError ParticipationAlreadyWithdrawn(Guid participationId) =>
        DomainError.Conflict(
            "Dev.Extracurricular.AlreadyWithdrawn",
            $"Participation '{participationId}' has already been withdrawn.");

    public static DomainError ParticipationNotActive(Guid participationId) =>
        DomainError.Conflict(
            "Dev.Extracurricular.ParticipationNotActive",
            $"Participation '{participationId}' is not in ACTIVE status.");

    public static DomainError CallerNotAssignedSupervisor(Guid callerId, Guid activityId) =>
        DomainError.Forbidden(
            "Dev.Extracurricular.NotAssignedSupervisor",
            $"User '{callerId}' is not the assigned supervisor for activity '{activityId}'.");

    public static DomainError EmptySessionLog() =>
        DomainError.Validation(
            "Dev.Extracurricular.EmptySessionLog",
            "Session log content cannot be empty.");

    public static DomainError SessionLogTooLong(int maxLength = 1000) =>
        DomainError.Validation(
            "Dev.Extracurricular.SessionLogTooLong",
            $"Session log cannot exceed {maxLength} characters.");

    public static DomainError AchievementNotFound(Guid id) =>
        DomainError.NotFound(
            "Dev.Achievement.NotFound",
            $"Achievement '{id}' was not found.");

    public static DomainError EmptyEventName() =>
        DomainError.Validation(
            "Dev.Achievement.EventNameRequired",
            "Event name is required.");

    public static DomainError EventNameTooLong(int maxLength = 200) =>
        DomainError.Validation(
            "Dev.Achievement.EventNameTooLong",
            $"Event name cannot exceed {maxLength} characters.");

    public static DomainError InvalidAchievementType(string? achievementType) =>
        DomainError.Validation(
            "Dev.Achievement.InvalidType",
            $"Achievement type '{achievementType}' is invalid. Allowed values: SPORTS, ARTS, SCIENCE, COMMUNITY, OTHER.");

    public static DomainError InvalidRecognitionLevel(string? recognitionLevel) =>
        DomainError.Validation(
            "Dev.Achievement.InvalidLevel",
            $"Recognition level '{recognitionLevel}' is invalid. Allowed values: SCHOOL, CITY, PROVINCE, NATIONAL.");
}
