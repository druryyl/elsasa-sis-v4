using ElsasaSis.Modules.Auth.Infrastructure;

namespace ElsasaSis.UnitTests.Auth;

public sealed class RefreshTokenServiceTests
{
    private readonly RefreshTokenService _service = new();

    [Fact]
    public void GenerateRawToken_ProducesUniqueUrlSafeTokens()
    {
        var token1 = _service.GenerateRawToken();
        var token2 = _service.GenerateRawToken();

        Assert.False(string.IsNullOrWhiteSpace(token1));
        Assert.NotEqual(token1, token2);
        Assert.DoesNotContain('+', token1);
        Assert.DoesNotContain('/', token1);
        Assert.DoesNotContain('=', token1);
    }

    [Fact]
    public void HashToken_IsDeterministicSha256Hex()
    {
        const string rawToken = "sample-opaque-refresh-token";

        var hash1 = _service.HashToken(rawToken);
        var hash2 = _service.HashToken(rawToken);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash1);
        Assert.NotEqual(rawToken, hash1);
    }

    [Fact]
    public void HashToken_DifferentInputsProduceDifferentHashes()
    {
        var hash1 = _service.HashToken("token-a");
        var hash2 = _service.HashToken("token-b");

        Assert.NotEqual(hash1, hash2);
    }
}
