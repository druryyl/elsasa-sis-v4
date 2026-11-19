using ElsasaSis.Modules.Opr.Features.AcademicYears;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Opr.Features.CalendarEntries.UpdateCalendarEntry;

public sealed class UpdateCalendarEntryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/opr/calendar-entries/{id:guid}", async (
                Guid id,
                UpdateCalendarEntryRequest request,
                UpdateCalendarEntryHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Operations - Calendar Entries")
            .RequireAuthorization(Permissions.Operations.Calendar.Manage)
            .AddEndpointFilter<ValidationFilter<UpdateCalendarEntryRequest>>()
            .Produces<CalendarEntryDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
