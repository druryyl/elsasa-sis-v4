namespace ElsasaSis.Modules.Opr.Features.CalendarEntries.UpdateCalendarEntry;

public sealed record UpdateCalendarEntryRequest(
    DateOnly EntryDate,
    string DayType,
    string? Description);
