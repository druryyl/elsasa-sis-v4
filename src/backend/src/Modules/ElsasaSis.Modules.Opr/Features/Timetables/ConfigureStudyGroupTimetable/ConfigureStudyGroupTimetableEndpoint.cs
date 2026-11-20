using ElsasaSis.Modules.Opr.Features.AcademicYears;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Opr.Features.Timetables.ConfigureStudyGroupTimetable;

public sealed class ConfigureStudyGroupTimetableEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/opr/study-groups/{id:guid}/timetables", async (
                Guid id,
                ConfigureStudyGroupTimetableRequest request,
                ConfigureStudyGroupTimetableHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Operations - Timetables")
            .RequireAuthorization(Permissions.Operations.Timetables.Manage)
            .AddEndpointFilter<ValidationFilter<ConfigureStudyGroupTimetableRequest>>()
            .Produces<StudyGroupScheduleDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        app.MapDelete("/api/opr/study-groups/{id:guid}/timetables/{scheduleId:guid}", async (
                Guid id,
                Guid scheduleId,
                DeleteStudyGroupTimetableSlotHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, scheduleId, ct);
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
    }
}
