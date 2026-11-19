using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Domain;

public enum RoomType
{
    Classroom = 0,
    Laboratory = 1,
    Hall = 2,
    Other = 3
}

public static class RoomTypeExtensions
{
    public static string ToCode(this RoomType roomType) => roomType switch
    {
        RoomType.Classroom => "CLASSROOM",
        RoomType.Laboratory => "LABORATORY",
        RoomType.Hall => "HALL",
        RoomType.Other => "OTHER",
        _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, "Unsupported room type.")
    };

    public static Result<RoomType> Parse(string? raw)
    {
        return raw?.Trim().ToUpperInvariant() switch
        {
            "CLASSROOM" => Result<RoomType>.Success(RoomType.Classroom),
            "LABORATORY" => Result<RoomType>.Success(RoomType.Laboratory),
            "HALL" => Result<RoomType>.Success(RoomType.Hall),
            "OTHER" => Result<RoomType>.Success(RoomType.Other),
            _ => Result<RoomType>.Failure(OprErrors.InvalidRoomType(raw))
        };
    }
}
