namespace ElsasaSis.Modules.Std.Features.Students.GetStudents;

public sealed record GetStudentsQuery(
    Guid? AcademicYearId = null,
    string? SchoolLevel = null,
    string? Grade = null,
    Guid? StudyGroupId = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20,
    string? SortField = null,
    string? SortOrder = null,
    string? Search = null);
