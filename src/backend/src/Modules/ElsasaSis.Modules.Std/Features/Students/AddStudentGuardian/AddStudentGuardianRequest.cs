namespace ElsasaSis.Modules.Std.Features.Students.AddStudentGuardian;

public sealed record AddStudentGuardianRequest(
    string FullName,
    string RelationshipType,
    string? Phone,
    string? Email,
    string? Address);
