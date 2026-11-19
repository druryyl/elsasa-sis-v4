using ElsasaSis.Modules.Std.Domain;

namespace ElsasaSis.Modules.Std.Features.Admissions;

public sealed record CandidateGuardianDto(
    Guid Id,
    Guid CandidateId,
    string FullName,
    string RelationshipType,
    string? Phone,
    string? Email,
    string? Address,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CandidateSummaryDto(
    Guid Id,
    Guid AdmissionYearId,
    string AdmissionYearName,
    string FullName,
    string? Nickname,
    DateOnly DateOfBirth,
    string Gender,
    string? Phone,
    string? Email,
    string? Address,
    string TargetSchoolLevel,
    string TargetGrade,
    string AdmissionStatus,
    DateTime RegisteredAt,
    DateTime? DecidedAt,
    string? DecisionNotes,
    Guid? EnrolledStudentId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CandidateDto(
    Guid Id,
    Guid AdmissionYearId,
    string AdmissionYearName,
    string FullName,
    string? Nickname,
    DateOnly DateOfBirth,
    string Gender,
    string? Phone,
    string? Email,
    string? Address,
    string TargetSchoolLevel,
    string TargetGrade,
    string AdmissionStatus,
    DateTime RegisteredAt,
    DateTime? DecidedAt,
    string? DecisionNotes,
    Guid? EnrolledStudentId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<CandidateGuardianDto> Guardians);

public sealed record NextNisResponse(string SuggestedNis);

public sealed record EnrolledStudentGuardianDto(
    Guid Id,
    Guid StudentId,
    string FullName,
    string RelationshipType,
    string? Phone,
    string? Email,
    string? Address,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record EnrolledStudentResponse(
    Guid Id,
    Guid? CandidateId,
    string Nis,
    string? Nisn,
    string FullName,
    string? Nickname,
    DateOnly DateOfBirth,
    string Gender,
    string? Phone,
    string? Email,
    string? Address,
    DateOnly EnrollmentDate,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<EnrolledStudentGuardianDto> Guardians);

internal static class AdmissionsMappings
{
    public static CandidateGuardianDto ToDto(this CandidateGuardian guardian) =>
        new(
            guardian.Id,
            guardian.CandidateId,
            guardian.FullName,
            guardian.RelationshipType.ToCode(),
            guardian.Phone,
            guardian.Email,
            guardian.Address,
            AdmissionsQueryHelper.SpecifyUtc(guardian.CreatedAt),
            AdmissionsQueryHelper.SpecifyUtc(guardian.UpdatedAt));

    public static CandidateDto ToDto(this Candidate candidate) =>
        new(
            candidate.Id,
            candidate.AdmissionYearId,
            candidate.AdmissionYearName,
            candidate.FullName,
            candidate.Nickname,
            candidate.DateOfBirth,
            candidate.Gender.ToCode(),
            candidate.Phone,
            candidate.Email,
            candidate.Address,
            candidate.TargetSchoolLevel.ToCode(),
            candidate.TargetGrade,
            candidate.AdmissionStatus.ToCode(),
            AdmissionsQueryHelper.SpecifyUtc(candidate.RegisteredAt),
            AdmissionsQueryHelper.SpecifyNullableUtc(candidate.DecidedAt),
            candidate.DecisionNotes,
            candidate.EnrolledStudentId,
            AdmissionsQueryHelper.SpecifyUtc(candidate.CreatedAt),
            AdmissionsQueryHelper.SpecifyUtc(candidate.UpdatedAt),
            candidate.Guardians
                .OrderBy(g => g.RelationshipType.ToCode(), StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.FullName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Id)
                .Select(g => g.ToDto())
                .ToArray());

    public static EnrolledStudentGuardianDto ToEnrolledDto(this StudentGuardian guardian) =>
        new(
            guardian.Id,
            guardian.StudentId,
            guardian.FullName,
            guardian.RelationshipType.ToCode(),
            guardian.Phone,
            guardian.Email,
            guardian.Address,
            AdmissionsQueryHelper.SpecifyUtc(guardian.CreatedAt),
            AdmissionsQueryHelper.SpecifyUtc(guardian.UpdatedAt));

    public static EnrolledStudentResponse ToEnrolledResponse(this Student student) =>
        new(
            student.Id,
            student.CandidateId,
            student.Nis,
            student.Nisn,
            student.FullName,
            student.Nickname,
            student.DateOfBirth,
            student.Gender.ToCode(),
            student.Phone,
            student.Email,
            student.Address,
            student.EnrollmentDate,
            student.Status.ToCode(),
            AdmissionsQueryHelper.SpecifyUtc(student.CreatedAt),
            AdmissionsQueryHelper.SpecifyUtc(student.UpdatedAt),
            student.Guardians
                .OrderBy(g => g.RelationshipType.ToCode(), StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.FullName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Id)
                .Select(g => g.ToEnrolledDto())
                .ToArray());
}
