namespace ElsasaSis.Modules.Tch.Features.Teachers.GetTeachers;

public sealed record GetTeachersQuery(
    string? Status = null,
    Guid? SubjectId = null,
    int Page = 1,
    int PageSize = 20,
    string? SortField = null,
    string? SortOrder = null,
    string? Search = null);
