using ElsasaSis.Modules.Opr.Domain;

namespace ElsasaSis.Modules.Opr.Features.Rooms;

public sealed record RoomDto(
    Guid Id,
    string Name,
    string RoomType,
    int Capacity,
    DateTime CreatedAt,
    DateTime UpdatedAt);

internal static class RoomMappings
{
    public static RoomDto ToDto(this Room room) =>
        new(
            room.Id,
            room.Name,
            room.RoomType.ToCode(),
            room.Capacity,
            room.CreatedAt,
            room.UpdatedAt);
}
