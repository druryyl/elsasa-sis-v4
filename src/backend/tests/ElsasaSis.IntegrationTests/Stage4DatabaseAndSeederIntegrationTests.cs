using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests;

public sealed class Stage4DatabaseAndSeederIntegrationTests : IntegrationTestBase
{
    public Stage4DatabaseAndSeederIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task MigrationAndSeedStage4FixturesAsync_IsIdempotent_AndPopulatesAllStage4Data()
    {
        // First execution
        await SeedStage4FixturesAsync();

        // Second execution (assert idempotency)
        await SeedStage4FixturesAsync();

        await using var connection = new SqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();

        // 1. Verify auth.user_students exists in sys.tables
        var tableExists = await connection.ExecuteScalarAsync<int>("""
            SELECT COUNT(1)
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name = 'auth' AND t.name = 'user_students';
            """);
        Assert.Equal(1, tableExists);

        // 2. Verify columns
        var columns = (await connection.QueryAsync<string>("""
            SELECT c.name
            FROM sys.columns c
            WHERE c.object_id = OBJECT_ID(N'[auth].[user_students]');
            """)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("user_id", columns);
        Assert.Contains("student_id", columns);
        Assert.Contains("created_at", columns);

        // 3. Verify primary key
        var pkName = await connection.ExecuteScalarAsync<string>("""
            SELECT kc.name
            FROM sys.key_constraints kc
            WHERE kc.parent_object_id = OBJECT_ID(N'[auth].[user_students]')
              AND kc.type = 'PK';
            """);
        Assert.Equal("PK_auth_user_students", pkName);

        // 4. Verify non-clustered index on student_id
        var indexExists = await connection.ExecuteScalarAsync<int>("""
            SELECT COUNT(1)
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'[auth].[user_students]')
              AND name = 'IX_auth_user_students_student_id';
            """);
        Assert.Equal(1, indexExists);

        // 5. Verify Parent user and role
        var parentUser = await connection.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT u.[id], u.[username], u.[email], u.[display_name], u.[is_active]
            FROM [auth].[users] u
            WHERE u.[username] = @Username;
            """, new { Username = Stage4Seeder.DefaultParentUsername });

        Assert.NotNull(parentUser);
        Assert.Equal(Stage4Seeder.DefaultParentDisplayName, (string)parentUser!.display_name);
        Assert.Equal(Stage4Seeder.DefaultParentEmail, (string)parentUser.email);
        Assert.True((bool)parentUser.is_active);

        var parentRole = await connection.ExecuteScalarAsync<int>("""
            SELECT COUNT(1)
            FROM [auth].[user_roles]
            WHERE [user_id] = @UserId AND [role_code] = 'PARENT';
            """, new { UserId = (Guid)parentUser.id });
        Assert.Equal(1, parentRole);

        // 6. Verify parent.hendra user_students child linkages (Ahmad and Siti)
        var childIds = (await connection.QueryAsync<Guid>("""
            SELECT [student_id]
            FROM [auth].[user_students]
            WHERE [user_id] = @UserId;
            """, new { UserId = (Guid)parentUser.id })).ToList();

        Assert.Equal(2, childIds.Count);
        Assert.Contains(Stage4Seeder.DefaultStudentAhmadId, childIds);
        Assert.Contains(Stage4Seeder.DefaultStudentSitiId, childIds);

        // 7. Verify Student Ahmad user, role, and actor linkage
        var ahmadUser = await connection.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT u.[id], u.[username], u.[email], u.[display_name], u.[is_active]
            FROM [auth].[users] u
            WHERE u.[username] = @Username;
            """, new { Username = Stage4Seeder.DefaultStudentAhmadUsername });

        Assert.NotNull(ahmadUser);
        Assert.Equal(Stage4Seeder.DefaultStudentAhmadDisplayName, (string)ahmadUser!.display_name);
        Assert.Equal(Stage4Seeder.DefaultStudentAhmadEmail, (string)ahmadUser.email);
        Assert.True((bool)ahmadUser.is_active);

        var ahmadRole = await connection.ExecuteScalarAsync<int>("""
            SELECT COUNT(1)
            FROM [auth].[user_roles]
            WHERE [user_id] = @UserId AND [role_code] = 'STUDENT';
            """, new { UserId = (Guid)ahmadUser.id });
        Assert.Equal(1, ahmadRole);

        var ahmadActorStudentId = await connection.ExecuteScalarAsync<Guid>("""
            SELECT [student_id]
            FROM [auth].[user_actors]
            WHERE [user_id] = @UserId;
            """, new { UserId = (Guid)ahmadUser.id });
        Assert.Equal(Stage4Seeder.DefaultStudentAhmadId, ahmadActorStudentId);

        // 8. Verify Student Siti user, role, and actor linkage
        var sitiUser = await connection.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT u.[id], u.[username], u.[email], u.[display_name], u.[is_active]
            FROM [auth].[users] u
            WHERE u.[username] = @Username;
            """, new { Username = Stage4Seeder.DefaultStudentSitiUsername });

        Assert.NotNull(sitiUser);
        Assert.Equal(Stage4Seeder.DefaultStudentSitiDisplayName, (string)sitiUser!.display_name);
        Assert.Equal(Stage4Seeder.DefaultStudentSitiEmail, (string)sitiUser.email);
        Assert.True((bool)sitiUser.is_active);

        var sitiRole = await connection.ExecuteScalarAsync<int>("""
            SELECT COUNT(1)
            FROM [auth].[user_roles]
            WHERE [user_id] = @UserId AND [role_code] = 'STUDENT';
            """, new { UserId = (Guid)sitiUser.id });
        Assert.Equal(1, sitiRole);

        var sitiActorStudentId = await connection.ExecuteScalarAsync<Guid>("""
            SELECT [student_id]
            FROM [auth].[user_actors]
            WHERE [user_id] = @UserId;
            """, new { UserId = (Guid)sitiUser.id });
        Assert.Equal(Stage4Seeder.DefaultStudentSitiId, sitiActorStudentId);

        // 9. Verify authentications succeed with seeded passwords
        var parentClient = await CreateAuthenticatedClientAsync(
            Stage4Seeder.DefaultParentUsername,
            Stage4Seeder.DefaultParentPassword);
        Assert.NotNull(parentClient);

        var studentClient = await CreateAuthenticatedClientAsync(
            Stage4Seeder.DefaultStudentAhmadUsername,
            Stage4Seeder.DefaultStudentAhmadPassword);
        Assert.NotNull(studentClient);
    }
}
