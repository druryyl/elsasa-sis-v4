using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Acd.Features.Participations.GetSubjectStageParticipations;

public sealed class GetSubjectStageParticipationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/acd/participations", async (
                [FromQuery] Guid? subjectStageId,
                [FromQuery] Guid? studyGroupId,
                [FromQuery] Guid? operationalPeriodId,
                GetSubjectStageParticipationsHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(subjectStageId, studyGroupId, operationalPeriodId, ct);
                return Results.Ok(result);
            })
            .WithTags("Academic - Participations")
            .RequireAuthorization(Policies.Academic.Curriculum.ViewOrManage)
            .Produces<IReadOnlyList<SubjectStageParticipationDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
