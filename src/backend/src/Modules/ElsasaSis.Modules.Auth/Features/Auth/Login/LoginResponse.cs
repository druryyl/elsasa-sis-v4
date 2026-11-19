namespace ElsasaSis.Modules.Auth.Features.Auth.Login;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    AuthUserDto User);
