namespace ElsasaSis.Modules.Std.Features.Students.UpdateStudentGuardian;

public sealed record UpdateStudentGuardianRequest(
    string FullName,
    string RelationshipType,
    string? Phone,
    string? Email,
    string? Address);
