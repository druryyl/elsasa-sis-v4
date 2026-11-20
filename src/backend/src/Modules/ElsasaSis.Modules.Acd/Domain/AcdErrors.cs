using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Domain;

public static class AcdErrors
{
    public static class ReportCards
    {
        public static DomainError ReportCardNotFound(Guid? id = null) =>
            DomainError.NotFound(
                "Acd.ReportCards.NotFound",
                id.HasValue
                    ? $"Report card with ID '{id.Value}' was not found."
                    : "Report card was not found.");

        public static DomainError AlreadyFinalized(Guid? id = null) =>
            DomainError.Conflict(
                "Acd.ReportCards.AlreadyFinalized",
                id.HasValue
                    ? $"Report card '{id.Value}' is already finalized and cannot be modified."
                    : "Report card is already finalized and cannot be modified.");

        public static DomainError NotFinalized(Guid? id = null) =>
            DomainError.Conflict(
                "Acd.ReportCards.NotFinalized",
                id.HasValue
                    ? $"Report card '{id.Value}' is not finalized."
                    : "Report card is not finalized.");

        public static DomainError EmptyOverrideReason() =>
            DomainError.Validation(
                "Acd.ReportCards.EmptyOverrideReason",
                "Override reason is required when unlocking a finalized report card.",
                "reason");

        public static DomainError TeacherNotAssignedToSubject(Guid teacherId, Guid subjectStageId) =>
            DomainError.Conflict(
                "Acd.ReportCards.TeacherNotAssignedToSubject",
                $"Teacher '{teacherId}' is not actively assigned to subject stage '{subjectStageId}'.");

        public static DomainError EmptyTeacherNotes() =>
            DomainError.Validation(
                "Acd.ReportCards.EmptyTeacherNotes",
                "Teacher notes cannot be empty.",
                "notes");

        public static DomainError EntryNotFound(Guid? entryId = null) =>
            DomainError.NotFound(
                "Acd.ReportCards.EntryNotFound",
                entryId.HasValue
                    ? $"Report card entry with ID '{entryId.Value}' was not found."
                    : "Report card entry was not found.");

        public static DomainError StudentIdRequired() =>
            DomainError.Validation(
                "Acd.ReportCards.StudentIdRequired",
                "Student identifier is required.",
                "studentId");

        public static DomainError AcademicYearRequired() =>
            DomainError.Validation(
                "Acd.ReportCards.AcademicYearRequired",
                "Academic year identifier and name are required.",
                "academicYearId");

        public static DomainError OperationalPeriodRequired() =>
            DomainError.Validation(
                "Acd.ReportCards.OperationalPeriodRequired",
                "Operational period identifier and name are required.",
                "operationalPeriodId");

        public static DomainError SubjectStageRequired() =>
            DomainError.Validation(
                "Acd.ReportCards.SubjectStageRequired",
                "Subject stage identifier and name are required.",
                "subjectStageId");

        public static DomainError StudyGroupNotFound(Guid? id = null) =>
            DomainError.NotFound(
                "Acd.ReportCards.StudyGroupNotFound",
                id.HasValue
                    ? $"Study group with ID '{id.Value}' was not found."
                    : "Study group was not found.");

        public static DomainError OperationalPeriodNotFound(Guid? id = null) =>
            DomainError.NotFound(
                "Acd.ReportCards.OperationalPeriodNotFound",
                id.HasValue
                    ? $"Operational period with ID '{id.Value}' was not found."
                    : "Operational period was not found.");

        public static DomainError NotAssignedHomeroomTeacher(Guid teacherId, Guid studyGroupId) =>
            DomainError.Conflict(
                "Acd.ReportCards.NotAssignedHomeroomTeacher",
                $"Teacher '{teacherId}' is not the assigned homeroom teacher for study group '{studyGroupId}'.");
    }
}
