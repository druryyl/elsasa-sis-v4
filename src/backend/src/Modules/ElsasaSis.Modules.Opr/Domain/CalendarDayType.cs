using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Domain;

public enum CalendarDayType
{
    SchoolDay = 0,
    Holiday = 1,
    Closure = 2
}

public static class CalendarDayTypeExtensions
{
    public static string ToCode(this CalendarDayType dayType) => dayType switch
    {
        CalendarDayType.SchoolDay => "SCHOOL_DAY",
        CalendarDayType.Holiday => "HOLIDAY",
        CalendarDayType.Closure => "CLOSURE",
        _ => throw new ArgumentOutOfRangeException(nameof(dayType), dayType, "Unsupported calendar day type.")
    };

    public static Result<CalendarDayType> Parse(string? raw)
    {
        return raw?.Trim().ToUpperInvariant() switch
        {
            "SCHOOL_DAY" => Result<CalendarDayType>.Success(CalendarDayType.SchoolDay),
            "HOLIDAY" => Result<CalendarDayType>.Success(CalendarDayType.Holiday),
            "CLOSURE" => Result<CalendarDayType>.Success(CalendarDayType.Closure),
            _ => Result<CalendarDayType>.Failure(OprErrors.InvalidCalendarDayType(raw))
        };
    }
}
