namespace ElsasaSis.Modules.Auth.Infrastructure;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "ElsasaSis";
    public string Audience { get; set; } = "ElsasaSis.Client";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public string RefreshCookieName { get; set; } = "elsasa_refresh_token";
    public string RefreshCookieSameSite { get; set; } = "Strict";
    public bool? RefreshCookieSecure { get; set; }
}
