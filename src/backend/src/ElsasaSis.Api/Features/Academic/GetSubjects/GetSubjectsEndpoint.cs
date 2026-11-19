using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Api.Features.Academic.GetSubjects;

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
            .RequireAuthorization()
            .Produces<IReadOnlyList<SubjectResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
