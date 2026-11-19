using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Auth.Domain;

public sealed class RefreshToken : Entity<Guid>
{
    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime? revokedAt,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        RevokedAt = revokedAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid UserId { get; }
    public string TokenHash { get; }
    public DateTime ExpiresAt { get; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsExpired(DateTime utcNow) => ExpiresAt <= utcNow;

    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return new RefreshToken(
            UuidV7.NewGuid(),
            userId,
            tokenHash,
            expiresAt,
            null,
            utcNow,
            utcNow);
    }

    public static RefreshToken Rehydrate(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime? revokedAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new RefreshToken(
            id,
            userId,
            tokenHash,
            expiresAt,
            revokedAt,
            createdAt,
            updatedAt);
    }

    public void Revoke(DateTime utcNow)
    {
        if (RevokedAt.HasValue)
        {
            return;
        }

        RevokedAt = utcNow;
        UpdatedAt = utcNow;
    }
}
