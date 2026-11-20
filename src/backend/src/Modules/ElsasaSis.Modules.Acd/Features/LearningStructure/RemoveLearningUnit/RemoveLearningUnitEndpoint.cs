using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.RemoveLearningUnit;

public sealed class RemoveLearningUnitEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/acd/subjects/{id:guid}/stages/{stageId:guid}/units/{unitId:guid}", async (
                Guid id,
                Guid stageId,
                Guid unitId,
                RemoveLearningUnitHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, stageId, unitId, ct);
                return result.IsSuccess
                    ? Results.NoContent()
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Academic - Learning Units")
            .RequireAuthorization(Permissions.Academic.Curriculum.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
