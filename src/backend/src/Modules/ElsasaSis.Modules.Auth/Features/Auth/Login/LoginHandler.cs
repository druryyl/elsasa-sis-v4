using ElsasaSis.Modules.Auth.Domain;
using ElsasaSis.Modules.Auth.Infrastructure;
using ElsasaSis.SharedKernel.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ElsasaSis.Modules.Auth.Features.Auth.Login;

public sealed record LoginResult(
    LoginResponse Response,
    string RawRefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public sealed class LoginHandler
{
    private readonly IAuthRepository _authRepository;
    private readonly PasswordHasher<object> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;

    public LoginHandler(
        IAuthRepository authRepository,
        PasswordHasher<object> passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider)
    {
        _authRepository = authRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
    }

    public async Task<Result<LoginResult>> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.GetUserByUsernameAsync(request.Username.Trim(), cancellationToken);
        if (user is null)
        {
            return Result<LoginResult>.Failure(AuthErrors.InvalidCredentials);
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            null!,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Result<LoginResult>.Failure(AuthErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result<LoginResult>.Failure(AuthErrors.InactiveUser);
        }

        var permissions = RolePermissionCatalog.GetPermissionsForRoles(user.Roles);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, permissions);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var refreshExpiresAt = utcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays);
        var rawRefreshToken = _refreshTokenService.GenerateRawToken();
        var refreshTokenHash = _refreshTokenService.HashToken(rawRefreshToken);

        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenHash,
            refreshExpiresAt,
            utcNow);

        await _authRepository.SaveRefreshTokenAsync(refreshToken, cancellationToken);

        var response = new LoginResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            AuthUserDto.FromDomain(user, permissions));

        return Result<LoginResult>.Success(new LoginResult(response, rawRefreshToken, refreshExpiresAt));
    }
}
