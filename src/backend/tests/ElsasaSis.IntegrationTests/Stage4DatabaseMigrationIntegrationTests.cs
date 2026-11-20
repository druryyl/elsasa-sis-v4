using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests;

public sealed class Stage4DatabaseMigrationIntegrationTests : IntegrationTestBase
{
    public Stage4DatabaseMigrationIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task DbUpScripts_004And102_ExecuteCleanlyAndIdempotently()
    {
        // First run
        await SeedStage4FixturesAsync();

        // Second run to assert clean idempotency
        await SeedStage4FixturesAsync();

        await using var connection = new SqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();

        // 1. Verify auth.user_students table existence
        var tableCount = await connection.ExecuteScalarAsync<int>("""
            SELECT COUNT(1)
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name = 'auth' AND t.name = 'user_students';
            """);
        Assert.Equal(1, tableCount);

        // 2. Verify columns (user_id, student_id, created_at)
        var columns = (await connection.QueryAsync<string>("""
            SELECT c.name
            FROM sys.columns c
            WHERE c.object_id = OBJECT_ID(N'[auth].[user_students]');
            """)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("user_id", columns);
        Assert.Contains("student_id", columns);
        Assert.Contains("created_at", columns);

        // 3. Verify clustered primary key (user_id, student_id)
        var pkName = await connection.ExecuteScalarAsync<string>("""
            SELECT kc.name
            FROM sys.key_constraints kc
            WHERE kc.parent_object_id = OBJECT_ID(N'[auth].[user_students]')
              AND kc.type = 'PK';
            """);
        Assert.Equal("PK_auth_user_students", pkName);

        // 4. Verify non-clustered index on student_id
        var indexCount = await connection.ExecuteScalarAsync<int>("""
            SELECT COUNT(1)
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'[auth].[user_students]')
              AND name = 'IX_auth_user_students_student_id';
            """);
        Assert.Equal(1, indexCount);

        // 5. Verify Parent account (parent.hendra) and role PARENT
        var parentUserId = await connection.ExecuteScalarAsync<Guid?>("""
            SELECT id FROM auth.users WHERE username = @Username;
            """, new { Username = Stage4Seeder.DefaultParentUsername });
        Assert.NotNull(parentUserId);

        var parentRoleCount = await connection.ExecuteScalarAsync<int>("""
            SELECT COUNT(1) FROM auth.user_roles WHERE user_id = @UserId AND role_code = 'PARENT';
            """, new { UserId = parentUserId.Value });
        Assert.Equal(1, parentRoleCount);

        // 6. Verify linked children (Ahmad & Siti)
        var linkedStudents = (await connection.QueryAsync<Guid>("""
            SELECT student_id FROM auth.user_students WHERE user_id = @UserId ORDER BY student_id;
            """, new { UserId = parentUserId.Value })).ToList();

        Assert.Equal(2, linkedStudents.Count);
        Assert.Contains(Stage4Seeder.DefaultStudentAhmadId, linkedStudents);
        Assert.Contains(Stage4Seeder.DefaultStudentSitiId, linkedStudents);

        // 7. Verify Student accounts and user_actors linkages
        var ahmadUserId = await connection.ExecuteScalarAsync<Guid?>("""
            SELECT id FROM auth.users WHERE username = @Username;
            """, new { Username = Stage4Seeder.DefaultStudentAhmadUsername });
        Assert.NotNull(ahmadUserId);

        var ahmadActorCount = await connection.ExecuteScalarAsync<int>("""
            SELECT COUNT(1) FROM auth.user_actors WHERE user_id = @UserId AND student_id = @StudentId;
            """, new { UserId = ahmadUserId.Value, StudentId = Stage4Seeder.DefaultStudentAhmadId });
        Assert.Equal(1, ahmadActorCount);

        var sitiUserId = await connection.ExecuteScalarAsync<Guid?>("""
            SELECT id FROM auth.users WHERE username = @Username;
            """, new { Username = Stage4Seeder.DefaultStudentSitiUsername });
        Assert.NotNull(sitiUserId);

        var sitiActorCount = await connection.ExecuteScalarAsync<int>("""
            SELECT COUNT(1) FROM auth.user_actors WHERE user_id = @UserId AND student_id = @StudentId;
            """, new { UserId = sitiUserId.Value, StudentId = Stage4Seeder.DefaultStudentSitiId });
        Assert.Equal(1, sitiActorCount);
    }
}
