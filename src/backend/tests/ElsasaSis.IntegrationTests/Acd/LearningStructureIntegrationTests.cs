using System.Net;
using System.Net.Http.Json;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Acd.Features.LearningStructure;
using ElsasaSis.Modules.Acd.Features.LearningStructure.CreateLearningUnit;
using ElsasaSis.Modules.Acd.Features.LearningStructure.CreateSubject;
using ElsasaSis.Modules.Acd.Features.LearningStructure.CreateSubjectStage;
using ElsasaSis.Modules.Acd.Features.LearningStructure.GetSubjects;
using ElsasaSis.Modules.Acd.Features.LearningStructure.MapSubjectStageGrade;
using ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateLearningUnit;
using ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateSubject;
using ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateSubjectStage;
using Xunit;

namespace ElsasaSis.IntegrationTests.Acd;

public sealed class LearningStructureIntegrationTests : IntegrationTestBase
{
    public LearningStructureIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task LearningStructure_FullLifecycle_Subject_Stage_Unit_GradeMapping_And_HierarchyQuery()
    {
        await SeedStage1FixturesAsync();
        await SeedStage2FixturesAsync();

        using var adminClient = await CreateAuthenticatedAdminClientAsync();
        using var teacherClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var subName = $"Biologi {suffix}";
        var subCode = $"BIO-{suffix}";

        SubjectDto? createdSubject = null;
        try
        {
            // 1. Create Subject (POST /api/acd/subjects)
            var createSubjectReq = new CreateSubjectRequest(subName, subCode);
            var createSubResp = await adminClient.PostAsJsonAsync("/api/acd/subjects", createSubjectReq);
            Assert.Equal(HttpStatusCode.Created, createSubResp.StatusCode);

            createdSubject = await createSubResp.Content.ReadFromJsonAsync<SubjectDto>(JsonOptions);
            Assert.NotNull(createdSubject);
            Assert.Equal(subName, createdSubject!.Name);
            Assert.Equal(subCode, createdSubject.Code);

            // 2. Duplicate Subject Name returns 409 Conflict
            var dupNameResp = await adminClient.PostAsJsonAsync("/api/acd/subjects", new CreateSubjectRequest(subName, $"B2-{suffix}"));
            Assert.Equal(HttpStatusCode.Conflict, dupNameResp.StatusCode);

            // 3. Update Subject (PUT /api/acd/subjects/{id})
            var updatedSubName = $"Biologi Umum {suffix}";
            var updatedSubCode = $"BIOU-{suffix}";
            var updateSubResp = await adminClient.PutAsJsonAsync(
                $"/api/acd/subjects/{createdSubject.Id}",
                new UpdateSubjectRequest(updatedSubName, updatedSubCode));
            Assert.Equal(HttpStatusCode.OK, updateSubResp.StatusCode);

            var updatedSubject = await updateSubResp.Content.ReadFromJsonAsync<SubjectDto>(JsonOptions);
            Assert.NotNull(updatedSubject);
            Assert.Equal(updatedSubName, updatedSubject!.Name);
            Assert.Equal(updatedSubCode, updatedSubject.Code);

            // 4. Create Subject Stage (POST /api/acd/subjects/{id}/stages)
            var createStageReq = new CreateSubjectStageRequest("Tingkat 10", 1, "Biologi SMA Kelas 10");
            var createStageResp = await adminClient.PostAsJsonAsync(
                $"/api/acd/subjects/{createdSubject.Id}/stages",
                createStageReq);
            Assert.Equal(HttpStatusCode.Created, createStageResp.StatusCode);

            var createdStage = await createStageResp.Content.ReadFromJsonAsync<SubjectStageDto>(JsonOptions);
            Assert.NotNull(createdStage);
            Assert.Equal("Tingkat 10", createdStage!.Name);
            Assert.Equal(1, createdStage.SequenceOrder);
            Assert.Equal(updatedSubName, createdStage.SubjectName);

            // 5. Duplicate Stage Sequence returns 409 Conflict
            var dupSeqResp = await adminClient.PostAsJsonAsync(
                $"/api/acd/subjects/{createdSubject.Id}/stages",
                new CreateSubjectStageRequest("Tingkat 10 Lanjutan", 1, null));
            Assert.Equal(HttpStatusCode.Conflict, dupSeqResp.StatusCode);

            // 6. Update Subject Stage (PUT /api/acd/subjects/{id}/stages/{stageId})
            var updateStageResp = await adminClient.PutAsJsonAsync(
                $"/api/acd/subjects/{createdSubject.Id}/stages/{createdStage.Id}",
                new UpdateSubjectStageRequest("Tingkat 10 Revisi", 1, "Deskripsi Baru"));
            Assert.Equal(HttpStatusCode.OK, updateStageResp.StatusCode);

            var updatedStage = await updateStageResp.Content.ReadFromJsonAsync<SubjectStageDto>(JsonOptions);
            Assert.NotNull(updatedStage);
            Assert.Equal("Tingkat 10 Revisi", updatedStage!.Name);

            // 7. Create Learning Unit (POST /api/acd/subjects/{id}/stages/{stageId}/units)
            var createUnitReq = new CreateLearningUnitRequest("Sel dan Jaringan", 1, "MODULE", "Modul Pembelajaran Sel");
            var createUnitResp = await adminClient.PostAsJsonAsync(
                $"/api/acd/subjects/{createdSubject.Id}/stages/{createdStage.Id}/units",
                createUnitReq);
            Assert.Equal(HttpStatusCode.Created, createUnitResp.StatusCode);

            var createdUnit = await createUnitResp.Content.ReadFromJsonAsync<LearningUnitDto>(JsonOptions);
            Assert.NotNull(createdUnit);
            Assert.Equal("Sel dan Jaringan", createdUnit!.Name);
            Assert.Equal("MODULE", createdUnit.UnitType);
            Assert.Equal(1, createdUnit.SequenceOrder);

            // 8. Update Learning Unit (PUT /api/acd/subjects/{id}/stages/{stageId}/units/{unitId})
            var updateUnitResp = await adminClient.PutAsJsonAsync(
                $"/api/acd/subjects/{createdSubject.Id}/stages/{createdStage.Id}/units/{createdUnit.Id}",
                new UpdateLearningUnitRequest("Struktur Sel & Jaringan", 1, "LESSON", "Lesson 1"));
            Assert.Equal(HttpStatusCode.OK, updateUnitResp.StatusCode);

            var updatedUnit = await updateUnitResp.Content.ReadFromJsonAsync<LearningUnitDto>(JsonOptions);
            Assert.NotNull(updatedUnit);
            Assert.Equal("Struktur Sel & Jaringan", updatedUnit!.Name);
            Assert.Equal("LESSON", updatedUnit.UnitType);

            // 9. Map Subject Stage Grade (POST /api/acd/subjects/{id}/stages/{stageId}/grade-mappings)
            var mapGradeReq = new MapSubjectStageGradeRequest("SMA", "10");
            var mapGradeResp = await adminClient.PostAsJsonAsync(
                $"/api/acd/subjects/{createdSubject.Id}/stages/{createdStage.Id}/grade-mappings",
                mapGradeReq);
            Assert.Equal(HttpStatusCode.Created, mapGradeResp.StatusCode);

            var createdGradeMapping = await mapGradeResp.Content.ReadFromJsonAsync<SubjectStageGradeDto>(JsonOptions);
            Assert.NotNull(createdGradeMapping);
            Assert.Equal("SMA", createdGradeMapping!.SchoolLevel);
            Assert.Equal("10", createdGradeMapping.Grade);

            // 10. Invalid Grade Combination returns 422
            var invalidGradeResp = await adminClient.PostAsJsonAsync(
                $"/api/acd/subjects/{createdSubject.Id}/stages/{createdStage.Id}/grade-mappings",
                new MapSubjectStageGradeRequest("SD", "10"));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidGradeResp.StatusCode);

            // 11. Duplicate Grade Mapping returns 409
            var dupGradeResp = await adminClient.PostAsJsonAsync(
                $"/api/acd/subjects/{createdSubject.Id}/stages/{createdStage.Id}/grade-mappings",
                new MapSubjectStageGradeRequest("SMA", "10"));
            Assert.Equal(HttpStatusCode.Conflict, dupGradeResp.StatusCode);

            // 12. GetLearningStructure (GET /api/acd/subjects) accessible by teacher (Curriculum.View) and admin (Curriculum.Manage)
            var getStructureResp = await teacherClient.GetAsync("/api/acd/subjects");
            Assert.Equal(HttpStatusCode.OK, getStructureResp.StatusCode);

            var subjects = await getStructureResp.Content.ReadFromJsonAsync<IReadOnlyList<SubjectResponse>>(JsonOptions);
            Assert.NotNull(subjects);
            Assert.Contains(subjects!, s => s.Name == updatedSubName);

            var biologiSub = subjects!.First(s => s.Name == updatedSubName);
            Assert.NotNull(biologiSub.Stages);
            Assert.Single(biologiSub.Stages!);

            var stageFromQuery = biologiSub.Stages![0];
            Assert.Equal("Tingkat 10 Revisi", stageFromQuery.Name);
            Assert.Single(stageFromQuery.Units);
            Assert.Equal("Struktur Sel & Jaringan", stageFromQuery.Units[0].Name);
            Assert.Single(stageFromQuery.GradeMappings);
            Assert.Equal("SMA", stageFromQuery.GradeMappings[0].SchoolLevel);
            Assert.Equal("10", stageFromQuery.GradeMappings[0].Grade);

            // 13. Remove Subject Stage Grade (DELETE /api/acd/subjects/{id}/stages/{stageId}/grade-mappings/{mappingId})
            var removeGradeResp = await adminClient.DeleteAsync(
                $"/api/acd/subjects/{createdSubject.Id}/stages/{createdStage.Id}/grade-mappings/{createdGradeMapping.Id}");
            Assert.Equal(HttpStatusCode.NoContent, removeGradeResp.StatusCode);

            // 14. Verify grade mapping is removed in query
            var verifyResp = await adminClient.GetAsync("/api/acd/subjects");
            var verifySubjects = await verifyResp.Content.ReadFromJsonAsync<IReadOnlyList<SubjectResponse>>(JsonOptions);
            var verifyStage = verifySubjects!.First(s => s.Name == updatedSubName).Stages![0];
            Assert.Empty(verifyStage.GradeMappings);
        }
        finally
        {
            if (createdSubject is not null)
            {
                await using var connection = new Microsoft.Data.SqlClient.SqlConnection(Factory.ConnectionString);
                await connection.OpenAsync();
                await Dapper.SqlMapper.ExecuteAsync(connection, "DELETE FROM acd.subjects WHERE id = @Id;", new { Id = createdSubject.Id });
            }
        }
    }
}
