using ElsasaSis.Modules.Auth.Domain;

namespace ElsasaSis.UnitTests.Auth;

public sealed class RefreshTokenTests
{
    [Fact]
    public void Create_AssignsUuidV7AndIsActiveUntilExpiryOrRevocation()
    {
        var now = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
        var token = RefreshToken.Create(Guid.NewGuid(), "hash-value", now.AddDays(7), now);

        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.False(token.IsRevoked);
        Assert.False(token.IsExpired(now.AddDays(6)));
        Assert.True(token.IsActive(now.AddDays(6)));
        Assert.True(token.IsExpired(now.AddDays(7)));
        Assert.False(token.IsActive(now.AddDays(7)));

        var revokedAt = now.AddHours(1);
        token.Revoke(revokedAt);

        Assert.True(token.IsRevoked);
        Assert.Equal(revokedAt, token.RevokedAt);
        Assert.False(token.IsActive(now.AddHours(2)));
    }
}
