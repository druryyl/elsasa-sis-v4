using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Std.Domain;

public static class StdErrors
{
    public static DomainError CandidateNotFound(Guid id) =>
        DomainError.NotFound(
            "Std.Candidate.NotFound",
            $"Candidate '{id}' was not found.");

    public static DomainError StudentNotFound(Guid id) =>
        DomainError.NotFound(
            "Std.Student.NotFound",
            $"Student '{id}' was not found.");

    public static DomainError GuardianNotFound(Guid studentId, Guid guardianId) =>
        DomainError.NotFound(
            "Std.Guardian.NotFound",
            $"Guardian '{guardianId}' was not found for student '{studentId}'.");

    public static DomainError StudyGroupNotFound(Guid id) =>
        DomainError.NotFound(
            "Std.StudyGroup.NotFound",
            $"Study group '{id}' was not found.");

    public static DomainError PlacementNotFound(Guid id) =>
        DomainError.NotFound(
            "Std.Placement.NotFound",
            $"Student placement '{id}' was not found.");

    public static DomainError AcademicYearNotFound(Guid academicYearId) =>
        DomainError.NotFound(
            "Std.AcademicYear.NotFound",
            $"Academic year '{academicYearId}' was not found.");

    public static DomainError HomeroomTeacherNotFound(Guid teacherId) =>
        DomainError.NotFound(
            "Std.StudyGroup.HomeroomTeacherNotFound",
            $"Homeroom teacher '{teacherId}' was not found.");

    public static DomainError CandidateAlreadyEnrolled(Guid candidateId) =>
        DomainError.Conflict(
            "Std.Candidate.AlreadyEnrolled",
            $"Candidate '{candidateId}' has already been enrolled and cannot change admission status.");

    public static DomainError InvalidCandidateStatusTransition(AdmissionStatus current, AdmissionStatus target) =>
        DomainError.Conflict(
            "Std.Candidate.InvalidStatusTransition",
            $"Candidate cannot transition from '{current.ToCode()}' to '{target.ToCode()}'. Only REGISTERED candidates may be accepted or rejected.");

    public static DomainError CandidateNotAcceptedForEnrollment(Guid candidateId, AdmissionStatus current) =>
        DomainError.Conflict(
            "Std.Candidate.NotAcceptedForEnrollment",
            $"Candidate '{candidateId}' cannot be enrolled because admission status is '{current.ToCode()}' instead of 'ACCEPTED'.");

    public static DomainError DuplicateNis(string nis) =>
        DomainError.Conflict(
            "Std.Student.DuplicateNis",
            $"A student with NIS '{nis}' already exists.");

    public static DomainError DuplicateNisn(string nisn) =>
        DomainError.Conflict(
            "Std.Student.DuplicateNisn",
            $"A student with NISN '{nisn}' already exists.");

    public static DomainError CannotRemoveLastGuardian(Guid studentId) =>
        DomainError.Conflict(
            "Std.Student.CannotRemoveLastGuardian",
            $"Student '{studentId}' must have at least one guardian on record.");

    public static DomainError DuplicateStudyGroupName(Guid academicYearId, string name) =>
        DomainError.Conflict(
            "Std.StudyGroup.DuplicateName",
            $"A study group named '{name}' already exists in academic year '{academicYearId}'.");

    public static DomainError HomeroomTeacherInactive(Guid teacherId) =>
        DomainError.Conflict(
            "Std.StudyGroup.HomeroomTeacherInactive",
            $"Teacher '{teacherId}' is inactive and cannot be assigned as homeroom teacher.");

    public static DomainError PlacementAggregateMismatch(Guid placementId) =>
        DomainError.Conflict(
            "Std.Placement.AggregateMismatch",
            $"Existing placement '{placementId}' does not match the specified student and academic year.");

    public static DomainError PlacementAlreadyExistsForAcademicYear(Guid studentId, Guid academicYearId) =>
        DomainError.Conflict(
            "Std.Placement.AlreadyExistsForAcademicYear",
            $"Student '{studentId}' already has a placement for academic year '{academicYearId}'.");

    public static DomainError PlacementStudyGroupMismatch(Guid studyGroupId) =>
        DomainError.Conflict(
            "Std.Placement.StudyGroupMismatch",
            $"Study group '{studyGroupId}' does not match the placement's academic year, school level, or grade.");

    public static DomainError AdmissionYearRequired() =>
        DomainError.Validation(
            "Std.Candidate.AdmissionYearRequired",
            "Admission year is required.",
            "admissionYearId");

    public static DomainError AdmissionYearNameRequired() =>
        DomainError.Validation(
            "Std.Candidate.AdmissionYearNameRequired",
            "Admission year name is required.",
            "admissionYearName");

    public static DomainError FullNameRequired(string field = "fullName") =>
        DomainError.Validation(
            "Std.Person.FullNameRequired",
            "Full name is required.",
            field);

    public static DomainError DateOfBirthInvalid(string field = "dateOfBirth") =>
        DomainError.Validation(
            "Std.Person.DateOfBirthInvalid",
            "Date of birth must be a valid date not in the future.",
            field);

    public static DomainError GenderInvalid(string? value, string field = "gender") =>
        DomainError.Validation(
            "Std.Person.GenderInvalid",
            $"Gender '{value}' is invalid. Allowed values are MALE or FEMALE.",
            field);

    public static DomainError SchoolLevelInvalid(string? value, string field = "schoolLevel") =>
        DomainError.Validation(
            "Std.SchoolLevel.Invalid",
            $"School level '{value}' is invalid. Allowed values are SD, SMP, SMA, or SMK.",
            field);

    public static DomainError GradeRequired(string field = "grade") =>
        DomainError.Validation(
            "Std.Grade.Required",
            "Grade is required.",
            field);

    public static DomainError AtLeastOneGuardianRequired() =>
        DomainError.Validation(
            "Std.Guardian.AtLeastOneRequired",
            "At least one guardian is required.",
            "guardians");

    public static DomainError GuardianFullNameRequired() =>
        DomainError.Validation(
            "Std.Guardian.FullNameRequired",
            "Each guardian must have a full name.",
            "guardians");

    public static DomainError GuardianRelationshipTypeInvalid(string? value, string field = "relationshipType") =>
        DomainError.Validation(
            "Std.Guardian.RelationshipTypeInvalid",
            $"Guardian relationship type '{value}' is invalid. Allowed values are FATHER, MOTHER, or GUARDIAN.",
            field);

    public static DomainError NisRequired() =>
        DomainError.Validation(
            "Std.Student.NisRequired",
            "Student number (NIS) is required.",
            "nis");

    public static DomainError NisTooLong() =>
        DomainError.Validation(
            "Std.Student.NisTooLong",
            "Student number (NIS) must not exceed 30 characters.",
            "nis");

    public static DomainError NisnTooLong() =>
        DomainError.Validation(
            "Std.Student.NisnTooLong",
            "National student number (NISN) must not exceed 20 characters.",
            "nisn");

    public static DomainError EnrollmentDateInvalid() =>
        DomainError.Validation(
            "Std.Student.EnrollmentDateInvalid",
            "Enrollment date is invalid.",
            "enrollmentDate");

    public static DomainError StudentIdRequired() =>
        DomainError.Validation(
            "Std.Student.IdRequired",
            "Student ID is required.",
            "studentId");

    public static DomainError AcademicYearRequired() =>
        DomainError.Validation(
            "Std.AcademicYear.Required",
            "Academic year is required.",
            "academicYearId");

    public static DomainError AcademicYearNameRequired() =>
        DomainError.Validation(
            "Std.AcademicYear.NameRequired",
            "Academic year name is required.",
            "academicYearName");

    public static DomainError StudyGroupNameRequired() =>
        DomainError.Validation(
            "Std.StudyGroup.NameRequired",
            "Study group name is required.",
            "name");

    public static DomainError HomeroomTeacherReferenceIncomplete(string field) =>
        DomainError.Validation(
            "Std.StudyGroup.HomeroomTeacherReferenceIncomplete",
            "Homeroom teacher ID and homeroom teacher name must both be null or both be provided.",
            field);

    public static DomainError StudyGroupReferenceIncomplete(string field) =>
        DomainError.Validation(
            "Std.Placement.StudyGroupReferenceIncomplete",
            "Study group ID and study group name must both be null or both be provided.",
            field);
}
