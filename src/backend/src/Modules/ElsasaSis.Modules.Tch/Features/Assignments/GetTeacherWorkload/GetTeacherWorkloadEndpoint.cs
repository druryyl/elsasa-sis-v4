using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Tch.Features.Assignments.GetTeacherWorkload;

public sealed class GetTeacherWorkloadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tch/workload", async (
                [AsParameters] GetTeacherWorkloadQuery query,
                GetTeacherWorkloadHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Teaching - Workload")
            .RequireAuthorization(Policies.Teaching.Assignments.ViewOrOversight)
            .Produces<TeacherWorkloadResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
