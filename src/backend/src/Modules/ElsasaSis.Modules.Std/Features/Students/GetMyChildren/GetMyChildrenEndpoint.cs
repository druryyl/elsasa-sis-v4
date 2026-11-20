using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Std.Features.Students.GetMyChildren;

public sealed class GetMyChildrenEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/std/students/my-children", async (
                GetMyChildrenHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(new GetMyChildrenQuery(), ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Students - Children")
            .RequireAuthorization(Permissions.Students.Children.View)
            .Produces<IReadOnlyList<LinkedChildDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
