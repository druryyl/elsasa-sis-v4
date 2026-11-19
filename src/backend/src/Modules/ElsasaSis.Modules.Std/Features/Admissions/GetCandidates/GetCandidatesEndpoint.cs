using ElsasaSis.SharedKernel.Endpoints;
using ElsasaSis.SharedKernel.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Std.Features.Admissions.GetCandidates;

public sealed class GetCandidatesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/std/candidates", async (
                [AsParameters] GetCandidatesQuery query,
                GetCandidatesHandler handler,
                CancellationToken ct) =>
            {
                var response = await handler.HandleAsync(query, ct);
                return Results.Ok(response);
            })
            .WithTags("Students - Admissions")
            .RequireAuthorization(Permissions.Students.Admissions.View)
            .Produces<PaginatedResponse<CandidateSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
