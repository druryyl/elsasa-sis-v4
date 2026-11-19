using ElsasaSis.Modules.Auth.Domain;
using ElsasaSis.Modules.Auth.Infrastructure;
using ElsasaSis.SharedKernel.Common;
using Microsoft.Extensions.Options;

namespace ElsasaSis.Modules.Auth.Features.Auth.Refresh;

public sealed record RefreshResult(
    RefreshResponse Response,
    string RawRefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public sealed class RefreshHandler
{
    private readonly IAuthRepository _authRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;

    public RefreshHandler(
        IAuthRepository authRepository,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider)
    {
        _authRepository = authRepository;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RefreshResult>> HandleAsync(
        string? rawRefreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Result<RefreshResult>.Failure(AuthErrors.InvalidRefreshToken);
        }

        var tokenHash = _refreshTokenService.HashToken(rawRefreshToken);
        var existingToken = await _authRepository.GetRefreshTokenByHashAsync(tokenHash, cancellationToken);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        if (existingToken is null || !existingToken.IsActive(utcNow))
        {
            return Result<RefreshResult>.Failure(AuthErrors.InvalidRefreshToken);
        }

        var user = await _authRepository.GetUserByIdAsync(existingToken.UserId, cancellationToken);
        if (user is null)
        {
            return Result<RefreshResult>.Failure(AuthErrors.InvalidRefreshToken);
        }

        if (!user.IsActive)
        {
            return Result<RefreshResult>.Failure(AuthErrors.InactiveUser);
        }

        var permissions = RolePermissionCatalog.GetPermissionsForRoles(user.Roles);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, permissions);

        var newRefreshExpiresAt = utcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays);
        var newRawRefreshToken = _refreshTokenService.GenerateRawToken();
        var newRefreshTokenHash = _refreshTokenService.HashToken(newRawRefreshToken);

        existingToken.Revoke(utcNow);
        var newRefreshToken = RefreshToken.Create(
            user.Id,
            newRefreshTokenHash,
            newRefreshExpiresAt,
            utcNow);

        await _authRepository.RotateRefreshTokenAsync(existingToken, newRefreshToken, cancellationToken);

        var response = new RefreshResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            AuthUserDto.FromDomain(user, permissions));

        return Result<RefreshResult>.Success(new RefreshResult(response, newRawRefreshToken, newRefreshExpiresAt));
    }
}
