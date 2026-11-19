namespace ElsasaSis.Modules.Auth.Features.Auth.Refresh;

public sealed record RefreshResponse(
    string AccessToken,
    DateTime ExpiresAt,
    AuthUserDto User);
