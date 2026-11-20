namespace ElsasaSis.Modules.Std.Features.BillingLookup;

public sealed record StudentBillingContactDto(
    Guid StudentId,
    string StudentNis,
    string StudentName,
    string? SchoolLevel,
    string? Grade,
    Guid? StudyGroupId,
    string? StudyGroupName,
    string? PrimaryGuardianName,
    string? PhoneNumber,
    string? Relationship,
    string? WhatsAppUrl);
