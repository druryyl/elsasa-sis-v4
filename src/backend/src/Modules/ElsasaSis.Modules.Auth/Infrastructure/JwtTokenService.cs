using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ElsasaSis.Modules.Auth.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ElsasaSis.Modules.Auth.Infrastructure;

public interface IJwtTokenService
{
    AccessTokenResult GenerateAccessToken(AuthUser user, IReadOnlyList<string> permissions);
}

public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public AccessTokenResult GenerateAccessToken(AuthUser user, IReadOnlyList<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(permissions);

        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be configured and at least 32 characters long.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_options.AccessTokenExpirationMinutes);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            ["preferred_username"] = user.Username,
            ["name"] = user.DisplayName,
            ["roles"] = user.Roles.ToArray(),
            ["permissions"] = permissions.ToArray()
        };

        if (user.Actors.TeacherId.HasValue)
        {
            claims["teacherId"] = user.Actors.TeacherId.Value.ToString();
        }

        if (user.Actors.StudentId.HasValue)
        {
            claims["studentId"] = user.Actors.StudentId.Value.ToString();
        }

        if (user.Actors.GuardianId.HasValue)
        {
            claims["guardianId"] = user.Actors.GuardianId.Value.ToString();
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Claims = claims,
            NotBefore = now,
            IssuedAt = now,
            Expires = expiresAt,
            SigningCredentials = credentials
        };

        var token = _tokenHandler.CreateToken(descriptor);
        return new AccessTokenResult(_tokenHandler.WriteToken(token), expiresAt);
    }
}
