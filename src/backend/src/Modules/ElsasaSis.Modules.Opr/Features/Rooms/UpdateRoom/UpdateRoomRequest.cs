namespace ElsasaSis.Modules.Opr.Features.Rooms.UpdateRoom;

public sealed record UpdateRoomRequest(
    string Name,
    string RoomType,
    int Capacity);
