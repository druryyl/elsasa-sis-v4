namespace ElsasaSis.Modules.Std.Features.Admissions.RegisterCandidate;

public sealed record RegisterCandidateGuardianRequest(
    string FullName,
    string RelationshipType,
    string? Phone = null,
    string? Email = null,
    string? Address = null);

public sealed record RegisterCandidateRequest(
    Guid AdmissionYearId,
    string FullName,
    string? Nickname,
    DateOnly DateOfBirth,
    string Gender,
    string? Phone,
    string? Email,
    string? Address,
    string TargetSchoolLevel,
    string TargetGrade,
    IReadOnlyList<RegisterCandidateGuardianRequest> Guardians);
