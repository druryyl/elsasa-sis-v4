using ElsasaSis.SharedKernel.Endpoints;
using ElsasaSis.SharedKernel.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Tch.Features.Assignments.GetTeacherAssignments;

public sealed class GetTeacherAssignmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tch/assignments", async (
                [AsParameters] GetTeacherAssignmentsQuery query,
                GetTeacherAssignmentsHandler handler,
                CancellationToken ct) =>
            {
                var response = await handler.HandleAsync(query, ct);
                return Results.Ok(response);
            })
            .WithTags("Teaching - Assignments")
            .RequireAuthorization(Policies.Teaching.Assignments.ViewOrOversight)
            .Produces<PaginatedResponse<TeacherAssignmentDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
