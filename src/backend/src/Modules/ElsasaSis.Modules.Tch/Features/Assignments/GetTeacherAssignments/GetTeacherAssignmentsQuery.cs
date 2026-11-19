namespace ElsasaSis.Modules.Tch.Features.Assignments.GetTeacherAssignments;

public sealed record GetTeacherAssignmentsQuery(
    Guid? OperationalPeriodId = null,
    Guid? AcademicYearId = null,
    Guid? StudyGroupId = null,
    Guid? TeacherId = null,
    Guid? SubjectId = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20,
    string? SortField = null,
    string? SortOrder = null,
    string? Search = null);
