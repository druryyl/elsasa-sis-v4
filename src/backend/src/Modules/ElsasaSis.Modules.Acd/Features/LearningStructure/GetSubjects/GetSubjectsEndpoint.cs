using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.GetSubjects;

public sealed class GetSubjectsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/acd/subjects", async (
                GetSubjectsHandler handler,
                CancellationToken ct) =>
            {
                var subjects = await handler.HandleAsync(ct);
                return Results.Ok(subjects);
            })
            .WithTags("Academic - Subjects")
            .RequireAuthorization(Policies.Academic.Curriculum.ViewOrManage)
            .Produces<IReadOnlyList<SubjectResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
