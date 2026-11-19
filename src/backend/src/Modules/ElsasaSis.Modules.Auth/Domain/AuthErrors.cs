using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Auth.Domain;

public static class AuthErrors
{
    public static readonly DomainError InvalidCredentials = DomainError.Unauthorized(
        "Auth.InvalidCredentials",
        "Invalid username or password.");

    public static readonly DomainError InactiveUser = DomainError.Forbidden(
        "Auth.InactiveUser",
        "User account is inactive.");

    public static readonly DomainError InvalidRefreshToken = DomainError.Unauthorized(
        "Auth.InvalidRefreshToken",
        "Refresh token is missing, invalid, or expired.");

    public static readonly DomainError Unauthorized = DomainError.Unauthorized(
        "Auth.Unauthorized",
        "Authentication is required.");
}
