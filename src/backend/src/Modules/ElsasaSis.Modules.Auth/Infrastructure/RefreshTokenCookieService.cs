using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ElsasaSis.Modules.Auth.Infrastructure;

public sealed class RefreshTokenCookieService
{
    private readonly JwtOptions _options;

    public RefreshTokenCookieService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string CookieName => _options.RefreshCookieName;

    public void AppendCookie(HttpContext httpContext, string rawToken, DateTime expiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        httpContext.Response.Cookies.Append(
            _options.RefreshCookieName,
            rawToken,
            BuildCookieOptions(httpContext, new DateTimeOffset(expiresAtUtc, TimeSpan.Zero)));
    }

    public void DeleteCookie(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.Cookies.Delete(
            _options.RefreshCookieName,
            BuildCookieOptions(httpContext, expires: null));
    }

    private CookieOptions BuildCookieOptions(HttpContext httpContext, DateTimeOffset? expires)
    {
        var sameSite = Enum.TryParse<SameSiteMode>(_options.RefreshCookieSameSite, ignoreCase: true, out var parsed)
            ? parsed
            : SameSiteMode.Strict;

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = _options.RefreshCookieSecure ?? httpContext.Request.IsHttps,
            SameSite = sameSite,
            Path = "/api/auth",
            Expires = expires
        };
    }
}
