using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Tch.Features.Teachers.RemoveTeachingCapability;

public sealed class RemoveTeachingCapabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/tch/teachers/{id:guid}/capabilities/{subjectId:guid}", async (
                Guid id,
                Guid subjectId,
                RemoveTeachingCapabilityHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, subjectId, ct);
                return result.IsSuccess
                    ? Results.NoContent()
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Teaching - Capabilities")
            .RequireAuthorization(Permissions.Teaching.Capabilities.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
