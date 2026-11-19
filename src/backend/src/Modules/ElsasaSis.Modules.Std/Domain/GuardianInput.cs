namespace ElsasaSis.Modules.Std.Domain;

public sealed record GuardianInput(
    string FullName,
    GuardianRelationshipType RelationshipType,
    string? Phone = null,
    string? Email = null,
    string? Address = null);
