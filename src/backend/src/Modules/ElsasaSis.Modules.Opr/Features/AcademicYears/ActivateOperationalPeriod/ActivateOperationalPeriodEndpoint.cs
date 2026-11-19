using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Opr.Features.AcademicYears.ActivateOperationalPeriod;

public sealed class ActivateOperationalPeriodEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/opr/academic-years/{id:guid}/periods/{periodId:guid}/activate", async (
                Guid id,
                Guid periodId,
                ActivateOperationalPeriodHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, periodId, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Operations - Academic Years")
            .RequireAuthorization(Permissions.Operations.Calendar.Manage)
            .Produces<AcademicYearDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
