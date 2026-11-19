using ElsasaSis.Contracts.Operational.Events;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Opr;

public sealed class AcademicYearTests
{
    private static readonly DateOnly YearStart = new(2025, 7, 1);
    private static readonly DateOnly YearEnd = new(2026, 6, 30);

    private static IReadOnlyList<OperationalPeriodInput> ValidTwoSemesters() =>
    [
        new(null, "Semester 1", new DateOnly(2025, 7, 1), new DateOnly(2025, 12, 31)),
        new(null, "Semester 2", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30))
    ];

    [Fact]
    public void Create_WithValidInputs_SucceedsAsInactiveWithInactivePeriodsAndUuidV7Ids()
    {
        var utcNow = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc);

        var result = AcademicYear.Create("  2025/2026  ", YearStart, YearEnd, ValidTwoSemesters(), utcNow);

        Assert.True(result.IsSuccess);
        var year = result.Value!;
        Assert.NotEqual(Guid.Empty, year.Id);
        Assert.Equal("2025/2026", year.Name);
        Assert.Equal(YearStart, year.StartDate);
        Assert.Equal(YearEnd, year.EndDate);
        Assert.Equal(AcademicYearStatus.Inactive, year.Status);
        Assert.Equal(utcNow, year.CreatedAt);
        Assert.Equal(utcNow, year.UpdatedAt);
        Assert.Equal(2, year.Periods.Count);
        Assert.All(year.Periods, p =>
        {
            Assert.NotEqual(Guid.Empty, p.Id);
            Assert.Equal(year.Id, p.AcademicYearId);
            Assert.Equal(OperationalPeriodStatus.Inactive, p.Status);
            Assert.Equal(utcNow, p.CreatedAt);
            Assert.Equal(utcNow, p.UpdatedAt);
        });
        Assert.Empty(year.DomainEvents);
    }

    [Theory]
    [InlineData(2025, 7, 1)]
    [InlineData(2025, 6, 30)]
    public void Create_WhenEndDateNotAfterStartDate_FailsWithInvalidDateRange(int endYear, int endMonth, int endDay)
    {
        var invalidEnd = new DateOnly(endYear, endMonth, endDay);

        var result = AcademicYear.Create("2025/2026", YearStart, invalidEnd, ValidTwoSemesters());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.DateRange.Invalid", result.Error.Code);
    }

    [Fact]
    public void Create_WithZeroPeriods_FailsWithAtLeastOnePeriodRequired()
    {
        var result = AcademicYear.Create("2025/2026", YearStart, YearEnd, []);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.AcademicYear.AtLeastOnePeriodRequired", result.Error.Code);
    }

    [Fact]
    public void Create_WhenPeriodEndDateNotAfterStartDate_FailsWithInvalidDateRange()
    {
        IReadOnlyList<OperationalPeriodInput> periods =
        [
            new(null, "Semester 1", new DateOnly(2025, 8, 1), new DateOnly(2025, 8, 1))
        ];

        var result = AcademicYear.Create("2025/2026", YearStart, YearEnd, periods);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.DateRange.Invalid", result.Error.Code);
    }

    [Fact]
    public void Create_WhenPeriodStartsBeforeYearStart_FailsWithPeriodOutOfBounds()
    {
        IReadOnlyList<OperationalPeriodInput> periods =
        [
            new(null, "Semester 1", new DateOnly(2025, 6, 30), new DateOnly(2025, 12, 31))
        ];

        var result = AcademicYear.Create("2025/2026", YearStart, YearEnd, periods);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.OperationalPeriod.OutOfBounds", result.Error.Code);
    }

    [Fact]
    public void Create_WhenPeriodEndsAfterYearEnd_FailsWithPeriodOutOfBounds()
    {
        IReadOnlyList<OperationalPeriodInput> periods =
        [
            new(null, "Semester 1", new DateOnly(2025, 7, 1), new DateOnly(2026, 7, 1))
        ];

        var result = AcademicYear.Create("2025/2026", YearStart, YearEnd, periods);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.OperationalPeriod.OutOfBounds", result.Error.Code);
    }

    [Fact]
    public void Create_WhenPeriodsOverlap_FailsWithOverlappingPeriods()
    {
        IReadOnlyList<OperationalPeriodInput> periods =
        [
            new(null, "Semester 1", new DateOnly(2025, 7, 1), new DateOnly(2025, 12, 31)),
            new(null, "Semester 2", new DateOnly(2025, 12, 31), new DateOnly(2026, 6, 30))
        ];

        var result = AcademicYear.Create("2025/2026", YearStart, YearEnd, periods);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.OperationalPeriod.Overlapping", result.Error.Code);
    }

    [Fact]
    public void Create_WhenPeriodNamesDuplicateCaseInsensitive_FailsWithDuplicatePeriodName()
    {
        IReadOnlyList<OperationalPeriodInput> periods =
        [
            new(null, "Semester 1", new DateOnly(2025, 7, 1), new DateOnly(2025, 12, 31)),
            new(null, "  semester 1 ", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30))
        ];

        var result = AcademicYear.Create("2025/2026", YearStart, YearEnd, periods);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.OperationalPeriod.DuplicateName", result.Error.Code);
    }

    [Fact]
    public void Activate_WhenInactive_ActivatesYearAndEarliestPeriodAndRaisesBothDomainEvents()
    {
        // Pass periods out of chronological order to verify earliest by StartDate is selected.
        IReadOnlyList<OperationalPeriodInput> periods =
        [
            new(null, "Semester 2", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30)),
            new(null, "Semester 1", new DateOnly(2025, 7, 1), new DateOnly(2025, 12, 31))
        ];
        var year = AcademicYear.Create("2025/2026", YearStart, YearEnd, periods).Value!;
        var sem1 = year.Periods.Single(p => p.Name == "Semester 1");
        var sem2 = year.Periods.Single(p => p.Name == "Semester 2");

        var activateResult = year.Activate();

        Assert.True(activateResult.IsSuccess);
        Assert.Equal(AcademicYearStatus.Active, year.Status);
        Assert.Equal(OperationalPeriodStatus.Active, sem1.Status);
        Assert.Equal(OperationalPeriodStatus.Inactive, sem2.Status);

        Assert.Equal(2, year.DomainEvents.Count);
        var yearEvent = Assert.IsType<AcademicYearActivatedDomainEvent>(year.DomainEvents[0]);
        Assert.Equal(year.Id, yearEvent.AcademicYearId);
        Assert.Equal("2025/2026", yearEvent.Name);

        var periodEvent = Assert.IsType<OperationalPeriodActivatedDomainEvent>(year.DomainEvents[1]);
        Assert.Equal(year.Id, periodEvent.AcademicYearId);
        Assert.Equal(sem1.Id, periodEvent.OperationalPeriodId);
        Assert.Equal("Semester 1", periodEvent.Name);
    }

    [Fact]
    public void ActivatePeriod_WhenParentYearIsInactive_FailsWithConflict()
    {
        var year = AcademicYear.Create("2025/2026", YearStart, YearEnd, ValidTwoSemesters()).Value!;
        var sem2 = year.Periods.Single(p => p.Name == "Semester 2");

        var result = year.ActivatePeriod(sem2.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Opr.AcademicYear.NotActiveForPeriodActivation", result.Error.Code);
        Assert.Empty(year.DomainEvents);
    }

    [Fact]
    public void ActivatePeriod_WhenParentYearIsActive_DeactivatesOtherPeriodAndRaisesEvent()
    {
        var year = AcademicYear.Create("2025/2026", YearStart, YearEnd, ValidTwoSemesters()).Value!;
        year.Activate();
        year.ClearDomainEvents();

        var sem1 = year.Periods.Single(p => p.Name == "Semester 1");
        var sem2 = year.Periods.Single(p => p.Name == "Semester 2");

        var result = year.ActivatePeriod(sem2.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(OperationalPeriodStatus.Inactive, sem1.Status);
        Assert.Equal(OperationalPeriodStatus.Active, sem2.Status);

        var domainEvent = Assert.Single(year.DomainEvents);
        var activatedEvent = Assert.IsType<OperationalPeriodActivatedDomainEvent>(domainEvent);
        Assert.Equal(year.Id, activatedEvent.AcademicYearId);
        Assert.Equal(sem2.Id, activatedEvent.OperationalPeriodId);
        Assert.Equal("Semester 2", activatedEvent.Name);
    }

    [Fact]
    public void ActivatePeriod_WhenPeriodNotFound_FailsWithNotFound()
    {
        var year = AcademicYear.Create("2025/2026", YearStart, YearEnd, ValidTwoSemesters()).Value!;
        year.Activate();

        var result = year.ActivatePeriod(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Opr.OperationalPeriod.NotFound", result.Error.Code);
    }

    [Fact]
    public void Update_WhenYearIsActive_PreservesActiveStatusesAndUpdatesPeriods()
    {
        var createdAt = new DateTime(2025, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 6, 2, 9, 0, 0, DateTimeKind.Utc);
        var year = AcademicYear.Create("2025/2026", YearStart, YearEnd, ValidTwoSemesters(), createdAt).Value!;
        year.Activate(createdAt);

        var sem1 = year.Periods.Single(p => p.Name == "Semester 1");

        IReadOnlyList<OperationalPeriodInput> updateInputs =
        [
            new(sem1.Id, "Term 1", new DateOnly(2025, 7, 1), new DateOnly(2025, 11, 30)),
            new(null, "Term 2", new DateOnly(2025, 12, 1), new DateOnly(2026, 3, 31)),
            new(null, "Term 3", new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30))
        ];

        var result = year.Update("2025/2026 Revised", YearStart, YearEnd, updateInputs, updatedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal("2025/2026 Revised", year.Name);
        Assert.Equal(AcademicYearStatus.Active, year.Status);
        Assert.Equal(createdAt, year.CreatedAt);
        Assert.Equal(updatedAt, year.UpdatedAt);
        Assert.Equal(3, year.Periods.Count);

        var updatedSem1 = year.Periods.Single(p => p.Id == sem1.Id);
        Assert.Equal("Term 1", updatedSem1.Name);
        Assert.Equal(OperationalPeriodStatus.Active, updatedSem1.Status);
        Assert.Equal(createdAt, updatedSem1.CreatedAt);
        Assert.Equal(updatedAt, updatedSem1.UpdatedAt);

        Assert.All(year.Periods.Where(p => p.Id != sem1.Id), p =>
        {
            Assert.Equal(OperationalPeriodStatus.Inactive, p.Status);
            Assert.Equal(updatedAt, p.CreatedAt);
        });
    }

    [Fact]
    public void Update_WhenRemovingActivePeriod_FailsWithConflictCannotRemoveActivePeriod()
    {
        var year = AcademicYear.Create("2025/2026", YearStart, YearEnd, ValidTwoSemesters()).Value!;
        year.Activate();
        var sem2 = year.Periods.Single(p => p.Name == "Semester 2");

        IReadOnlyList<OperationalPeriodInput> updateWithoutActiveSem1 =
        [
            new(sem2.Id, sem2.Name, sem2.StartDate, sem2.EndDate)
        ];

        var result = year.Update("2025/2026", YearStart, YearEnd, updateWithoutActiveSem1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Opr.OperationalPeriod.CannotRemoveActive", result.Error.Code);
        Assert.Equal(2, year.Periods.Count);
    }
}
