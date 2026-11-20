using ElsasaSis.Modules.Opr.Features.AcademicYears;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Opr.Features.Timetables.ManageSmkBlockRotations;

public sealed class ManageSmkBlockRotationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/opr/study-groups/{id:guid}/blocks", async (
                Guid id,
                CreateSmkBlockRequest request,
                CreateSmkBlockHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Operations - Timetables")
            .RequireAuthorization(Permissions.Operations.Timetables.Manage)
            .AddEndpointFilter<ValidationFilter<CreateSmkBlockRequest>>()
            .Produces<StudyGroupTimetableBlockDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        app.MapDelete("/api/opr/study-groups/{id:guid}/blocks/{blockId:guid}", async (
                Guid id,
                Guid blockId,
                DeleteSmkBlockHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, blockId, ct);
                return result.IsSuccess
                    ? Results.NoContent()
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Operations - Timetables")
            .RequireAuthorization(Permissions.Operations.Timetables.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapGet("/api/opr/study-groups/{id:guid}/blocks", async (
                Guid id,
                [FromQuery] Guid periodId,
                GetSmkBlocksHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, periodId, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Operations - Timetables")
            .RequireAuthorization(Policies.Operations.Timetables.ViewOrManageOrOwn)
            .Produces<IReadOnlyList<StudyGroupTimetableBlockDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
