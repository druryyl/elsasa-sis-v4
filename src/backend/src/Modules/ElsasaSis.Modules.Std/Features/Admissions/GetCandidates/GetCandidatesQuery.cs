namespace ElsasaSis.Modules.Std.Features.Admissions.GetCandidates;

public sealed record GetCandidatesQuery(
    Guid? AdmissionYearId = null,
    string? Status = null,
    string? SchoolLevel = null,
    string? Grade = null,
    int Page = 1,
    int PageSize = 20,
    string? SortField = null,
    string? SortOrder = null,
    string? Search = null);
