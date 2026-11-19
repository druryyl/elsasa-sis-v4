using Dapper;
using ElsasaSis.SharedKernel.Pagination;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Opr.Features.Rooms.GetRooms;

public sealed class GetRoomsHandler(ISqlConnectionFactory connectionFactory)
{
    public async Task<PaginatedResponse<RoomDto>> HandleAsync(
        GetRoomsQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, offset) = OprQueryHelper.NormalizePagination(query.Page, query.PageSize);
        var searchPattern = OprQueryHelper.ToSearchPattern(query.Search);
        var normalizedRoomType = string.IsNullOrWhiteSpace(query.RoomType)
            ? null
            : query.RoomType.Trim().ToUpperInvariant();
        var orderBy = ResolveOrderBy(query.SortField, query.SortOrder);

        var conditions = new List<string>();
        if (normalizedRoomType is not null)
        {
            conditions.Add("room_type = @RoomType");
        }

        if (searchPattern is not null)
        {
            conditions.Add("name LIKE @SearchPattern ESCAPE '\\'");
        }

        var whereClause = conditions.Count == 0
            ? string.Empty
            : $"WHERE {string.Join(" AND ", conditions)}";

        var countSql = $"""
            SELECT COUNT(1)
            FROM opr.rooms
            {whereClause};
            """;

        var itemsSql = $"""
            SELECT id, name, room_type, capacity, created_at, updated_at
            FROM opr.rooms
            {whereClause}
            ORDER BY {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new
        {
            RoomType = normalizedRoomType,
            SearchPattern = searchPattern,
            Offset = offset,
            PageSize = pageSize
        };

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        if (totalCount == 0)
        {
            return new PaginatedResponse<RoomDto>([], page, pageSize, 0);
        }

        var rows = await connection.QueryAsync<RoomRow>(
            new CommandDefinition(itemsSql, parameters, cancellationToken: cancellationToken));

        var items = rows
            .Select(r => new RoomDto(
                r.id,
                r.name,
                r.room_type,
                r.capacity,
                DateTime.SpecifyKind(r.created_at, DateTimeKind.Utc),
                DateTime.SpecifyKind(r.updated_at, DateTimeKind.Utc)))
            .ToArray();

        return new PaginatedResponse<RoomDto>(items, page, pageSize, totalCount);
    }

    private static string ResolveOrderBy(string? sortField, string? sortOrder)
    {
        var direction = OprQueryHelper.ResolveSortDirection(sortOrder);

        return sortField?.Trim().ToLowerInvariant() switch
        {
            "name" => $"name {direction ?? "ASC"}",
            "roomtype" or "room_type" => $"room_type {direction ?? "ASC"}, name ASC",
            "capacity" => $"capacity {direction ?? "ASC"}, name ASC",
            "createdat" or "created_at" => $"created_at {direction ?? "DESC"}, name ASC",
            _ => "name ASC"
        };
    }

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
