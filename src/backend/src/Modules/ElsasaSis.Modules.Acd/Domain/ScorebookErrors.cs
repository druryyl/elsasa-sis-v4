using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Domain;

public static class ScorebookErrors
{
    public static DomainError TitleRequired() =>
        DomainError.Validation(
            "Acd.Scorebook.TitleRequired",
            "Assignment title is required.",
            "title");

    public static DomainError TitleTooLong(int max = 150) =>
        DomainError.Validation(
            "Acd.Scorebook.TitleTooLong",
            $"Assignment title cannot exceed {max} characters.",
            "title");

    public static DomainError InvalidAssignmentType(string? raw, string field = "assignmentType") =>
        DomainError.Validation(
            "Acd.Scorebook.InvalidAssignmentType",
            string.IsNullOrWhiteSpace(raw)
                ? "Assignment type must be one of: FORMATIVE, SUMMATIVE, EXAM."
                : $"'{raw}' is not a valid assignment type. Allowed values are FORMATIVE, SUMMATIVE, EXAM.",
            field);

    public static DomainError MaxScoreMustBePositive() =>
        DomainError.Validation(
            "Acd.Scorebook.MaxScoreMustBePositive",
            "Maximum score must be greater than zero.",
            "maxScore");

    public static DomainError InvalidWeight(decimal? weight) =>
        DomainError.Validation(
            "Acd.Scorebook.InvalidWeight",
            $"Assessment weight must be between 0.00 and 100.00. Value provided: {weight}.",
            "weight");

    public static DomainError ScoreOutOfRange(decimal score, decimal maxScore, string? studentId = null) =>
        DomainError.Validation(
            "Acd.Scorebook.ScoreOutOfRange",
            studentId is not null
                ? $"Score {score} for student '{studentId}' is out of range. Must be between 0.00 and {maxScore}."
                : $"Score {score} is out of range. Must be between 0.00 and {maxScore}.",
            "score");

    public static DomainError SubjectStageIdRequired() =>
        DomainError.Validation(
            "Acd.Scorebook.SubjectStageIdRequired",
            "Subject stage identifier is required.",
            "subjectStageId");

    public static DomainError SubjectIdRequired() =>
        DomainError.Validation(
            "Acd.Scorebook.SubjectIdRequired",
            "Subject identifier is required.",
            "subjectId");

    public static DomainError StudyGroupIdRequired() =>
        DomainError.Validation(
            "Acd.Scorebook.StudyGroupIdRequired",
            "Study group identifier is required.",
            "studyGroupId");

    public static DomainError OperationalPeriodIdRequired() =>
        DomainError.Validation(
            "Acd.Scorebook.OperationalPeriodIdRequired",
            "Operational period identifier is required.",
            "operationalPeriodId");

    public static DomainError TeacherIdRequired() =>
        DomainError.Validation(
            "Acd.Scorebook.TeacherIdRequired",
            "Teacher identifier is required.",
            "teacherId");

    public static DomainError SnapshotRequired(string field, string name) =>
        DomainError.Validation(
            $"Acd.Scorebook.{field}Required",
            $"{name} is required.",
            field);

    public static DomainError DueDateBeforeAssignedDate() =>
        DomainError.Validation(
            "Acd.Scorebook.DueDateBeforeAssignedDate",
            "Due date cannot be earlier than assigned date.",
            "dueDate");

    public static DomainError FeedbackTooLong(int max = 1000) =>
        DomainError.Validation(
            "Acd.Scorebook.FeedbackTooLong",
            $"Feedback cannot exceed {max} characters.",
            "feedback");

    public static DomainError ItemsRequired() =>
        DomainError.Validation(
            "Acd.Scorebook.ItemsRequired",
            "At least one score entry item is required.",
            "items");

    public static DomainError DuplicateStudentInBatch(Guid studentId) =>
        DomainError.Validation(
            "Acd.Scorebook.DuplicateStudentInBatch",
            $"Student '{studentId}' appears more than once in the score entry batch.",
            "items");

    public static DomainError AssignmentNotFound(Guid id) =>
        DomainError.NotFound(
            "Acd.Scorebook.AssignmentNotFound",
            $"Assignment '{id}' was not found.");

    public static DomainError TeacherNotAuthorized(Guid teacherId, Guid studyGroupId, Guid subjectId) =>
        DomainError.Forbidden(
            "Acd.Scorebook.TeacherNotAuthorized",
            $"Teacher '{teacherId}' does not have an active teaching assignment for study group '{studyGroupId}' and subject '{subjectId}'.");

    public static DomainError NoActiveTeacherAssigned(Guid studyGroupId, Guid subjectId) =>
        DomainError.Conflict(
            "Acd.Scorebook.NoActiveTeacherAssigned",
            $"No active teacher is currently assigned to study group '{studyGroupId}' and subject '{subjectId}'.");

    public static DomainError StudentNotActivelyEnrolled(Guid studentId) =>
        DomainError.Conflict(
            "Acd.Scorebook.StudentNotActivelyEnrolled",
            $"Student '{studentId}' does not have an active academic participation in this subject stage.");

    public static DomainError OperationalPeriodNotFound(Guid periodId) =>
        DomainError.NotFound(
            "Acd.Scorebook.OperationalPeriodNotFound",
            $"Operational period '{periodId}' was not found.");

    public static DomainError SubjectNotFound(Guid subjectId) =>
        DomainError.NotFound(
            "Acd.Scorebook.SubjectNotFound",
            $"Subject '{subjectId}' was not found.");

    public static DomainError SubjectStageNotFound(Guid stageId) =>
        DomainError.NotFound(
            "Acd.Scorebook.SubjectStageNotFound",
            $"Subject stage '{stageId}' was not found.");

    public static DomainError StudyGroupNotFound(Guid studyGroupId) =>
        DomainError.NotFound(
            "Acd.Scorebook.StudyGroupNotFound",
            $"Study group '{studyGroupId}' was not found.");

    public static DomainError LearningUnitNotFound(Guid unitId) =>
        DomainError.NotFound(
            "Acd.Scorebook.LearningUnitNotFound",
            $"Learning unit '{unitId}' was not found.");
}
