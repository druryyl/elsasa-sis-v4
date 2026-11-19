using ElsasaSis.Modules.Opr.Features.AcademicYears;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Opr.Features.Rooms.CreateRoom;

public sealed class CreateRoomEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/opr/rooms", async (
                CreateRoomRequest request,
                CreateRoomHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/opr/rooms/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Operations - Rooms")
            .RequireAuthorization(Permissions.Operations.Rooms.Manage)
            .AddEndpointFilter<ValidationFilter<CreateRoomRequest>>()
            .Produces<RoomDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
