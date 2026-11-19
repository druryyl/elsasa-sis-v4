using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Opr.Features.Rooms.CreateRoom;

public sealed class CreateRoomHandler(
    IRoomRepository repository,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<RoomDto>> HandleAsync(
        CreateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var roomTypeResult = RoomTypeExtensions.Parse(request.RoomType);
        if (roomTypeResult.IsFailure)
        {
            return Result<RoomDto>.Failure(roomTypeResult.Error);
        }

        var trimmedName = request.Name.Trim();
        if (await repository.ExistsByNameAsync(trimmedName, excludeId: null, cancellationToken))
        {
            return Result<RoomDto>.Failure(OprErrors.DuplicateRoomName(trimmedName));
        }

        var createResult = Room.Create(
            request.Name,
            roomTypeResult.Value,
            request.Capacity);

        if (createResult.IsFailure)
        {
            return Result<RoomDto>.Failure(createResult.Error);
        }

        var room = createResult.Value!;

        var saveResult = await repository.AddAsync(room, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<RoomDto>.Failure(saveResult.Error);
        }

        if (room.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(room.DomainEvents, cancellationToken);
            room.ClearDomainEvents();
        }

        return Result<RoomDto>.Success(room.ToDto());
    }
}
