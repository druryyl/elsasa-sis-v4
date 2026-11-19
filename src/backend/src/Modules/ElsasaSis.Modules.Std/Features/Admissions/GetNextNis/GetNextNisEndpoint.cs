using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Std.Features.Admissions.GetNextNis;

public sealed class GetNextNisEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/std/candidates/{id:guid}/next-nis", async (
                Guid id,
                DateOnly? enrollmentDate,
                GetNextNisHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, enrollmentDate, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Students - Admissions")
            .RequireAuthorization(Permissions.Students.Admissions.View)
            .Produces<NextNisResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
