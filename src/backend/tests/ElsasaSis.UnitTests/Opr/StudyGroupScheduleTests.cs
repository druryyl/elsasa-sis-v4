using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.Modules.Opr.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Opr;

public sealed class StudyGroupScheduleTests
{
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _studyGroupId = Guid.NewGuid();
    private readonly Guid _subjectId = Guid.NewGuid();
    private readonly Guid _teacherId = Guid.NewGuid();
    private readonly Guid _roomId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidInputs_SucceedsAndSetsTimestamps()
    {
        var utcNow = new DateTime(2026, 7, 15, 8, 0, 0, DateTimeKind.Utc);
        var startTime = new TimeOnly(7, 0);
        var endTime = new TimeOnly(7, 45);

        var result = StudyGroupSchedule.Create(
            _periodId,
            _studyGroupId,
            "  Kelas 7A  ",
            "REGULAR",
            1,
            1,
            startTime,
            endTime,
            _subjectId,
            "  Matematika  ",
            _teacherId,
            "  Budi Santoso  ",
            _roomId,
            "  Ruang 101  ",
            utcNow);

        Assert.True(result.IsSuccess);
        var schedule = result.Value!;
        Assert.NotEqual(Guid.Empty, schedule.Id);
        Assert.Equal(_periodId, schedule.OperationalPeriodId);
        Assert.Equal(_studyGroupId, schedule.StudyGroupId);
        Assert.Equal("Kelas 7A", schedule.StudyGroupName);
        Assert.Equal("REGULAR", schedule.ScheduleType);
        Assert.Equal(1, schedule.DayOfWeek);
        Assert.Equal(1, schedule.PeriodSlot);
        Assert.Equal(startTime, schedule.StartTime);
        Assert.Equal(endTime, schedule.EndTime);
        Assert.Equal(_subjectId, schedule.SubjectId);
        Assert.Equal("Matematika", schedule.SubjectName);
        Assert.Equal(_teacherId, schedule.TeacherId);
        Assert.Equal("Budi Santoso", schedule.TeacherName);
        Assert.Equal(_roomId, schedule.RoomId);
        Assert.Equal("Ruang 101", schedule.RoomName);
        Assert.Equal(utcNow, schedule.CreatedAt);
        Assert.Equal(utcNow, schedule.UpdatedAt);
    }

    [Fact]
    public void Create_WhenStartTimeEqualsEndTime_FailsWithInvalidTimeRange()
    {
        var sameTime = new TimeOnly(8, 0);

        var result = StudyGroupSchedule.Create(
            _periodId,
            _studyGroupId,
            "Kelas 7A",
            "REGULAR",
            1,
            1,
            sameTime,
            sameTime,
            _subjectId,
            "Matematika",
            _teacherId,
            "Budi Santoso");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.Timetable.InvalidTimeRange", result.Error.Code);
    }

    [Fact]
    public void Create_WhenStartTimeIsAfterEndTime_FailsWithInvalidTimeRange()
    {
        var startTime = new TimeOnly(8, 45);
        var endTime = new TimeOnly(8, 0);

        var result = StudyGroupSchedule.Create(
            _periodId,
            _studyGroupId,
            "Kelas 7A",
            "REGULAR",
            1,
            1,
            startTime,
            endTime,
            _subjectId,
            "Matematika",
            _teacherId,
            "Budi Santoso");

        Assert.True(result.IsFailure);
        Assert.Equal("Opr.Timetable.InvalidTimeRange", result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public void Create_WhenDayOfWeekOutOfRange_FailsWithInvalidDayOfWeek(int invalidDay)
    {
        var result = StudyGroupSchedule.Create(
            _periodId,
            _studyGroupId,
            "Kelas 7A",
            "REGULAR",
            invalidDay,
            1,
            new TimeOnly(7, 0),
            new TimeOnly(7, 45),
            _subjectId,
            "Matematika",
            _teacherId,
            "Budi Santoso");

        Assert.True(result.IsFailure);
        Assert.Equal("Opr.Timetable.InvalidDayOfWeek", result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WhenPeriodSlotInvalid_FailsWithInvalidPeriodSlot(int invalidSlot)
    {
        var result = StudyGroupSchedule.Create(
            _periodId,
            _studyGroupId,
            "Kelas 7A",
            "REGULAR",
            1,
            invalidSlot,
            new TimeOnly(7, 0),
            new TimeOnly(7, 45),
            _subjectId,
            "Matematika",
            _teacherId,
            "Budi Santoso");

        Assert.True(result.IsFailure);
        Assert.Equal("Opr.Timetable.InvalidPeriodSlot", result.Error.Code);
    }

    [Fact]
    public void Create_WhenScheduleTypeInvalid_FailsWithInvalidScheduleType()
    {
        var result = StudyGroupSchedule.Create(
            _periodId,
            _studyGroupId,
            "Kelas 7A",
            "INVALID_TYPE",
            1,
            1,
            new TimeOnly(7, 0),
            new TimeOnly(7, 45),
            _subjectId,
            "Matematika",
            _teacherId,
            "Budi Santoso");

        Assert.True(result.IsFailure);
        Assert.Equal("Opr.Timetable.InvalidScheduleType", result.Error.Code);
    }

    [Fact]
    public void AssignRoom_UpdatesRoomAndTimestamp()
    {
        var schedule = StudyGroupSchedule.Create(
            _periodId,
            _studyGroupId,
            "Kelas 7A",
            "REGULAR",
            1,
            1,
            new TimeOnly(7, 0),
            new TimeOnly(7, 45),
            _subjectId,
            "Matematika",
            _teacherId,
            "Budi Santoso").Value!;

        var newRoomId = Guid.NewGuid();
        var updateTime = new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc);

        var result = schedule.AssignRoom(newRoomId, "Lab IPA", updateTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(newRoomId, schedule.RoomId);
        Assert.Equal("Lab IPA", schedule.RoomName);
        Assert.Equal(updateTime, schedule.UpdatedAt);
    }

    [Fact]
    public void UpdateSlot_WithValidData_UpdatesProperties()
    {
        var schedule = StudyGroupSchedule.Create(
            _periodId,
            _studyGroupId,
            "Kelas 7A",
            "REGULAR",
            1,
            1,
            new TimeOnly(7, 0),
            new TimeOnly(7, 45),
            _subjectId,
            "Matematika",
            _teacherId,
            "Budi Santoso").Value!;

        var newSubjectId = Guid.NewGuid();
        var newTeacherId = Guid.NewGuid();
        var updateTime = new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc);

        var result = schedule.UpdateSlot(
            new TimeOnly(8, 0),
            new TimeOnly(8, 45),
            newSubjectId,
            "Bahasa Inggris",
            newTeacherId,
            "Siti Rahma",
            null,
            null,
            updateTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(new TimeOnly(8, 0), schedule.StartTime);
        Assert.Equal(new TimeOnly(8, 45), schedule.EndTime);
        Assert.Equal(newSubjectId, schedule.SubjectId);
        Assert.Equal("Bahasa Inggris", schedule.SubjectName);
        Assert.Equal(newTeacherId, schedule.TeacherId);
        Assert.Equal("Siti Rahma", schedule.TeacherName);
        Assert.Null(schedule.RoomId);
        Assert.Null(schedule.RoomName);
        Assert.Equal(updateTime, schedule.UpdatedAt);
    }

    [Fact]
    public void Rehydrate_ReconstructsExactState()
    {
        var id = Guid.NewGuid();
        var createdAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc);

        var schedule = StudyGroupSchedule.Rehydrate(
            id,
            _periodId,
            _studyGroupId,
            "Kelas 7A",
            "VOCATIONAL",
            2,
            3,
            new TimeOnly(9, 0),
            new TimeOnly(9, 45),
            _subjectId,
            "Teknik Mesin",
            _teacherId,
            "Budi Santoso",
            _roomId,
            "Bengkel Mesin",
            createdAt,
            updatedAt);

        Assert.Equal(id, schedule.Id);
        Assert.Equal(_periodId, schedule.OperationalPeriodId);
        Assert.Equal("VOCATIONAL", schedule.ScheduleType);
        Assert.Equal(2, schedule.DayOfWeek);
        Assert.Equal(3, schedule.PeriodSlot);
        Assert.Equal(createdAt, schedule.CreatedAt);
        Assert.Equal(updatedAt, schedule.UpdatedAt);
    }

    [Theory]
    [InlineData("REGULAR", ScheduleType.Regular)]
    [InlineData("GENERAL", ScheduleType.General)]
    [InlineData("VOCATIONAL", ScheduleType.Vocational)]
    public void ScheduleTypeExtensions_ParseAndToCode_WorksForAllTypes(string code, ScheduleType expected)
    {
        var result = ScheduleTypeExtensions.Parse(code);
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
        Assert.Equal(code, result.Value.ToCode());
    }
}
