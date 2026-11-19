using System.Data.Common;
using Dapper;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Opr.Infrastructure.Persistence;

public sealed class RoomRepository(ISqlConnectionFactory connectionFactory) : IRoomRepository
{
    private const string UniqueRoomNameConstraint = "UQ_rooms_name";

    private const string SelectByIdSql = """
        SELECT id, name, room_type, capacity, created_at, updated_at
        FROM opr.rooms
        WHERE id = @Id;
        """;

    private const string ExistsByNameSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM opr.rooms
            WHERE LOWER(LTRIM(RTRIM(name))) = LOWER(LTRIM(RTRIM(@Name)))
              AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string InsertSql = """
        INSERT INTO opr.rooms (id, name, room_type, capacity, created_at, updated_at)
        VALUES (@Id, @Name, @RoomType, @Capacity, @CreatedAt, @UpdatedAt);
        """;

    private const string UpdateSql = """
        UPDATE opr.rooms
        SET name = @Name,
            room_type = @RoomType,
            capacity = @Capacity,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    public async Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<RoomRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var roomTypeResult = RoomTypeExtensions.Parse(row.room_type);
        if (roomTypeResult.IsFailure)
        {
            throw new InvalidOperationException($"Unsupported room_type '{row.room_type}' stored for room '{row.id}'.");
        }

        return Room.Rehydrate(
            row.id,
            row.name,
            roomTypeResult.Value,
            row.capacity,
            DateTime.SpecifyKind(row.created_at, DateTimeKind.Utc),
            DateTime.SpecifyKind(row.updated_at, DateTimeKind.Utc));
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                ExistsByNameSql,
                new { Name = name, ExcludeId = excludeId },
                cancellationToken: cancellationToken));
    }

    public async Task<Result> AddAsync(Room room, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                InsertSql,
                ToParameters(room),
                cancellationToken: cancellationToken));

            return Result.Success();
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, UniqueRoomNameConstraint))
        {
            return Result.Failure(OprErrors.DuplicateRoomName(room.Name));
        }
    }

    public async Task<Result> UpdateAsync(Room room, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                UpdateSql,
                ToParameters(room),
                cancellationToken: cancellationToken));

            return Result.Success();
        }
        catch (DbException ex) when (SqlExceptionHelper.IsUniqueConstraintViolation(ex, UniqueRoomNameConstraint))
        {
            return Result.Failure(OprErrors.DuplicateRoomName(room.Name));
        }
    }

    private static object ToParameters(Room room) => new
    {
        room.Id,
        room.Name,
        RoomType = room.RoomType.ToCode(),
        room.Capacity,
        room.CreatedAt,
        room.UpdatedAt
    };

    private sealed class RoomRow
    {
        public Guid id { get; init; }
        public string name { get; init; } = string.Empty;
        public string room_type { get; init; } = string.Empty;
        public int capacity { get; init; }
        public DateTime created_at { get; init; }
        public DateTime updated_at { get; init; }
    }
}
