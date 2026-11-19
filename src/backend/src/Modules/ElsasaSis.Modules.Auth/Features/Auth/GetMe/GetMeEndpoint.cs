using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Auth.Features.Auth.GetMe;

public sealed class GetMeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/me", async (
                GetMeHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Auth")
            .RequireAuthorization()
            .Produces<AuthUserDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
