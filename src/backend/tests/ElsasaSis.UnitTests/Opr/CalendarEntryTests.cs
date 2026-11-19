using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Opr;

public sealed class CalendarEntryTests
{
    private static readonly DateOnly YearStart = new(2026, 7, 1);
    private static readonly DateOnly YearEnd = new(2027, 6, 30);

    [Fact]
    public void Create_WithValidInputs_SucceedsWithUuidV7IdAndNormalizedDescription()
    {
        var yearId = UuidV7.NewGuid();
        var utcNow = new DateTime(2026, 7, 15, 8, 30, 0, DateTimeKind.Utc);

        var result = CalendarEntry.Create(
            yearId,
            YearStart,
            YearEnd,
            new DateOnly(2026, 8, 17),
            CalendarDayType.Holiday,
            "  Indonesian Independence Day  ",
            utcNow);

        Assert.True(result.IsSuccess);
        var entry = result.Value!;
        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(yearId, entry.AcademicYearId);
        Assert.Equal(new DateOnly(2026, 8, 17), entry.EntryDate);
        Assert.Equal(CalendarDayType.Holiday, entry.DayType);
        Assert.Equal("Indonesian Independence Day", entry.Description);
        Assert.Equal(utcNow, entry.CreatedAt);
        Assert.Equal(utcNow, entry.UpdatedAt);
    }

    [Fact]
    public void Create_WithBlankDescription_NormalizesDescriptionToNull()
    {
        var result = CalendarEntry.Create(
            UuidV7.NewGuid(),
            YearStart,
            YearEnd,
            new DateOnly(2026, 7, 13),
            CalendarDayType.SchoolDay,
            "   ");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Description);
    }

    [Theory]
    [InlineData(2026, 7, 1)]
    [InlineData(2027, 6, 30)]
    public void Create_WhenEntryDateMatchesAcademicYearBoundaries_Succeeds(int year, int month, int day)
    {
        var boundaryDate = new DateOnly(year, month, day);

        var result = CalendarEntry.Create(
            UuidV7.NewGuid(),
            YearStart,
            YearEnd,
            boundaryDate,
            CalendarDayType.SchoolDay,
            "Boundary day");

        Assert.True(result.IsSuccess);
        Assert.Equal(boundaryDate, result.Value!.EntryDate);
    }

    [Theory]
    [InlineData(2026, 6, 30)]
    [InlineData(2027, 7, 1)]
    public void Create_WhenEntryDateOutsideAcademicYear_FailsWithDateOutOfBounds(int year, int month, int day)
    {
        var invalidDate = new DateOnly(year, month, day);

        var result = CalendarEntry.Create(
            UuidV7.NewGuid(),
            YearStart,
            YearEnd,
            invalidDate,
            CalendarDayType.Holiday,
            "Out of range");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.CalendarEntry.DateOutOfBounds", result.Error.Code);
        Assert.NotNull(result.Error.Errors);
        Assert.True(result.Error.Errors!.ContainsKey("entryDate"));
    }

    [Fact]
    public void Create_WhenAcademicYearIdIsEmpty_FailsWithValidation()
    {
        var result = CalendarEntry.Create(
            Guid.Empty,
            YearStart,
            YearEnd,
            new DateOnly(2026, 8, 17),
            CalendarDayType.Holiday,
            "Holiday");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.CalendarEntry.AcademicYearRequired", result.Error.Code);
        Assert.True(result.Error.Errors!.ContainsKey("academicYearId"));
    }

    [Theory]
    [InlineData("SCHOOL_DAY", CalendarDayType.SchoolDay)]
    [InlineData("  holiday ", CalendarDayType.Holiday)]
    [InlineData("Closure", CalendarDayType.Closure)]
    public void CalendarDayTypeExtensions_ParseAndToCode_MapsValidCodes(string raw, CalendarDayType expected)
    {
        var parseResult = CalendarDayTypeExtensions.Parse(raw);

        Assert.True(parseResult.IsSuccess);
        Assert.Equal(expected, parseResult.Value);
        Assert.Equal(raw.Trim().ToUpperInvariant(), expected.ToCode());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("WEEKEND")]
    public void CalendarDayTypeExtensions_ParseInvalid_FailsWithValidation(string? raw)
    {
        var result = CalendarDayTypeExtensions.Parse(raw);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.CalendarEntry.InvalidDayType", result.Error.Code);
        Assert.True(result.Error.Errors!.ContainsKey("dayType"));
    }

    [Fact]
    public void Update_WithValidInputs_UpdatesEntryAndPreservesCreatedAt()
    {
        var createdAt = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc);
        var entry = CalendarEntry.Create(
            UuidV7.NewGuid(),
            YearStart,
            YearEnd,
            new DateOnly(2026, 9, 1),
            CalendarDayType.SchoolDay,
            "Regular day",
            createdAt).Value!;

        var result = entry.Update(
            YearStart,
            YearEnd,
            new DateOnly(2026, 9, 2),
            CalendarDayType.Closure,
            "  Emergency weather closure  ",
            updatedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2026, 9, 2), entry.EntryDate);
        Assert.Equal(CalendarDayType.Closure, entry.DayType);
        Assert.Equal("Emergency weather closure", entry.Description);
        Assert.Equal(createdAt, entry.CreatedAt);
        Assert.Equal(updatedAt, entry.UpdatedAt);
    }

    [Fact]
    public void Update_WhenEntryDateOutsideAcademicYear_FailsAndPreservesOriginalState()
    {
        var originalDate = new DateOnly(2026, 9, 1);
        var entry = CalendarEntry.Create(
            UuidV7.NewGuid(),
            YearStart,
            YearEnd,
            originalDate,
            CalendarDayType.SchoolDay,
            "Original").Value!;

        var result = entry.Update(
            YearStart,
            YearEnd,
            new DateOnly(2027, 7, 5),
            CalendarDayType.Holiday,
            "Too late");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.CalendarEntry.DateOutOfBounds", result.Error.Code);
        Assert.Equal(originalDate, entry.EntryDate);
        Assert.Equal(CalendarDayType.SchoolDay, entry.DayType);
        Assert.Equal("Original", entry.Description);
    }

    [Fact]
    public void Create_WhenDayTypeEnumIsUndefined_FailsWithInvalidDayType()
    {
        var result = CalendarEntry.Create(
            UuidV7.NewGuid(),
            YearStart,
            YearEnd,
            new DateOnly(2026, 8, 17),
            (CalendarDayType)999,
            "Invalid");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.CalendarEntry.InvalidDayType", result.Error.Code);
    }
}
