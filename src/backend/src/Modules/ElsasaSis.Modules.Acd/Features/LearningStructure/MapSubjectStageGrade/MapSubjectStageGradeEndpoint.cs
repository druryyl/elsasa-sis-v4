using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.MapSubjectStageGrade;

public sealed class MapSubjectStageGradeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/acd/subjects/{id:guid}/stages/{stageId:guid}/grade-mappings", async (
                Guid id,
                Guid stageId,
                MapSubjectStageGradeRequest request,
                MapSubjectStageGradeHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, stageId, request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/acd/subjects/{id}/stages/{stageId}/grade-mappings/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Academic - Grade Mappings")
            .RequireAuthorization(Permissions.Academic.Curriculum.Manage)
            .AddEndpointFilter<ValidationFilter<MapSubjectStageGradeRequest>>()
            .Produces<SubjectStageGradeDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
