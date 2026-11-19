using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.Database;

/// <summary>
/// Idempotently seeds the initial SCHOOL_ADMIN ("admin") account.
/// </summary>
public static class AdminSeeder
{
    public const string DefaultAdminUsername = "admin";
    public const string DefaultAdminRole = "SCHOOL_ADMIN";
    public const string DevelopmentDefaultPassword = "Admin123!";

    public static async Task SeedAsync(
        string connectionString,
        string? adminPassword,
        bool isDevelopment,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        Guid? existingUserId = null;
        await using (var findCmd = connection.CreateCommand())
        {
            findCmd.Transaction = transaction;
            findCmd.CommandText = "SELECT [id] FROM [auth].[users] WHERE [username] = @Username;";
            findCmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.VarChar, 50) { Value = DefaultAdminUsername });

            var scalar = await findCmd.ExecuteScalarAsync(cancellationToken);
            if (scalar is Guid id)
            {
                existingUserId = id;
            }
        }

        if (existingUserId.HasValue)
        {
            // Do NOT overwrite existing credentials on re-run; only ensure role mapping exists.
            await EnsureAdminRoleAsync(connection, transaction, existingUserId.Value, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var resolvedPassword = ResolveInitialPassword(adminPassword, isDevelopment);
        var passwordHash = new PasswordHasher<object>().HashPassword(new object(), resolvedPassword);
        var userId = DbUuidV7.NewGuid();
        var utcNow = DateTime.UtcNow;

        await using (var insertUserCmd = connection.CreateCommand())
        {
            insertUserCmd.Transaction = transaction;
            insertUserCmd.CommandText = """
                INSERT INTO [auth].[users] ([id], [username], [password_hash], [email], [display_name], [is_active], [created_at], [updated_at])
                VALUES (@Id, @Username, @PasswordHash, @Email, @DisplayName, @IsActive, @CreatedAt, @UpdatedAt);
                """;
            insertUserCmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = userId });
            insertUserCmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.VarChar, 50) { Value = DefaultAdminUsername });
            insertUserCmd.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.VarChar, 255) { Value = passwordHash });
            insertUserCmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.VarChar, 100) { Value = DBNull.Value });
            insertUserCmd.Parameters.Add(new SqlParameter("@DisplayName", SqlDbType.NVarChar, 100) { Value = "System Administrator" });
            insertUserCmd.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = true });
            insertUserCmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });
            insertUserCmd.Parameters.Add(new SqlParameter("@UpdatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });

            await insertUserCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await EnsureAdminRoleAsync(connection, transaction, userId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static string ResolveInitialPassword(string? adminPassword, bool isDevelopment)
    {
        if (!string.IsNullOrWhiteSpace(adminPassword))
        {
            return adminPassword;
        }

        if (isDevelopment)
        {
            return DevelopmentDefaultPassword;
        }

        throw new InvalidOperationException(
            "Seed:AdminPassword must be configured in non-Development environments when the initial 'admin' user does not yet exist.");
    }

    private static async Task EnsureAdminRoleAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        await using var roleCmd = connection.CreateCommand();
        roleCmd.Transaction = transaction;
        roleCmd.CommandText = """
            IF NOT EXISTS (SELECT 1 FROM [auth].[user_roles] WHERE [user_id] = @UserId AND [role_code] = @RoleCode)
            BEGIN
                INSERT INTO [auth].[user_roles] ([user_id], [role_code], [created_at], [updated_at])
                VALUES (@UserId, @RoleCode, @CreatedAt, @UpdatedAt);
            END
            """;
        roleCmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.UniqueIdentifier) { Value = userId });
        roleCmd.Parameters.Add(new SqlParameter("@RoleCode", SqlDbType.VarChar, 30) { Value = DefaultAdminRole });
        roleCmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });
        roleCmd.Parameters.Add(new SqlParameter("@UpdatedAt", SqlDbType.DateTime2, 3) { Value = utcNow });

        await roleCmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
