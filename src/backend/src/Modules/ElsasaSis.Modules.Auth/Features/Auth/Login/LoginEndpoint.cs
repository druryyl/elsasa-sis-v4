using ElsasaSis.Modules.Auth.Infrastructure;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Auth.Features.Auth.Login;

public sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
                LoginRequest request,
                LoginHandler handler,
                RefreshTokenCookieService cookieService,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                if (result.IsFailure)
                {
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
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
