using ElsasaSis.SharedKernel.Endpoints;
using ElsasaSis.SharedKernel.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Tch.Features.Teachers.GetTeachers;

public sealed class GetTeachersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tch/teachers", async (
                [AsParameters] GetTeachersQuery query,
                GetTeachersHandler handler,
                CancellationToken ct) =>
            {
                var response = await handler.HandleAsync(query, ct);
                return Results.Ok(response);
            })
            .WithTags("Teaching - Teachers")
            .RequireAuthorization(Policies.Teaching.Assignments.ViewOrOversight)
            .Produces<PaginatedResponse<TeacherDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
