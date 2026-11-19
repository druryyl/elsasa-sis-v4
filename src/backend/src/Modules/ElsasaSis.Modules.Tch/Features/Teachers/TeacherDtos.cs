using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.Modules.Tch.Features.Assignments;

namespace ElsasaSis.Modules.Tch.Features.Teachers;

public sealed record TeachingCapabilityDto(
    Guid Id,
    Guid TeacherId,
    Guid SubjectId,
    string SubjectName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record TeacherDto(
    Guid Id,
    string FullName,
    string? Nickname,
    string? Nip,
    string? Nik,
    DateOnly DateOfBirth,
    string Gender,
    string? Phone,
    string? Email,
    string? Address,
    string Status,
    int ActiveAssignmentCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<TeachingCapabilityDto> Capabilities);

public sealed record TeacherDetailDto(
    Guid Id,
    string FullName,
    string? Nickname,
    string? Nip,
    string? Nik,
    DateOnly DateOfBirth,
    string Gender,
    string? Phone,
    string? Email,
    string? Address,
    string Status,
    int ActiveAssignmentCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<TeachingCapabilityDto> Capabilities,
    IReadOnlyList<TeacherAssignmentDto> Assignments);

internal static class TeacherMappings
{
    public static TeachingCapabilityDto ToDto(this TeachingCapability capability) =>
        new(
            capability.Id,
            capability.TeacherId,
            capability.SubjectId,
            capability.SubjectName,
            DateTime.SpecifyKind(capability.CreatedAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(capability.UpdatedAt, DateTimeKind.Utc));

    public static TeacherDto ToDto(this Teacher teacher, int activeAssignmentCount) =>
        new(
            teacher.Id,
            teacher.FullName,
            teacher.Nickname,
            teacher.Nip,
            teacher.Nik,
            teacher.DateOfBirth,
            teacher.Gender.ToString().ToUpperInvariant(),
            teacher.Phone,
            teacher.Email,
            teacher.Address,
            teacher.Status.ToString().ToUpperInvariant(),
            activeAssignmentCount,
            DateTime.SpecifyKind(teacher.CreatedAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(teacher.UpdatedAt, DateTimeKind.Utc),
            teacher.Capabilities
                .OrderBy(c => c.SubjectName, StringComparer.OrdinalIgnoreCase)
                .Select(c => c.ToDto())
                .ToArray());
}
