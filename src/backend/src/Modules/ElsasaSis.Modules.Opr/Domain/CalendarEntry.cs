using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Opr.Domain;

public sealed class CalendarEntry : AggregateRoot<Guid>
{
    public Guid AcademicYearId { get; private set; }
    public DateOnly EntryDate { get; private set; }
    public CalendarDayType DayType { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private CalendarEntry()
    {
    }

    private CalendarEntry(
        Guid id,
        Guid academicYearId,
        DateOnly entryDate,
        CalendarDayType dayType,
        string? description,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        AcademicYearId = academicYearId;
        EntryDate = entryDate;
        DayType = dayType;
        Description = description;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<CalendarEntry> Create(
        Guid academicYearId,
        DateOnly academicYearStartDate,
        DateOnly academicYearEndDate,
        DateOnly entryDate,
        CalendarDayType dayType,
        string? description,
        DateTime? utcNow = null)
    {
        if (academicYearId == Guid.Empty)
        {
            return Result<CalendarEntry>.Failure(OprErrors.CalendarEntryAcademicYearRequired());
        }

        var validationResult = ValidateEntry(academicYearStartDate, academicYearEndDate, entryDate, dayType);
        if (validationResult.IsFailure)
        {
            return Result<CalendarEntry>.Failure(validationResult.Error);
        }

        var now = utcNow ?? DateTime.UtcNow;
        var entry = new CalendarEntry(
            UuidV7.NewGuid(),
            academicYearId,
            entryDate,
            dayType,
            NormalizeDescription(description),
            now,
            now);

        return Result<CalendarEntry>.Success(entry);
    }

    public Result Update(
        DateOnly academicYearStartDate,
        DateOnly academicYearEndDate,
        DateOnly entryDate,
        CalendarDayType dayType,
        string? description,
        DateTime? utcNow = null)
    {
        var validationResult = ValidateEntry(academicYearStartDate, academicYearEndDate, entryDate, dayType);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        EntryDate = entryDate;
        DayType = dayType;
        Description = NormalizeDescription(description);
        UpdatedAt = utcNow ?? DateTime.UtcNow;

        return Result.Success();
    }

    public static CalendarEntry Rehydrate(
        Guid id,
        Guid academicYearId,
        DateOnly entryDate,
        CalendarDayType dayType,
        string? description,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(id, academicYearId, entryDate, dayType, description, createdAt, updatedAt);

    private static Result ValidateEntry(
        DateOnly academicYearStartDate,
        DateOnly academicYearEndDate,
        DateOnly entryDate,
        CalendarDayType dayType)
    {
        if (!Enum.IsDefined(dayType))
        {
            return Result.Failure(OprErrors.InvalidCalendarDayType(dayType.ToString()));
        }

        // BR-OPS-007: A Calendar Entry's date must fall within the date range of its Academic Year.
        if (entryDate < academicYearStartDate || entryDate > academicYearEndDate)
        {
            return Result.Failure(OprErrors.CalendarEntryDateOutOfBounds(
                entryDate,
                academicYearStartDate,
                academicYearEndDate));
        }

        return Result.Success();
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
