using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Tch.Features.Teachers.AddTeachingCapability;

public sealed class AddTeachingCapabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tch/teachers/{id:guid}/capabilities", async (
                Guid id,
                AddTeachingCapabilityRequest request,
                AddTeachingCapabilityHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/tch/teachers/{id}/capabilities/{result.Value!.SubjectId}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Teaching - Capabilities")
            .RequireAuthorization(Permissions.Teaching.Capabilities.Manage)
            .AddEndpointFilter<ValidationFilter<AddTeachingCapabilityRequest>>()
            .Produces<TeachingCapabilityDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
