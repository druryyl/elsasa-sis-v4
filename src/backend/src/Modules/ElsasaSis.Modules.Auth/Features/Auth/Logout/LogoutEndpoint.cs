using ElsasaSis.Modules.Auth.Infrastructure;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Auth.Features.Auth.Logout;

public sealed class LogoutEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/logout", async (
                LogoutHandler handler,
                RefreshTokenCookieService cookieService,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                httpContext.Request.Cookies.TryGetValue(cookieService.CookieName, out var rawRefreshToken);
                await handler.HandleAsync(rawRefreshToken, ct);
                cookieService.DeleteCookie(httpContext);

                return Results.NoContent();
            })
            .WithTags("Auth")
            .AllowAnonymous()
            .Produces(StatusCodes.Status204NoContent);
    }
}
