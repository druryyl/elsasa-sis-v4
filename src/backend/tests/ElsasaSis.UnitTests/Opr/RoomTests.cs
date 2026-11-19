using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Opr;

public sealed class RoomTests
{
    [Fact]
    public void Create_WithValidInputs_TrimsNameAndSetsTimestamps()
    {
        var utcNow = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);

        var result = Room.Create("  Room 101  ", RoomType.Classroom, 32, utcNow);

        Assert.True(result.IsSuccess);
        var room = result.Value!;
        Assert.NotEqual(Guid.Empty, room.Id);
        Assert.Equal("Room 101", room.Name);
        Assert.Equal(RoomType.Classroom, room.RoomType);
        Assert.Equal(32, room.Capacity);
        Assert.Equal(utcNow, room.CreatedAt);
        Assert.Equal(utcNow, room.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenNameIsBlank_FailsWithRoomNameRequired(string blankName)
    {
        var result = Room.Create(blankName, RoomType.Classroom, 30);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.Room.NameRequired", result.Error.Code);
        Assert.True(result.Error.Errors!.ContainsKey("name"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Create_WhenCapacityIsNotPositive_FailsWithInvalidCapacity(int invalidCapacity)
    {
        var result = Room.Create("Lab Komputer", RoomType.Laboratory, invalidCapacity);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.Room.InvalidCapacity", result.Error.Code);
        Assert.True(result.Error.Errors!.ContainsKey("capacity"));
    }

    [Theory]
    [InlineData("CLASSROOM", RoomType.Classroom)]
    [InlineData("laboratory", RoomType.Laboratory)]
    [InlineData("  Hall ", RoomType.Hall)]
    [InlineData("OTHER", RoomType.Other)]
    public void RoomTypeExtensions_ParseAndToCode_MapsAllSupportedValues(string raw, RoomType expected)
    {
        var result = RoomTypeExtensions.Parse(raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
        Assert.Equal(raw.Trim().ToUpperInvariant(), expected.ToCode());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("GYM")]
    public void RoomTypeExtensions_ParseInvalid_FailsWithValidation(string? raw)
    {
        var result = RoomTypeExtensions.Parse(raw);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.Room.InvalidRoomType", result.Error.Code);
        Assert.True(result.Error.Errors!.ContainsKey("roomType"));
    }

    [Fact]
    public void Update_WithValidInputs_UpdatesFieldsAndPreservesCreatedAt()
    {
        var createdAt = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 7, 3, 14, 0, 0, DateTimeKind.Utc);
        var room = Room.Create("Aula Lama", RoomType.Hall, 120, createdAt).Value!;

        var result = room.Update("  Aula Utama  ", RoomType.Hall, 250, updatedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal("Aula Utama", room.Name);
        Assert.Equal(RoomType.Hall, room.RoomType);
        Assert.Equal(250, room.Capacity);
        Assert.Equal(createdAt, room.CreatedAt);
        Assert.Equal(updatedAt, room.UpdatedAt);
    }

    [Fact]
    public void Update_WhenInvalidCapacity_FailsAndPreservesOriginalState()
    {
        var room = Room.Create("Lab IPA", RoomType.Laboratory, 24).Value!;

        var result = room.Update("Lab IPA Updated", RoomType.Laboratory, 0);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.Room.InvalidCapacity", result.Error.Code);
        Assert.Equal("Lab IPA", room.Name);
        Assert.Equal(24, room.Capacity);
    }

    [Fact]
    public void Create_WhenRoomTypeEnumIsUndefined_FailsWithInvalidRoomType()
    {
        var result = Room.Create("Room 202", (RoomType)999, 25);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Opr.Room.InvalidRoomType", result.Error.Code);
    }
}
