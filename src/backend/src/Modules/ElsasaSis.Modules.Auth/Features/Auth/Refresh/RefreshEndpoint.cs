using ElsasaSis.Modules.Auth.Infrastructure;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Auth.Features.Auth.Refresh;

public sealed class RefreshEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/refresh", async (
                RefreshHandler handler,
                RefreshTokenCookieService cookieService,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                httpContext.Request.Cookies.TryGetValue(cookieService.CookieName, out var rawRefreshToken);

                var result = await handler.HandleAsync(rawRefreshToken, ct);
                if (result.IsFailure)
                {
                    cookieService.DeleteCookie(httpContext);
                    return result.Error.ToProblemDetails();
                }

                cookieService.AppendCookie(
                    httpContext,
                    result.Value!.RawRefreshToken,
                    result.Value.RefreshTokenExpiresAtUtc);

                return Results.Ok(result.Value.Response);
            })
            .WithTags("Auth")
            .AllowAnonymous()
            .Produces<RefreshResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
