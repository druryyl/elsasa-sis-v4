namespace ElsasaSis.Modules.Auth.Domain;

public interface IAuthRepository
{
    Task<AuthUser?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<AuthUser?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task SaveRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task RotateRefreshTokenAsync(RefreshToken revokedToken, RefreshToken newToken, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenByHashAsync(string tokenHash, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
}
