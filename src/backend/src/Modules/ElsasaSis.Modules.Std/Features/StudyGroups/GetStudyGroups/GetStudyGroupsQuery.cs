namespace ElsasaSis.Modules.Std.Features.StudyGroups.GetStudyGroups;

public sealed record GetStudyGroupsQuery(
    Guid? AcademicYearId = null,
    string? SchoolLevel = null,
    string? Grade = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20,
    string? SortField = null,
    string? SortOrder = null);
