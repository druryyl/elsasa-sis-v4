using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Acd.Features.Monitoring.GetAcademicMonitoringProgress;

public sealed class GetAcademicMonitoringProgressEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/acd/monitoring/progress", async (
                [FromQuery] Guid? academicYearId,
                [FromQuery] Guid? operationalPeriodId,
                [FromQuery] DateOnly? date,
                GetAcademicMonitoringProgressHandler handler,
                CancellationToken ct) =>
            {
                var query = new GetAcademicMonitoringProgressQuery(academicYearId, operationalPeriodId, date);
                var result = await handler.HandleAsync(query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Academic - Monitoring")
            .RequireAuthorization(Permissions.Academic.Monitoring.View)
            .Produces<AcademicMonitoringProgressDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
