using System.Data;
using Dapper;
using ElsasaSis.Modules.Auth.Domain;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Auth.Infrastructure;

public sealed class AuthRepository : IAuthRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AuthRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AuthUser?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                u.id AS Id,
                u.username AS Username,
                u.password_hash AS PasswordHash,
                u.email AS Email,
                u.display_name AS DisplayName,
                u.is_active AS IsActive,
                u.created_at AS CreatedAt,
                u.updated_at AS UpdatedAt,
                ua.teacher_id AS TeacherId,
                ua.student_id AS StudentId,
                ua.guardian_id AS GuardianId
            FROM auth.users u
            LEFT JOIN auth.user_actors ua ON ua.user_id = u.id
            WHERE u.username = @Username;

            SELECT ur.role_code
            FROM auth.user_roles ur
            INNER JOIN auth.users u ON u.id = ur.user_id
            WHERE u.username = @Username;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { Username = username }, cancellationToken: cancellationToken));

        var userRow = await multi.ReadSingleOrDefaultAsync<UserRow>();
        if (userRow is null)
        {
            return null;
        }

        var roles = (await multi.ReadAsync<string>()).AsList();
        return MapUser(userRow, roles);
    }

    public async Task<AuthUser?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                u.id AS Id,
                u.username AS Username,
                u.password_hash AS PasswordHash,
                u.email AS Email,
                u.display_name AS DisplayName,
                u.is_active AS IsActive,
                u.created_at AS CreatedAt,
                u.updated_at AS UpdatedAt,
                ua.teacher_id AS TeacherId,
                ua.student_id AS StudentId,
                ua.guardian_id AS GuardianId
            FROM auth.users u
            LEFT JOIN auth.user_actors ua ON ua.user_id = u.id
            WHERE u.id = @UserId;

            SELECT ur.role_code
            FROM auth.user_roles ur
            WHERE ur.user_id = @UserId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));

        var userRow = await multi.ReadSingleOrDefaultAsync<UserRow>();
        if (userRow is null)
        {
            return null;
        }

        var roles = (await multi.ReadAsync<string>()).AsList();
        return MapUser(userRow, roles);
    }

    public async Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                token_hash AS TokenHash,
                expires_at AS ExpiresAt,
                revoked_at AS RevokedAt,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM auth.refresh_tokens
            WHERE token_hash = @TokenHash;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<RefreshTokenRow>(
            new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken));

        return row is null
            ? null
            : RefreshToken.Rehydrate(
                row.Id,
                row.UserId,
                row.TokenHash,
                DateTime.SpecifyKind(row.ExpiresAt, DateTimeKind.Utc),
                row.RevokedAt.HasValue ? DateTime.SpecifyKind(row.RevokedAt.Value, DateTimeKind.Utc) : null,
                DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc),
                DateTime.SpecifyKind(row.UpdatedAt, DateTimeKind.Utc));
    }

    public async Task SaveRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO auth.refresh_tokens (id, user_id, token_hash, expires_at, revoked_at, created_at, updated_at)
            VALUES (@Id, @UserId, @TokenHash, @ExpiresAt, @RevokedAt, @CreatedAt, @UpdatedAt);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                refreshToken.Id,
                refreshToken.UserId,
                refreshToken.TokenHash,
                refreshToken.ExpiresAt,
                refreshToken.RevokedAt,
                refreshToken.CreatedAt,
                refreshToken.UpdatedAt
            }, cancellationToken: cancellationToken));
    }

    public async Task RotateRefreshTokenAsync(RefreshToken revokedToken, RefreshToken newToken, CancellationToken cancellationToken = default)
    {
        const string revokeSql = """
            UPDATE auth.refresh_tokens
            SET revoked_at = @RevokedAt,
                updated_at = @UpdatedAt
            WHERE id = @Id;
            """;

        const string insertSql = """
            INSERT INTO auth.refresh_tokens (id, user_id, token_hash, expires_at, revoked_at, created_at, updated_at)
            VALUES (@Id, @UserId, @TokenHash, @ExpiresAt, @RevokedAt, @CreatedAt, @UpdatedAt);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(
            new CommandDefinition(revokeSql, new
            {
                revokedToken.Id,
                revokedToken.RevokedAt,
                revokedToken.UpdatedAt
            }, transaction: transaction, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(insertSql, new
            {
                newToken.Id,
                newToken.UserId,
                newToken.TokenHash,
                newToken.ExpiresAt,
                newToken.RevokedAt,
                newToken.CreatedAt,
                newToken.UpdatedAt
            }, transaction: transaction, cancellationToken: cancellationToken));

        transaction.Commit();
    }

    public async Task RevokeRefreshTokenByHashAsync(string tokenHash, DateTime revokedAtUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE auth.refresh_tokens
            SET revoked_at = @RevokedAt,
                updated_at = @UpdatedAt
            WHERE token_hash = @TokenHash
              AND revoked_at IS NULL;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                TokenHash = tokenHash,
                RevokedAt = revokedAtUtc,
                UpdatedAt = revokedAtUtc
            }, cancellationToken: cancellationToken));
    }

    private static AuthUser MapUser(UserRow row, IReadOnlyList<string> roles)
    {
        return AuthUser.Rehydrate(
            row.Id,
            row.Username,
            row.PasswordHash,
            row.Email,
            row.DisplayName,
            row.IsActive,
            roles,
            new UserActors(row.TeacherId, row.StudentId, row.GuardianId),
            DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(row.UpdatedAt, DateTimeKind.Utc));
    }

    private sealed class UserRow
    {
        public Guid Id { get; init; }
        public string Username { get; init; } = string.Empty;
        public string PasswordHash { get; init; } = string.Empty;
        public string? Email { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public Guid? TeacherId { get; init; }
        public Guid? StudentId { get; init; }
        public Guid? GuardianId { get; init; }
    }

    private sealed class RefreshTokenRow
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string TokenHash { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
        public DateTime? RevokedAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
