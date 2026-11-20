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

public sealed class Stage2ScorebookIntegrationTests : IntegrationTestBase
{
    public Stage2ScorebookIntegrationTests(IntegrationTestWebApplicationFactory factory)
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
    public async Task Scorebook_FullLifecycle_Assignments_TeacherAuthorization_Scores_And_GradebookSummary_Succeed()
    {
        var context = await GetScorebookFixtureContextAsync();

        using var teacherClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Verify unassigned teacher cannot create assignments for a study group they are not assigned to (7B) -> 403 Forbidden
        var unassignedCreateReq = new CreateAssignmentRequest(
            context.SubjectStageId,
            context.SubjectId,
            context.StudyGroupId7B, // Teacher Budi has no teaching assignment for 7B
            context.OperationalPeriodId,
            null,
            "Tugas Mandiri 7B",
            "FORMATIVE",
            100.00m,
            10.00m,
            today,
            today.AddDays(7));

        var forbiddenResp = await teacherClient.PostAsJsonAsync("/api/acd/assignments", unassignedCreateReq);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResp.StatusCode);

        // 2. Create formative and summative assignments for study group 7A (assigned to teacher Budi) -> 201 Created
        var formativeReq = new CreateAssignmentRequest(
            context.SubjectStageId,
            context.SubjectId,
            context.StudyGroupId7A,
            context.OperationalPeriodId,
            null,
            "Kuis Formatif 2",
            "FORMATIVE",
            100.00m,
            15.00m,
            today,
            today.AddDays(7));

        var formativeResp = await teacherClient.PostAsJsonAsync("/api/acd/assignments", formativeReq);
        Assert.Equal(HttpStatusCode.Created, formativeResp.StatusCode);

        var createdFormative = await formativeResp.Content.ReadFromJsonAsync<AssignmentDto>(JsonOptions);
        Assert.NotNull(createdFormative);
        Assert.Equal("Kuis Formatif 2", createdFormative!.Title);
        Assert.Equal("FORMATIVE", createdFormative.AssignmentType);
        Assert.Equal(15.00m, createdFormative.Weight);

        var summativeReq = new CreateAssignmentRequest(
            context.SubjectStageId,
            context.SubjectId,
            context.StudyGroupId7A,
            context.OperationalPeriodId,
            null,
            "Ulangan Sumatif 2",
            "SUMMATIVE",
            100.00m,
            20.00m,
            today,
            today.AddDays(14));

        var summativeResp = await teacherClient.PostAsJsonAsync("/api/acd/assignments", summativeReq);
        Assert.Equal(HttpStatusCode.Created, summativeResp.StatusCode);

        var createdSummative = await summativeResp.Content.ReadFromJsonAsync<AssignmentDto>(JsonOptions);
        Assert.NotNull(createdSummative);
        Assert.Equal("Ulangan Sumatif 2", createdSummative!.Title);
        Assert.Equal("SUMMATIVE", createdSummative.AssignmentType);
        Assert.Equal(20.00m, createdSummative.Weight);

        // 3. Batch score entry for formative assignment: submit scores and qualitative feedback
        var enterScoresReq = new EnterStudentScoresRequest([
            new ScoreEntryItemDto(context.Student1Id, 90.00m, "Pemahaman konsep sangat baik"),
            new ScoreEntryItemDto(context.Student2Id, 80.00m, "Tingkatkan latihan aljabar")
        ]);

        var scoreResp = await teacherClient.PostAsJsonAsync(
            $"/api/acd/assignments/{createdFormative.Id}/scores",
            enterScoresReq);
        Assert.Equal(HttpStatusCode.OK, scoreResp.StatusCode);

        var scoreResult = await scoreResp.Content.ReadFromJsonAsync<EnterStudentScoresResponse>(JsonOptions);
        Assert.NotNull(scoreResult);
        Assert.Equal(createdFormative.Id, scoreResult!.AssignmentId);
        Assert.Equal(2, scoreResult.ScoresUpdatedCount);

        // 4. Gradebook summary query: verifies running score computation matches weighted formula and includes aggregated attendance
        var gradebookUrl = $"/api/acd/gradebook?subjectStageId={context.SubjectStageId}&studyGroupId={context.StudyGroupId7A}&operationalPeriodId={context.OperationalPeriodId}";
        var gradebookResp = await teacherClient.GetAsync(gradebookUrl);
        Assert.Equal(HttpStatusCode.OK, gradebookResp.StatusCode);

        var gradebook = await gradebookResp.Content.ReadFromJsonAsync<GradebookSummaryResponse>(JsonOptions);
        Assert.NotNull(gradebook);
        Assert.Equal(context.SubjectStageId, gradebook!.SubjectStageId);
        Assert.Equal(context.StudyGroupId7A, gradebook.StudyGroupId);
        Assert.Equal(context.OperationalPeriodId, gradebook.OperationalPeriodId);

        // Gradebook contains seeded assignments (Kuis Aljabar 1 [wt 10], Ulangan Harian 1 [wt 25]) plus new assignments
        Assert.Contains(gradebook.Assignments, a => a.Id == createdFormative.Id);
        Assert.Contains(gradebook.Assignments, a => a.Id == createdSummative.Id);

        Assert.Equal(2, gradebook.Students.Count);

        // Ahmad Dahlan:
        // Seeded Kuis Aljabar 1: 85 (wt 10) -> 850
        // Seeded Ulangan Harian 1: 88 (wt 25) -> 2200
        // Kuis Formatif 2: 90 (wt 15) -> 1350
        // Total weighted score = (850 + 2200 + 1350) / (10 + 25 + 15) = 4400 / 50 = 88.00
        var ahmad = gradebook.Students.First(s => s.StudentId == context.Student1Id);
        Assert.NotNull(ahmad.RunningScore);
        Assert.Equal(88.00m, ahmad.RunningScore!.Value);
        Assert.True(ahmad.Scores.Count >= 3);

        var ahmadFormativeScore = ahmad.Scores.First(s => s.AssignmentId == createdFormative.Id);
        Assert.Equal(90.00m, ahmadFormativeScore.Score);
        Assert.Equal("Pemahaman konsep sangat baik", ahmadFormativeScore.Feedback);

        // Verify aggregated attendance metrics are included
        Assert.True(ahmad.Attendance.PresentCount >= 1);

        // Siti Nurhaliza:
        // Seeded Kuis Aljabar 1: 92.5 (wt 10) -> 925
        // Seeded Ulangan Harian 1: 95 (wt 25) -> 2375
        // Kuis Formatif 2: 80 (wt 15) -> 1200
        // Total weighted score = (925 + 2375 + 1200) / 50 = 4500 / 50 = 90.00
        var siti = gradebook.Students.First(s => s.StudentId == context.Student2Id);
        Assert.NotNull(siti.RunningScore);
        Assert.Equal(90.00m, siti.RunningScore!.Value);
        Assert.True(siti.Scores.Count >= 3);

        var sitiFormativeScore = siti.Scores.First(s => s.AssignmentId == createdFormative.Id);
        Assert.Equal(80.00m, sitiFormativeScore.Score);
        Assert.Equal("Tingkatkan latihan aljabar", sitiFormativeScore.Feedback);

        Assert.True(siti.Attendance.PresentCount >= 1);
    }

    [Fact]
    public async Task ScorebookEndpoints_Anonymous_Return401Unauthorized()
    {
        var context = await GetScorebookFixtureContextAsync();
        using var client = CreateAnonymousClient();

        var gradebookResp = await client.GetAsync(
            $"/api/acd/gradebook?subjectStageId={context.SubjectStageId}&studyGroupId={context.StudyGroupId7A}&operationalPeriodId={context.OperationalPeriodId}");
        Assert.Equal(HttpStatusCode.Unauthorized, gradebookResp.StatusCode);

        var postResp = await client.PostAsJsonAsync("/api/acd/assignments", new CreateAssignmentRequest(
            context.SubjectStageId,
            context.SubjectId,
            context.StudyGroupId7A,
            context.OperationalPeriodId,
            null,
            "Anonymous Assignment",
            "FORMATIVE",
            100m,
            10m,
            DateOnly.FromDateTime(DateTime.UtcNow),
            null));
        Assert.Equal(HttpStatusCode.Unauthorized, postResp.StatusCode);
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

        var studyGroupId7A = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[study_groups] WHERE [name] = '7A'");

        var studyGroupId7B = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[study_groups] WHERE [name] = '7B'");

        var periodId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [opr].[operational_periods] WHERE [status] = 'ACTIVE' ORDER BY [start_date] ASC");

        var teacherId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [teacher_id] FROM [tch].[teacher_assignments] WHERE [study_group_id] = @GroupId AND [status] = 'ACTIVE';",
            new { GroupId = studyGroupId7A });

        var students = (await connection.QueryAsync<Guid>(
            "SELECT [student_id] FROM [acd].[academic_participations] WHERE [subject_stage_id] = @StageId AND [study_group_id] = @GroupId ORDER BY [student_name];",
            new { StageId = stageId, GroupId = studyGroupId7A })).ToList();

        return new ScorebookFixtureContext(
            stageId,
            subjectId,
            studyGroupId7A,
            studyGroupId7B,
            periodId,
            teacherId,
            students[0],
            students[1]);
    }

    private sealed record ScorebookFixtureContext(
        Guid SubjectStageId,
        Guid SubjectId,
        Guid StudyGroupId7A,
        Guid StudyGroupId7B,
        Guid OperationalPeriodId,
        Guid TeacherId,
        Guid Student1Id,
        Guid Student2Id);
}
