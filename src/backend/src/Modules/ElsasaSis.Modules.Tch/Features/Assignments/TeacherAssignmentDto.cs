using ElsasaSis.Modules.Tch.Domain;

namespace ElsasaSis.Modules.Tch.Features.Assignments;

public sealed record TeacherAssignmentDto(
    Guid Id,
    Guid TeacherId,
    string TeacherName,
    Guid AcademicYearId,
    string AcademicYearName,
    Guid OperationalPeriodId,
    string OperationalPeriodName,
    Guid SubjectId,
    string SubjectName,
    Guid StudyGroupId,
    string StudyGroupName,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

internal static class TeacherAssignmentMappings
{
    public static TeacherAssignmentDto ToDto(this TeacherAssignment assignment) =>
        new(
            assignment.Id,
            assignment.TeacherId,
            assignment.TeacherName,
            assignment.AcademicYearId,
            assignment.AcademicYearName,
            assignment.OperationalPeriodId,
            assignment.OperationalPeriodName,
            assignment.SubjectId,
            assignment.SubjectName,
            assignment.StudyGroupId,
            assignment.StudyGroupName,
            assignment.Status.ToString().ToUpperInvariant(),
            DateTime.SpecifyKind(assignment.CreatedAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(assignment.UpdatedAt, DateTimeKind.Utc));
}
