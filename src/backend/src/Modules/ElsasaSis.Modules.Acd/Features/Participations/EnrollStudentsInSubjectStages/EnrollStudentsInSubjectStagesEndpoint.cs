using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Acd.Features.Participations.EnrollStudentsInSubjectStages;

public sealed class EnrollStudentsInSubjectStagesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/acd/participations/batch", async (
                EnrollStudentsInSubjectStagesRequest request,
                EnrollStudentsInSubjectStagesHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Academic - Participations")
            .RequireAuthorization(Permissions.Academic.Curriculum.Manage)
            .AddEndpointFilter<ValidationFilter<EnrollStudentsInSubjectStagesRequest>>()
            .Produces<EnrollStudentsInSubjectStagesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
