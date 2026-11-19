using ElsasaSis.SharedKernel.Endpoints;
using ElsasaSis.SharedKernel.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Opr.Features.AcademicYears.GetAcademicYears;

public sealed class GetAcademicYearsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/opr/academic-years", async (
                [AsParameters] PagedQuery query,
                GetAcademicYearsHandler handler,
                CancellationToken ct) =>
            {
                var response = await handler.HandleAsync(query, ct);
                return Results.Ok(response);
            })
            .WithTags("Operations - Academic Years")
            .RequireAuthorization(Permissions.Operations.Calendar.View)
            .Produces<PaginatedResponse<AcademicYearDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
