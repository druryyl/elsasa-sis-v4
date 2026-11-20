using System.Net;
using System.Net.Http.Json;
using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Acd.Features.Scorebook;
using ElsasaSis.Modules.Acd.Features.Scorebook.CreateClassroomAssignment;
using ElsasaSis.Modules.Acd.Features.Scorebook.EnterStudentScores;
using ElsasaSis.Modules.Acd.Features.Scorebook.GetSubjectGradebookSummary;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests.Acd;

public sealed class ScorebookIntegrationTests : IntegrationTestBase
{
    public ScorebookIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedStage1FixturesAsync();
        await SeedStage2FixturesAsync();
    }

    [Fact]
    public async Task GetSubjectGradebookSummary_WhenSeededDataExists_ReturnsAssignmentsRosterAndComputedMetrics()
    {
        var context = await GetScorebookFixtureContextAsync();

        using var client = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        var url = $"/api/acd/gradebook?subjectStageId={context.SubjectStageId}&studyGroupId={context.StudyGroupId}&operationalPeriodId={context.OperationalPeriodId}";

        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var gradebook = await response.Content.ReadFromJsonAsync<GradebookSummaryResponse>(JsonOptions);
        Assert.NotNull(gradebook);
        Assert.Equal(context.SubjectStageId, gradebook!.SubjectStageId);
        Assert.Equal(context.StudyGroupId, gradebook.StudyGroupId);
        Assert.Equal(context.OperationalPeriodId, gradebook.OperationalPeriodId);

        // Seeded 2 assignments: Kuis Aljabar 1 (10% weight) and Ulangan Harian 1 (25% weight)
        Assert.True(gradebook.Assignments.Count >= 2);
        Assert.Contains(gradebook.Assignments, a => a.Title == "Kuis Aljabar 1");
        Assert.Contains(gradebook.Assignments, a => a.Title == "Ulangan Harian 1");

        // Seeded 2 students in 7A: Ahmad Dahlan and Siti Nurhaliza
        Assert.Equal(2, gradebook.Students.Count);

        var ahmad = gradebook.Students.First(s => s.StudentName == "Ahmad Dahlan");
        Assert.NotNull(ahmad.RunningScore);
        // Formula: (85 * 10 + 88 * 25) / (10 + 25) = (850 + 2200) / 35 = 3050 / 35 = 87.14
        Assert.Equal(87.14m, ahmad.RunningScore!.Value);
        Assert.Equal(2, ahmad.Scores.Count);
        Assert.Equal(1, ahmad.Attendance.PresentCount);

        var siti = gradebook.Students.First(s => s.StudentName == "Siti Nurhaliza");
        Assert.NotNull(siti.RunningScore);
        // Formula: (92.5 * 10 + 95 * 25) / 35 = (925 + 2375) / 35 = 3300 / 35 = 94.29
        Assert.Equal(94.29m, siti.RunningScore!.Value);
        Assert.Equal(2, siti.Scores.Count);
        Assert.Equal(1, siti.Attendance.PresentCount);
    }

    [Fact]
    public async Task CreateClassroomAssignment_And_EnterScores_EndToEndLifecycle_Succeeds()
    {
        var context = await GetScorebookFixtureContextAsync();

        using var client = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        // 1. Create Assignment
        var createRequest = new CreateAssignmentRequest(
            context.SubjectStageId,
            context.SubjectId,
            context.StudyGroupId,
            context.OperationalPeriodId,
            null,
            "Tugas Mandiri 1",
            "FORMATIVE",
            100.00m,
            15.00m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));

        var createResponse = await client.PostAsJsonAsync("/api/acd/assignments", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdAssignment = await createResponse.Content.ReadFromJsonAsync<AssignmentDto>(JsonOptions);
        Assert.NotNull(createdAssignment);
        Assert.Equal("Tugas Mandiri 1", createdAssignment!.Title);
        Assert.Equal(100.00m, createdAssignment.MaxScore);
        Assert.Equal(15.00m, createdAssignment.Weight);
        Assert.Equal(context.TeacherId, createdAssignment.TeacherId);

        // 2. Enter Student Scores
        var enterRequest = new EnterStudentScoresRequest([
            new ScoreEntryItemDto(context.Student1Id, 95.00m, "Sangat rapi"),
            new ScoreEntryItemDto(context.Student2Id, 90.00m, "Bagus")
        ]);

        var scoreResponse = await client.PostAsJsonAsync($"/api/acd/assignments/{createdAssignment.Id}/scores", enterRequest);
        Assert.Equal(HttpStatusCode.OK, scoreResponse.StatusCode);

        var scoreResult = await scoreResponse.Content.ReadFromJsonAsync<EnterStudentScoresResponse>(JsonOptions);
        Assert.NotNull(scoreResult);
        Assert.Equal(createdAssignment.Id, scoreResult!.AssignmentId);
        Assert.Equal(2, scoreResult.ScoresUpdatedCount);

        // 3. Verify in Consolidated Gradebook
        var gradebookUrl = $"/api/acd/gradebook?subjectStageId={context.SubjectStageId}&studyGroupId={context.StudyGroupId}&operationalPeriodId={context.OperationalPeriodId}";
        var gradebookResponse = await client.GetAsync(gradebookUrl);
        Assert.Equal(HttpStatusCode.OK, gradebookResponse.StatusCode);

        var gradebook = await gradebookResponse.Content.ReadFromJsonAsync<GradebookSummaryResponse>(JsonOptions);
        Assert.NotNull(gradebook);

        var ahmad = gradebook!.Students.First(s => s.StudentId == context.Student1Id);
        // Ahmad now has 3 assignments:
        // Kuis Aljabar 1: 85 * 10 = 850
        // Ulangan Harian 1: 88 * 25 = 2200
        // Tugas Mandiri 1: 95 * 15 = 1425
        // Total: (850 + 2200 + 1425) / (10 + 25 + 15) = 4475 / 50 = 89.50
        Assert.Equal(89.50m, ahmad.RunningScore);
        Assert.Equal(3, ahmad.Scores.Count);

        var newScore = ahmad.Scores.First(s => s.AssignmentId == createdAssignment.Id);
        Assert.Equal(95.00m, newScore.Score);
        Assert.Equal("Sangat rapi", newScore.Feedback);
    }

    private async Task<ScorebookFixtureContext> GetScorebookFixtureContextAsync()
    {
        await using var connection = new SqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();

        var stageId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [acd].[subject_stages] WHERE [name] = N'Matematika Tingkat 7' OR [name] = N'Mathematics Stage 7'");

        var subjectId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [subject_id] FROM [acd].[subject_stages] WHERE [id] = @Id;",
            new { Id = stageId });

        var studyGroupId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[study_groups] WHERE [name] = '7A'");

        var periodId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [opr].[operational_periods] WHERE [status] = 'ACTIVE' ORDER BY [start_date] ASC");

        var teacherId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [teacher_id] FROM [tch].[teacher_assignments] WHERE [study_group_id] = @GroupId AND [status] = 'ACTIVE';",
            new { GroupId = studyGroupId });

        var students = (await connection.QueryAsync<Guid>(
            "SELECT [student_id] FROM [acd].[academic_participations] WHERE [subject_stage_id] = @StageId AND [study_group_id] = @GroupId ORDER BY [student_name];",
            new { StageId = stageId, GroupId = studyGroupId })).ToList();

        return new ScorebookFixtureContext(
            stageId,
            subjectId,
            studyGroupId,
            periodId,
            teacherId,
            students[0],
            students[1]);
    }

    private sealed record ScorebookFixtureContext(
        Guid SubjectStageId,
        Guid SubjectId,
        Guid StudyGroupId,
        Guid OperationalPeriodId,
        Guid TeacherId,
        Guid Student1Id,
        Guid Student2Id);
}
