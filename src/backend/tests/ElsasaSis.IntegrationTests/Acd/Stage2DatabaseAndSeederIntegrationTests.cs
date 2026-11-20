using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests.Acd;

public sealed class Stage2DatabaseAndSeederIntegrationTests : IntegrationTestBase
{
    public Stage2DatabaseAndSeederIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task SeedStage2FixturesAsync_IsIdempotent_AndPopulatesAllStage2Data()
    {
        // First execution
        await SeedStage2FixturesAsync();

        // Second execution (assert idempotency)
        await SeedStage2FixturesAsync();

        await using var connection = new SqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();

        // Verify 11 new tables exist in sys.tables
        var tableNames = (await connection.QueryAsync<string>("""
            SELECT s.name + '.' + t.name
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name IN ('opr', 'acd')
            """)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("opr.study_group_schedules", tableNames);
        Assert.Contains("opr.study_group_timetables", tableNames);
        Assert.Contains("acd.subject_stages", tableNames);
        Assert.Contains("acd.learning_units", tableNames);
        Assert.Contains("acd.subject_stage_grades", tableNames);
        Assert.Contains("acd.academic_participations", tableNames);
        Assert.Contains("acd.attendance_records", tableNames);
        Assert.Contains("acd.assignments", tableNames);
        Assert.Contains("acd.assignment_scores", tableNames);
        Assert.Contains("acd.report_cards", tableNames);
        Assert.Contains("acd.report_card_entries", tableNames);

        // Verify Room Classroom 7A exists
        var roomCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [opr].[rooms] WHERE [name] = @Name",
            new { Name = Stage2Seeder.DefaultClassroomName });
        Assert.True(roomCount >= 1);

        // Verify Subject Stages
        var mathStageCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [acd].[subject_stages] WHERE [id] = @Id AND [name] = @Name",
            new { Id = Stage2Seeder.DefaultMathStageId, Name = Stage2Seeder.DefaultMathStageName });
        Assert.Equal(1, mathStageCount);

        var englishStageCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [acd].[subject_stages] WHERE [id] = @Id AND [name] = @Name",
            new { Id = Stage2Seeder.DefaultEnglishStageId, Name = Stage2Seeder.DefaultEnglishStageName });
        Assert.Equal(1, englishStageCount);

        // Verify Learning Units
        var luCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [acd].[learning_units] WHERE [subject_stage_id] = @MathStageId",
            new { MathStageId = Stage2Seeder.DefaultMathStageId });
        Assert.True(luCount >= 2);

        // Verify Timetable Slots for 7A
        var timetableSlots = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [opr].[study_group_schedules] WHERE [study_group_name] = '7A' AND [day_of_week] = 1",
            null);
        Assert.True(timetableSlots >= 2);

        // Verify Placed Students & Academic Participations
        var apCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [acd].[academic_participations] WHERE [study_group_name] = '7A' AND [status] = 'ACTIVE'",
            null);
        Assert.True(apCount >= 2);

        // Verify Attendance Records
        var attCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [acd].[attendance_records] WHERE [study_group_name] = '7A'",
            null);
        Assert.True(attCount >= 2);

        // Verify Assignments & Scores
        var formativeAssignment = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [acd].[assignments] WHERE [title] = @Title",
            new { Title = Stage2Seeder.DefaultFormativeAssignmentTitle });
        Assert.Equal(1, formativeAssignment);

        var formativeScores = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [acd].[assignment_scores] WHERE [assignment_id] = @Id",
            new { Id = Stage2Seeder.DefaultFormativeAssignmentId });
        Assert.True(formativeScores >= 2);

        var summativeAssignment = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [acd].[assignments] WHERE [title] = @Title",
            new { Title = Stage2Seeder.DefaultSummativeAssignmentTitle });
        Assert.Equal(1, summativeAssignment);

        var summativeScores = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [acd].[assignment_scores] WHERE [assignment_id] = @Id",
            new { Id = Stage2Seeder.DefaultSummativeAssignmentId });
        Assert.True(summativeScores >= 2);
    }
}
