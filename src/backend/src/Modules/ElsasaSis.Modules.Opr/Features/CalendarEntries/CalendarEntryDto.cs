using ElsasaSis.Modules.Opr.Domain;

namespace ElsasaSis.Modules.Opr.Features.CalendarEntries;

public sealed record CalendarEntryDto(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    DateOnly EntryDate,
    string DayType,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);

internal static class CalendarEntryMappings
{
    public static CalendarEntryDto ToDto(this CalendarEntry entry, string academicYearName) =>
        new(
            entry.Id,
            entry.AcademicYearId,
            academicYearName,
            entry.EntryDate,
            entry.DayType.ToCode(),
            entry.Description,
            entry.CreatedAt,
            entry.UpdatedAt);
}
