using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Std.Features.Students.RemoveStudentGuardian;

public sealed class RemoveStudentGuardianEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/std/students/{id:guid}/guardians/{guardianId:guid}", async (
                Guid id,
                Guid guardianId,
                RemoveStudentGuardianHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, guardianId, ct);
                return result.IsSuccess
                    ? Results.NoContent()
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Students - Guardians")
            .RequireAuthorization(Permissions.Students.Directory.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
