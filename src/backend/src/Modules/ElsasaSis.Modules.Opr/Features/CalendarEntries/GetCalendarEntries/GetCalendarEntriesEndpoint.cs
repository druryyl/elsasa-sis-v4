using ElsasaSis.SharedKernel.Endpoints;
using ElsasaSis.SharedKernel.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Opr.Features.CalendarEntries.GetCalendarEntries;

public sealed class GetCalendarEntriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/opr/calendar-entries", async (
                [AsParameters] GetCalendarEntriesQuery query,
                GetCalendarEntriesHandler handler,
                CancellationToken ct) =>
            {
                var response = await handler.HandleAsync(query, ct);
                return Results.Ok(response);
            })
            .WithTags("Operations - Calendar Entries")
            .RequireAuthorization(Policies.Operations.Calendar.ViewOrManage)
            .Produces<PaginatedResponse<CalendarEntryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
