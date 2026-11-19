namespace ElsasaSis.Modules.Opr.Features.CalendarEntries.GetCalendarEntries;

public sealed record GetCalendarEntriesQuery(
    Guid? AcademicYearId = null,
    string? DayType = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int Page = 1,
    int PageSize = 20,
    string? SortField = null,
    string? SortOrder = null,
    string? Search = null);
