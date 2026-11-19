using ElsasaSis.Modules.Auth.Domain;
using ElsasaSis.Modules.Auth.Infrastructure;

namespace ElsasaSis.Modules.Auth.Features.Auth.Logout;

public sealed class LogoutHandler
{
    private readonly IAuthRepository _authRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly TimeProvider _timeProvider;

    public LogoutHandler(
        IAuthRepository authRepository,
        IRefreshTokenService refreshTokenService,
        TimeProvider timeProvider)
    {
        _authRepository = authRepository;
        _refreshTokenService = refreshTokenService;
        _timeProvider = timeProvider;
    }

    public async Task HandleAsync(string? rawRefreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return;
        }

        var tokenHash = _refreshTokenService.HashToken(rawRefreshToken);
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        await _authRepository.RevokeRefreshTokenByHashAsync(tokenHash, utcNow, cancellationToken);
    }
}
