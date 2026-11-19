namespace ElsasaSis.Modules.Opr.Features.Rooms.GetRooms;

public sealed record GetRoomsQuery(
    string? RoomType = null,
    int Page = 1,
    int PageSize = 20,
    string? SortField = null,
    string? SortOrder = null,
    string? Search = null);
