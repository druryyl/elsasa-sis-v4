using System.Security.Cryptography;
using System.Text;

namespace ElsasaSis.Modules.Auth.Infrastructure;

public interface IRefreshTokenService
{
    string GenerateRawToken();
    string HashToken(string rawToken);
}

public sealed class RefreshTokenService : IRefreshTokenService
{
    public string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string HashToken(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
