using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.Modules.Opr.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Opr;

public sealed class StudyGroupTimetableTests
{
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _studyGroupId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidInputs_Succeeds()
    {
        var startDate = new DateOnly(2026, 8, 3);
        var endDate = new DateOnly(2026, 8, 14);
        var utcNow = new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc);

        var result = StudyGroupTimetable.Create(
            _periodId,
            _studyGroupId,
            "  Kelas 10 SMK  ",
            "GENERAL",
            startDate,
            endDate,
            utcNow);

        Assert.True(result.IsSuccess);
        var block = result.Value!;
        Assert.NotEqual(Guid.Empty, block.Id);
        Assert.Equal(_periodId, block.OperationalPeriodId);
        Assert.Equal(_studyGroupId, block.StudyGroupId);
        Assert.Equal("Kelas 10 SMK", block.StudyGroupName);
        Assert.Equal("GENERAL", block.ScheduleType);
        Assert.Equal(startDate, block.StartDate);
        Assert.Equal(endDate, block.EndDate);
        Assert.Equal(utcNow, block.CreatedAt);
        Assert.Equal(utcNow, block.UpdatedAt);
    }

    [Fact]
    public void Create_WhenStartDateEqualsEndDate_Succeeds()
    {
        var singleDate = new DateOnly(2026, 8, 3);

        var result = StudyGroupTimetable.Create(
            _periodId,
            _studyGroupId,
            "Kelas 10 SMK",
            "VOCATIONAL",
            singleDate,
            singleDate);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WhenStartDateIsAfterEndDate_FailsWithInvalidDateRange()
    {
        var startDate = new DateOnly(2026, 8, 15);
        var endDate = new DateOnly(2026, 8, 3);

        var result = StudyGroupTimetable.Create(
            _periodId,
            _studyGroupId,
            "Kelas 10 SMK",
            "GENERAL",
            startDate,
            endDate);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.Timetable.InvalidDateRange", result.Error.Code);
    }

    [Theory]
    [InlineData("REGULAR")]
    [InlineData("UNKNOWN")]
    [InlineData("")]
    public void Create_WhenBlockScheduleTypeIsNotGeneralOrVocational_Fails(string invalidType)
    {
        var result = StudyGroupTimetable.Create(
            _periodId,
            _studyGroupId,
            "Kelas 10 SMK",
            invalidType,
            new DateOnly(2026, 8, 3),
            new DateOnly(2026, 8, 14));

        Assert.True(result.IsFailure);
        Assert.Equal("Opr.Timetable.InvalidBlockScheduleType", result.Error.Code);
    }

    [Fact]
    public void Update_WhenValid_UpdatesScheduleTypeAndDates()
    {
        var block = StudyGroupTimetable.Create(
            _periodId,
            _studyGroupId,
            "Kelas 10 SMK",
            "GENERAL",
            new DateOnly(2026, 8, 3),
            new DateOnly(2026, 8, 14)).Value!;

        var updateTime = new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Utc);
        var newStartDate = new DateOnly(2026, 8, 17);
        var newEndDate = new DateOnly(2026, 8, 28);

        var result = block.Update("VOCATIONAL", newStartDate, newEndDate, updateTime);

        Assert.True(result.IsSuccess);
        Assert.Equal("VOCATIONAL", block.ScheduleType);
        Assert.Equal(newStartDate, block.StartDate);
        Assert.Equal(newEndDate, block.EndDate);
        Assert.Equal(updateTime, block.UpdatedAt);
    }

    [Fact]
    public void Rehydrate_ReconstructsExactState()
    {
        var id = Guid.NewGuid();
        var startDate = new DateOnly(2026, 9, 1);
        var endDate = new DateOnly(2026, 9, 15);
        var createdAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc);

        var block = StudyGroupTimetable.Rehydrate(
            id,
            _periodId,
            _studyGroupId,
            "Kelas 10 SMK",
            "GENERAL",
            startDate,
            endDate,
            createdAt,
            updatedAt);

        Assert.Equal(id, block.Id);
        Assert.Equal(_periodId, block.OperationalPeriodId);
        Assert.Equal(_studyGroupId, block.StudyGroupId);
        Assert.Equal("GENERAL", block.ScheduleType);
        Assert.Equal(startDate, block.StartDate);
        Assert.Equal(endDate, block.EndDate);
        Assert.Equal(createdAt, block.CreatedAt);
        Assert.Equal(updatedAt, block.UpdatedAt);
    }
}
