using ElsasaSis.Modules.Std.Domain;

namespace ElsasaSis.Modules.Std.Features.Students;

public sealed record StudentGuardianDto(
    Guid Id,
    Guid StudentId,
    string FullName,
    string RelationshipType,
    string? Phone,
    string? Email,
    string? Address,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record StudentGuardianContactDto(
    Guid Id,
    string FullName,
    string RelationshipType,
    string? Phone,
    string? Email);

public sealed record StudentPlacementDto(
    Guid Id,
    Guid StudentId,
    Guid AcademicYearId,
    string AcademicYearName,
    string SchoolLevel,
    string Grade,
    Guid? StudyGroupId,
    string? StudyGroupName,
    Guid? HomeroomTeacherId,
    string? HomeroomTeacherName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record StudentDto(
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
    IReadOnlyList<StudentGuardianDto> Guardians);

public sealed record StudentListItemDto(
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
    Guid? AcademicYearId,
    string? AcademicYearName,
    string? SchoolLevel,
    string? Grade,
    Guid? StudyGroupId,
    string? StudyGroupName,
    string? PrimaryGuardianName,
    string? PrimaryGuardianPhone,
    IReadOnlyList<StudentGuardianContactDto> Guardians,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record StudentDetailDto(
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
    IReadOnlyList<StudentGuardianDto> Guardians,
    IReadOnlyList<StudentPlacementDto> Placements);

internal static class StudentMappings
{
    public static StudentGuardianDto ToDto(this StudentGuardian guardian) =>
        new(
            guardian.Id,
            guardian.StudentId,
            guardian.FullName,
            guardian.RelationshipType.ToCode(),
            guardian.Phone,
            guardian.Email,
            guardian.Address,
            guardian.CreatedAt,
            guardian.UpdatedAt);

    public static StudentDto ToDto(this Student student) =>
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
            student.CreatedAt,
            student.UpdatedAt,
            student.Guardians
                .OrderBy(g => g.RelationshipType.ToCode(), StringComparer.Ordinal)
                .ThenBy(g => g.FullName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Id)
                .Select(g => g.ToDto())
                .ToArray());

    public static StudentPlacementDto ToDto(
        this StudentPlacement placement,
        Guid? homeroomTeacherId = null,
        string? homeroomTeacherName = null) =>
        new(
            placement.Id,
            placement.StudentId,
            placement.AcademicYearId,
            placement.AcademicYearName,
            placement.SchoolLevel.ToCode(),
            placement.Grade,
            placement.StudyGroupId,
            placement.StudyGroupName,
            homeroomTeacherId,
            homeroomTeacherName,
            placement.CreatedAt,
            placement.UpdatedAt);
}
