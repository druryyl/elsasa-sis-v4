using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Opr.Features.Rooms.UpdateRoom;

public sealed class UpdateRoomHandler(
    IRoomRepository repository,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<RoomDto>> HandleAsync(
        Guid id,
        UpdateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await repository.GetByIdAsync(id, cancellationToken);
        if (room is null)
        {
            return Result<RoomDto>.Failure(OprErrors.RoomNotFound(id));
        }

        var roomTypeResult = RoomTypeExtensions.Parse(request.RoomType);
        if (roomTypeResult.IsFailure)
        {
            return Result<RoomDto>.Failure(roomTypeResult.Error);
        }

        var trimmedName = request.Name.Trim();
        if (await repository.ExistsByNameAsync(trimmedName, excludeId: id, cancellationToken))
        {
            return Result<RoomDto>.Failure(OprErrors.DuplicateRoomName(trimmedName));
        }

        var updateResult = room.Update(
            request.Name,
            roomTypeResult.Value,
            request.Capacity);

        if (updateResult.IsFailure)
        {
            return Result<RoomDto>.Failure(updateResult.Error);
        }

        var saveResult = await repository.UpdateAsync(room, cancellationToken);
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
