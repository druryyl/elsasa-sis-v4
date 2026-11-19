using ElsasaSis.Modules.Opr.Features.AcademicYears;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Opr.Features.Rooms.UpdateRoom;

public sealed class UpdateRoomEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/opr/rooms/{id:guid}", async (
                Guid id,
                UpdateRoomRequest request,
                UpdateRoomHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Operations - Rooms")
            .RequireAuthorization(Permissions.Operations.Rooms.Manage)
            .AddEndpointFilter<ValidationFilter<UpdateRoomRequest>>()
            .Produces<RoomDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
