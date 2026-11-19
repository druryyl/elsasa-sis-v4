using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Opr.Domain;

public sealed class Room : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public RoomType RoomType { get; private set; }
    public int Capacity { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Room()
    {
    }

    private Room(
        Guid id,
        string name,
        RoomType roomType,
        int capacity,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        Name = name;
        RoomType = roomType;
        Capacity = capacity;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<Room> Create(
        string name,
        RoomType roomType,
        int capacity,
        DateTime? utcNow = null)
    {
        var validationResult = ValidateRoom(name, roomType, capacity);
        if (validationResult.IsFailure)
        {
            return Result<Room>.Failure(validationResult.Error);
        }

        var now = utcNow ?? DateTime.UtcNow;
        var room = new Room(
            UuidV7.NewGuid(),
            name.Trim(),
            roomType,
            capacity,
            now,
            now);

        return Result<Room>.Success(room);
    }

    public Result Update(
        string name,
        RoomType roomType,
        int capacity,
        DateTime? utcNow = null)
    {
        var validationResult = ValidateRoom(name, roomType, capacity);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        Name = name.Trim();
        RoomType = roomType;
        Capacity = capacity;
        UpdatedAt = utcNow ?? DateTime.UtcNow;

        return Result.Success();
    }

    public static Room Rehydrate(
        Guid id,
        string name,
        RoomType roomType,
        int capacity,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(id, name, roomType, capacity, createdAt, updatedAt);

    private static Result ValidateRoom(string name, RoomType roomType, int capacity)
    {
        // BR-OPS-010: Room name must be non-blank (uniqueness enforced across school).
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(OprErrors.RoomNameRequired());
        }

        if (!Enum.IsDefined(roomType))
        {
            return Result.Failure(OprErrors.InvalidRoomType(roomType.ToString()));
        }

        // BR-OPS-011: Room Capacity must be a positive number.
        if (capacity <= 0)
        {
            return Result.Failure(OprErrors.InvalidRoomCapacity());
        }

        return Result.Success();
    }
}
