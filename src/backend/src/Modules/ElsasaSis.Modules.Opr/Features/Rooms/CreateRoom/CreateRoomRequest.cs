namespace ElsasaSis.Modules.Opr.Features.Rooms.CreateRoom;

public sealed record CreateRoomRequest(
    string Name,
    string RoomType,
    int Capacity);
