namespace ElsasaSis.Modules.Std.Features.Admissions.EnrollCandidate;

public sealed record EnrollCandidateRequest(
    string Nis,
    string? Nisn = null,
    DateOnly? EnrollmentDate = null);
