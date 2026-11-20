using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Acd.Features.Scorebook.GetSubjectGradebookSummary;

public sealed class GetSubjectGradebookSummaryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/acd/gradebook", async (
                [FromQuery] Guid subjectStageId,
                [FromQuery] Guid studyGroupId,
                [FromQuery] Guid operationalPeriodId,
                GetSubjectGradebookSummaryHandler handler,
                CancellationToken ct) =>
            {
                var query = new GetSubjectGradebookSummaryQuery(subjectStageId, studyGroupId, operationalPeriodId);
                var result = await handler.HandleAsync(query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Academic - Scorebook")
            .RequireAuthorization(Policies.Academic.Scorebook.ViewOrManage)
            .Produces<GradebookSummaryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
