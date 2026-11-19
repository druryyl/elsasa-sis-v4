using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Auth.Domain;

public sealed class AuthUser : Entity<Guid>
{
    private AuthUser(
        Guid id,
        string username,
        string passwordHash,
        string? email,
        string displayName,
        bool isActive,
        IReadOnlyList<string> roles,
        UserActors actors,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        Username = username;
        PasswordHash = passwordHash;
        Email = email;
        DisplayName = displayName;
        IsActive = isActive;
        Roles = roles;
        Actors = actors;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public string Username { get; }
    public string PasswordHash { get; }
    public string? Email { get; }
    public string DisplayName { get; }
    public bool IsActive { get; }
    public IReadOnlyList<string> Roles { get; }
    public UserActors Actors { get; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; }

    public static AuthUser Rehydrate(
        Guid id,
        string username,
        string passwordHash,
        string? email,
        string displayName,
        bool isActive,
        IReadOnlyList<string> roles,
        UserActors actors,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new AuthUser(
            id,
            username,
            passwordHash,
            email,
            displayName,
            isActive,
            roles,
            actors,
            createdAt,
            updatedAt);
    }
}
