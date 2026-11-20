using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Opr.Features.Timetables.GetStudyGroupTimetable;

public sealed class GetStudyGroupTimetableEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/opr/study-groups/{id:guid}/timetables", async (
                Guid id,
                [FromQuery] Guid periodId,
                [FromQuery] string? scheduleType,
                GetStudyGroupTimetableHandler handler,
                CancellationToken ct) =>
            {
                var query = new GetStudyGroupTimetableQuery(id, periodId, scheduleType);
                var result = await handler.HandleAsync(query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Operations - Timetables")
            .RequireAuthorization(Policies.Operations.Timetables.ViewOrManageOrOwn)
            .Produces<IReadOnlyList<StudyGroupTimetableSlotDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
