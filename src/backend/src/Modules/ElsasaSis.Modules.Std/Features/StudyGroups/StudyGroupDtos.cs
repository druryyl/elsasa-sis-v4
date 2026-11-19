using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.Modules.Std.Features.Students;

namespace ElsasaSis.Modules.Std.Features.StudyGroups;

public sealed record StudyGroupDto(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    string SchoolLevel,
    string Grade,
    string Name,
    Guid? HomeroomTeacherId,
    string? HomeroomTeacherName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record StudyGroupListItemDto(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    string SchoolLevel,
    string Grade,
    string Name,
    Guid? HomeroomTeacherId,
    string? HomeroomTeacherName,
    int StudentCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record StudyGroupRosterStudentDto(
    Guid Id,
    Guid PlacementId,
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
    IReadOnlyList<StudentGuardianDto> Guardians);

public sealed record StudyGroupRosterDto(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    string SchoolLevel,
    string Grade,
    string Name,
    Guid? HomeroomTeacherId,
    string? HomeroomTeacherName,
    int StudentCount,
    IReadOnlyList<StudyGroupRosterStudentDto> Students);

internal static class StudyGroupMappings
{
    public static StudyGroupDto ToDto(this StudyGroup studyGroup) =>
        new(
            studyGroup.Id,
            studyGroup.AcademicYearId,
            studyGroup.AcademicYearName,
            studyGroup.SchoolLevel.ToCode(),
            studyGroup.Grade,
            studyGroup.Name,
            studyGroup.HomeroomTeacherId,
            studyGroup.HomeroomTeacherName,
            studyGroup.CreatedAt,
            studyGroup.UpdatedAt);
}
