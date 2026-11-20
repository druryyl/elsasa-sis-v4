using System.Net;
using System.Net.Http.Json;
using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Acd.Features.LearningStructure;
using ElsasaSis.Modules.Acd.Features.LearningStructure.CreateLearningUnit;
using ElsasaSis.Modules.Acd.Features.LearningStructure.CreateSubject;
using ElsasaSis.Modules.Acd.Features.LearningStructure.CreateSubjectStage;
using ElsasaSis.Modules.Acd.Features.LearningStructure.MapSubjectStageGrade;
using ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateLearningUnit;
using ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateSubject;
using ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateSubjectStage;
using ElsasaSis.Modules.Acd.Features.Participations.EnrollStudentsInSubjectStages;
using ElsasaSis.Modules.Acd.Features.Participations.GetSubjectStageParticipations;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests.Acd;

public sealed class Stage2CurriculumIntegrationTests : IntegrationTestBase
{
    public Stage2CurriculumIntegrationTests(IntegrationTestWebApplicationFactory factory)
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
    public async Task Curriculum_Subject_Stage_Unit_GradeMapping_And_BatchEnrollment_Succeed()
    {
        var context = await GetCurriculumFixtureContextAsync();

        using var adminClient = await CreateAuthenticatedAdminClientAsync();
        using var teacherClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var subjectName = $"Fisika {suffix}";
        var subjectCode = $"FIS-{suffix}";

        // 1. Subject CRUD
        var createSubReq = new CreateSubjectRequest(subjectName, subjectCode);
        var createSubResp = await adminClient.PostAsJsonAsync("/api/acd/subjects", createSubReq);
        Assert.Equal(HttpStatusCode.Created, createSubResp.StatusCode);

        var subjectDto = await createSubResp.Content.ReadFromJsonAsync<SubjectDto>(JsonOptions);
        Assert.NotNull(subjectDto);
        Assert.Equal(subjectName, subjectDto!.Name);
        Assert.Equal(subjectCode, subjectDto.Code);

        // Update Subject
        var updateSubReq = new UpdateSubjectRequest($"{subjectName} Terapan", $"{subjectCode}T");
        var updateSubResp = await adminClient.PutAsJsonAsync($"/api/acd/subjects/{subjectDto.Id}", updateSubReq);
        Assert.Equal(HttpStatusCode.OK, updateSubResp.StatusCode);

        var updatedSubject = await updateSubResp.Content.ReadFromJsonAsync<SubjectDto>(JsonOptions);
        Assert.NotNull(updatedSubject);
        Assert.Equal($"{subjectName} Terapan", updatedSubject!.Name);

        // 2. Stage CRUD & sequence ordering validation
        var createStageReq1 = new CreateSubjectStageRequest("Tingkat Dasar", 1, "Pengantar Fisika SMP");
        var createStageResp1 = await adminClient.PostAsJsonAsync($"/api/acd/subjects/{subjectDto.Id}/stages", createStageReq1);
        Assert.Equal(HttpStatusCode.Created, createStageResp1.StatusCode);

        var stageDto1 = await createStageResp1.Content.ReadFromJsonAsync<SubjectStageDto>(JsonOptions);
        Assert.NotNull(stageDto1);
        Assert.Equal(1, stageDto1!.SequenceOrder);

        // Sequence ordering validation: attempting duplicate sequenceOrder = 1 returns 409 Conflict
        var dupSeqStageReq = new CreateSubjectStageRequest("Tingkat Lanjutan", 1, "Duplicate Sequence");
        var dupSeqResp = await adminClient.PostAsJsonAsync($"/api/acd/subjects/{subjectDto.Id}/stages", dupSeqStageReq);
        Assert.Equal(HttpStatusCode.Conflict, dupSeqResp.StatusCode);

        // Valid sequence order 2
        var createStageReq2 = new CreateSubjectStageRequest("Tingkat Lanjutan", 2, "Fisika SMP Lanjutan");
        var createStageResp2 = await adminClient.PostAsJsonAsync($"/api/acd/subjects/{subjectDto.Id}/stages", createStageReq2);
        Assert.Equal(HttpStatusCode.Created, createStageResp2.StatusCode);

        // Update stage
        var updateStageReq = new UpdateSubjectStageRequest("Tingkat Dasar Revisi", 1, "Deskripsi Diperbarui");
        var updateStageResp = await adminClient.PutAsJsonAsync($"/api/acd/subjects/{subjectDto.Id}/stages/{stageDto1.Id}", updateStageReq);
        Assert.Equal(HttpStatusCode.OK, updateStageResp.StatusCode);

        // 3. Learning Unit CRUD & sequence ordering validation
        var createUnitReq1 = new CreateLearningUnitRequest("Mekanika Dasar", 1, "MODULE", "Pengenalan gerak dan gaya");
        var createUnitResp1 = await adminClient.PostAsJsonAsync($"/api/acd/subjects/{subjectDto.Id}/stages/{stageDto1.Id}/units", createUnitReq1);
        Assert.Equal(HttpStatusCode.Created, createUnitResp1.StatusCode);

        var unitDto1 = await createUnitResp1.Content.ReadFromJsonAsync<LearningUnitDto>(JsonOptions);
        Assert.NotNull(unitDto1);
        Assert.Equal(1, unitDto1!.SequenceOrder);

        // Duplicate unit sequence returns 409 Conflict
        var dupUnitSeqReq = new CreateLearningUnitRequest("Optika Dasar", 1, "MODULE", "Duplicate Unit Seq");
        var dupUnitSeqResp = await adminClient.PostAsJsonAsync($"/api/acd/subjects/{subjectDto.Id}/stages/{stageDto1.Id}/units", dupUnitSeqReq);
        Assert.Equal(HttpStatusCode.Conflict, dupUnitSeqResp.StatusCode);

        // Unit update
        var updateUnitReq = new UpdateLearningUnitRequest("Mekanika Terapan", 1, "MODULE", "Gaya dan hukum Newton");
        var updateUnitResp = await adminClient.PutAsJsonAsync($"/api/acd/subjects/{subjectDto.Id}/stages/{stageDto1.Id}/units/{unitDto1.Id}", updateUnitReq);
        Assert.Equal(HttpStatusCode.OK, updateUnitResp.StatusCode);

        // 4. Grade mapping creation and deletion
        var mapGradeReq = new MapSubjectStageGradeRequest("SMP", "7");
        var mapGradeResp = await adminClient.PostAsJsonAsync(
            $"/api/acd/subjects/{subjectDto.Id}/stages/{stageDto1.Id}/grade-mappings",
            mapGradeReq);
        Assert.Equal(HttpStatusCode.Created, mapGradeResp.StatusCode);

        var gradeMappingDto = await mapGradeResp.Content.ReadFromJsonAsync<SubjectStageGradeDto>(JsonOptions);
        Assert.NotNull(gradeMappingDto);
        Assert.Equal("SMP", gradeMappingDto!.SchoolLevel);
        Assert.Equal("7", gradeMappingDto.Grade);

        // Delete grade mapping
        var deleteGradeResp = await adminClient.DeleteAsync(
            $"/api/acd/subjects/{subjectDto.Id}/stages/{stageDto1.Id}/grade-mappings/{gradeMappingDto.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteGradeResp.StatusCode);

        // Re-add grade mapping for subsequent batch enrollment verification
        var reMapGradeResp = await adminClient.PostAsJsonAsync(
            $"/api/acd/subjects/{subjectDto.Id}/stages/{stageDto1.Id}/grade-mappings",
            mapGradeReq);
        Assert.Equal(HttpStatusCode.Created, reMapGradeResp.StatusCode);

        // 5. Batch academic participation enrollment (POST /api/acd/participations/batch)
        // First call: placed students of 7A enrolled into newly created stage
        var batchReq = new EnrollStudentsInSubjectStagesRequest(
            context.OperationalPeriodId,
            stageDto1.Id,
            context.StudyGroupId7A);

        var batchResp1 = await adminClient.PostAsJsonAsync("/api/acd/participations/batch", batchReq);
        Assert.Equal(HttpStatusCode.OK, batchResp1.StatusCode);

        var batchResult1 = await batchResp1.Content.ReadFromJsonAsync<EnrollStudentsInSubjectStagesResponse>(JsonOptions);
        Assert.NotNull(batchResult1);
        Assert.Equal(2, batchResult1!.EnrolledCount);
        Assert.Equal(0, batchResult1.SkippedCount);

        // Query participations: verify enrolled students
        var participationsResp = await adminClient.GetAsync(
            $"/api/acd/participations?subjectStageId={stageDto1.Id}&studyGroupId={context.StudyGroupId7A}&operationalPeriodId={context.OperationalPeriodId}");
        Assert.Equal(HttpStatusCode.OK, participationsResp.StatusCode);

        var participations = await participationsResp.Content.ReadFromJsonAsync<List<SubjectStageParticipationDto>>(JsonOptions);
        Assert.NotNull(participations);
        Assert.Equal(2, participations!.Count);
        Assert.Contains(participations, p => p.StudentId == context.Student1Id && p.Status == "ACTIVE");
        Assert.Contains(participations, p => p.StudentId == context.Student2Id && p.Status == "ACTIVE");

        // Second call: skips duplicate students idempotently and returns { enrolledCount: 0, skippedCount: N }
        var batchResp2 = await adminClient.PostAsJsonAsync("/api/acd/participations/batch", batchReq);
        Assert.Equal(HttpStatusCode.OK, batchResp2.StatusCode);

        var batchResult2 = await batchResp2.Content.ReadFromJsonAsync<EnrollStudentsInSubjectStagesResponse>(JsonOptions);
        Assert.NotNull(batchResult2);
        Assert.Equal(0, batchResult2!.EnrolledCount);
        Assert.Equal(2, batchResult2.SkippedCount);
    }

    private async Task<CurriculumFixtureContext> GetCurriculumFixtureContextAsync()
    {
        await using var connection = new SqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();

        var periodId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [opr].[operational_periods] WHERE [status] = 'ACTIVE'");

        var studyGroupId7A = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[study_groups] WHERE [name] = '7A'");

        var student1Id = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[students] WHERE [nis] = @Nis",
            new { Nis = Stage2Seeder.DefaultStudent1Nis });

        var student2Id = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[students] WHERE [nis] = @Nis",
            new { Nis = Stage2Seeder.DefaultStudent2Nis });

        return new CurriculumFixtureContext(
            periodId,
            studyGroupId7A,
            student1Id,
            student2Id);
    }

    private sealed record CurriculumFixtureContext(
        Guid OperationalPeriodId,
        Guid StudyGroupId7A,
        Guid Student1Id,
        Guid Student2Id);
}
