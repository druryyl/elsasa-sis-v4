using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Opr.Features.Timetables.GetTeacherPersonalSchedule;

public sealed class GetTeacherPersonalScheduleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/opr/teacher-schedule", async (
                [FromQuery] Guid periodId,
                GetTeacherPersonalScheduleHandler handler,
                CancellationToken ct) =>
            {
                var query = new GetTeacherPersonalScheduleQuery(periodId);
                var result = await handler.HandleAsync(query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Operations - Timetables")
            .RequireAuthorization(Permissions.Operations.Timetables.ViewOwn)
            .Produces<TeacherPersonalScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
