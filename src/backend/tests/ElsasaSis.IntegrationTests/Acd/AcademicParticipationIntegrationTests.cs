using System.Net;
using System.Net.Http.Json;
using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Acd.Features.LearningStructure;
using ElsasaSis.Modules.Acd.Features.LearningStructure.CreateSubjectStage;
using ElsasaSis.Modules.Acd.Features.Participations.EnrollStudentsInSubjectStages;
using ElsasaSis.Modules.Acd.Features.Participations.GetSubjectStageParticipations;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ElsasaSis.IntegrationTests.Acd;

public sealed class AcademicParticipationIntegrationTests : IntegrationTestBase
{
    public AcademicParticipationIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await using (var connection = new SqlConnection(Factory.ConnectionString))
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync("""
                DELETE FROM acd.academic_participations WHERE subject_stage_name LIKE 'Stage Test%';
                DELETE FROM acd.subject_stages WHERE name LIKE 'Stage Test%';
                DELETE FROM acd.subjects WHERE code NOT IN ('PA-BP', 'PPKN', 'BIND', 'MTK', 'IPA', 'IPS', 'BING', 'SNB', 'PJOK', 'INF');
                """);
        }

        await SeedStage1FixturesAsync();
        await SeedStage2FixturesAsync();
    }

    [Fact]
    public async Task GetParticipations_WhenSeededParticipationsExist_ReturnsEnrolledStudentsList()
    {
        var (subjectStageId, studyGroupId, periodId) = await GetSeededContextAsync();

        using var client = await CreateAuthenticatedAdminClientAsync();

        var url = $"/api/acd/participations?subjectStageId={subjectStageId}&studyGroupId={studyGroupId}&operationalPeriodId={periodId}";
        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<List<SubjectStageParticipationDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.Equal(2, list!.Count);

        var ahmad = list.First(s => s.StudentName == Stage2Seeder.DefaultStudent1Name);
        Assert.Equal("ACTIVE", ahmad.Status);
        Assert.Equal(Stage2Seeder.DefaultMathStageName, ahmad.SubjectStageName);
        Assert.Equal("Matematika", ahmad.SubjectName);
        Assert.Equal("7A", ahmad.StudyGroupName);

        var siti = list.First(s => s.StudentName == Stage2Seeder.DefaultStudent2Name);
        Assert.Equal("ACTIVE", siti.Status);
        Assert.Equal(Stage2Seeder.DefaultMathStageName, siti.SubjectStageName);
        Assert.Equal("Matematika", siti.SubjectName);
        Assert.Equal("7A", siti.StudyGroupName);
    }

    [Fact]
    public async Task BatchEnroll_WhenStudentsAlreadyEnrolled_SkipsGracefully()
    {
        var (subjectStageId, studyGroupId, periodId) = await GetSeededContextAsync();

        using var client = await CreateAuthenticatedAdminClientAsync();

        var request = new EnrollStudentsInSubjectStagesRequest(periodId, subjectStageId, studyGroupId);
        var response = await client.PostAsJsonAsync("/api/acd/participations/batch", request, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EnrollStudentsInSubjectStagesResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(0, result!.EnrolledCount);
        Assert.Equal(2, result.SkippedCount);
    }

    [Fact]
    public async Task BatchEnroll_IntoNewSubjectStage_EnrollsAllPlacedStudents()
    {
        var (_, studyGroupId, periodId) = await GetSeededContextAsync();

        using var client = await CreateAuthenticatedAdminClientAsync();

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var stageName = $"Stage Test {suffix}";
        SubjectStageDto? stage = null;

        try
        {
            // 1. Create a new Subject Stage under existing MTK subject
            var createStageReq = new CreateSubjectStageRequest(stageName, 99, "Test Stage for Batch Enroll");
            var createStageResp = await client.PostAsJsonAsync($"/api/acd/subjects/{Stage2Seeder.DefaultMtkSubjectId}/stages", createStageReq, JsonOptions);
            Assert.Equal(HttpStatusCode.Created, createStageResp.StatusCode);
            stage = await createStageResp.Content.ReadFromJsonAsync<SubjectStageDto>(JsonOptions);
            Assert.NotNull(stage);

            // 2. Batch enroll Study Group 7A into new stage
            var batchResp = await client.PostAsJsonAsync(
                "/api/acd/participations/batch",
                new EnrollStudentsInSubjectStagesRequest(periodId, stage!.Id, studyGroupId),
                JsonOptions);

            Assert.Equal(HttpStatusCode.OK, batchResp.StatusCode);
            var batchResult = await batchResp.Content.ReadFromJsonAsync<EnrollStudentsInSubjectStagesResponse>(JsonOptions);
            Assert.NotNull(batchResult);
            Assert.Equal(2, batchResult!.EnrolledCount);
            Assert.Equal(0, batchResult.SkippedCount);

            // 3. Query GET /api/acd/participations to verify new enrollments
            var getResp = await client.GetAsync($"/api/acd/participations?subjectStageId={stage.Id}&studyGroupId={studyGroupId}&operationalPeriodId={periodId}");
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
            var participations = await getResp.Content.ReadFromJsonAsync<List<SubjectStageParticipationDto>>(JsonOptions);
            Assert.NotNull(participations);
            Assert.Equal(2, participations!.Count);
            Assert.All(participations, p =>
            {
                Assert.Equal("ACTIVE", p.Status);
                Assert.Equal(stageName, p.SubjectStageName);
                Assert.Equal("Matematika", p.SubjectName);
            });

            // 4. Subsequent batch enrollment call returns 0 enrolled, 2 skipped
            var secondBatchResp = await client.PostAsJsonAsync(
                "/api/acd/participations/batch",
                new EnrollStudentsInSubjectStagesRequest(periodId, stage.Id, studyGroupId),
                JsonOptions);

            Assert.Equal(HttpStatusCode.OK, secondBatchResp.StatusCode);
            var secondBatchResult = await secondBatchResp.Content.ReadFromJsonAsync<EnrollStudentsInSubjectStagesResponse>(JsonOptions);
            Assert.NotNull(secondBatchResult);
            Assert.Equal(0, secondBatchResult!.EnrolledCount);
            Assert.Equal(2, secondBatchResult.SkippedCount);
        }
        finally
        {
            if (stage is not null)
            {
                await using var connection = new SqlConnection(Factory.ConnectionString);
                await connection.OpenAsync();
                await connection.ExecuteAsync("DELETE FROM acd.academic_participations WHERE subject_stage_id = @StageId;", new { StageId = stage.Id });
                await connection.ExecuteAsync("DELETE FROM acd.subject_stages WHERE id = @StageId;", new { StageId = stage.Id });
            }
        }
    }

    private async Task<(Guid SubjectStageId, Guid StudyGroupId, Guid OperationalPeriodId)> GetSeededContextAsync()
    {
        await using var connection = new SqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();

        var stageId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [acd].[subject_stages] WHERE [name] LIKE '%Matematika%'");

        var groupId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[study_groups] WHERE [name] = '7A'");

        var periodId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [opr].[operational_periods] WHERE [status] = 'ACTIVE'");

        return (stageId, groupId, periodId);
    }
}
