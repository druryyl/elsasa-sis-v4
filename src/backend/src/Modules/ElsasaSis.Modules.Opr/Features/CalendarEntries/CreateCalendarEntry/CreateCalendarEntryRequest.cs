namespace ElsasaSis.Modules.Opr.Features.CalendarEntries.CreateCalendarEntry;

public sealed record CreateCalendarEntryRequest(
    Guid AcademicYearId,
    DateOnly EntryDate,
    string DayType,
    string? Description);
