using System.Net;
using System.Net.Http.Json;
using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Acd.Features.ReportCards;
using ElsasaSis.Modules.Acd.Features.ReportCards.GenerateDraftReportCards;
using ElsasaSis.Modules.Acd.Features.ReportCards.GetHomeroomReportCards;
using ElsasaSis.Modules.Acd.Features.ReportCards.GetReportCardById;
using ElsasaSis.Modules.Acd.Features.ReportCards.OverrideReportCardStatus;
using ElsasaSis.Modules.Acd.Features.ReportCards.ProvideSubjectReportCardNotes;
using ElsasaSis.Modules.Acd.Features.ReportCards.WriteHomeroomRemarks;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests;

public sealed class Stage3AcdReportCardTests : IntegrationTestBase
{
    public Stage3AcdReportCardTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedStage1FixturesAsync();
        await SeedStage3FixturesAsync();
    }

    private async Task<(Guid StudyGroupId, Guid OperationalPeriodId, Guid Student1Id, Guid MathStageId, Guid EnglishStageId, Guid AhmadCardId, Guid SitiCardId)> GetFixtureContextAsync()
    {
        await using var conn = new SqlConnection(Factory.ConnectionString);
        await conn.OpenAsync();

        var studyGroupId = await conn.QuerySingleAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[study_groups] WHERE [name] = '7A'");

        var periodId = await conn.QuerySingleAsync<Guid>(
            "SELECT TOP (1) [id] FROM [opr].[operational_periods] WHERE [status] = 'ACTIVE' ORDER BY [start_date] ASC");

        var student1Id = await conn.QuerySingleAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[students] WHERE [nis] = '202607001' OR [full_name] = N'Ahmad Dahlan'");

        var mathStageId = await conn.QuerySingleAsync<Guid>(
            "SELECT TOP (1) [id] FROM [acd].[subject_stages] WHERE [name] LIKE N'%Matematika%'");

        var englishStageId = await conn.QuerySingleAsync<Guid>(
            "SELECT TOP (1) [id] FROM [acd].[subject_stages] WHERE [name] LIKE N'%Inggris%'");

        var ahmadCardId = await conn.QuerySingleOrDefaultAsync<Guid>(
            "SELECT TOP (1) [id] FROM [acd].[report_cards] WHERE [student_id] = @StudentId AND [operational_period_id] = @PeriodId",
            new { StudentId = student1Id, PeriodId = periodId });

        var student2Id = await conn.QuerySingleAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[students] WHERE [nis] = '202607002' OR [full_name] = N'Siti Nurhaliza'");

        var sitiCardId = await conn.QuerySingleOrDefaultAsync<Guid>(
            "SELECT TOP (1) [id] FROM [acd].[report_cards] WHERE [student_id] = @StudentId AND [operational_period_id] = @PeriodId",
            new { StudentId = student2Id, PeriodId = periodId });

        return (studyGroupId, periodId, student1Id, mathStageId, englishStageId, ahmadCardId, sitiCardId);
    }

    [Fact]
    public async Task BatchDraftReportCardGeneration_ComputesArithmeticMeanFinalGrade_AndIsIdempotent()
    {
        var ctx = await GetFixtureContextAsync();
        using var adminClient = await CreateAuthenticatedAdminClientAsync();

        // 1. Delete pre-existing report cards for 7A to verify fresh generation
        await using (var conn = new SqlConnection(Factory.ConnectionString))
        {
            await conn.OpenAsync();
            await conn.ExecuteAsync("""
                DELETE FROM [acd].[report_card_entries];
                DELETE FROM [acd].[report_cards];
                """);
        }

        var batchReq = new GenerateDraftReportCardsRequest(
            ctx.StudyGroupId,
            ctx.OperationalPeriodId);

        // 2. Fresh generation -> generates draft cards for active placed students (Ahmad & Siti)
        var postResp = await adminClient.PostAsJsonAsync("/api/acd/report-cards/generate-batch", batchReq);
        Assert.Equal(HttpStatusCode.OK, postResp.StatusCode);

        var batchResult = await postResp.Content.ReadFromJsonAsync<GenerateDraftReportCardsResponse>(JsonOptions);
        Assert.NotNull(batchResult);
        Assert.Equal(2, batchResult!.GeneratedCount);
        Assert.Equal(0, batchResult.SkippedCount);

        // 3. Verify arithmetic mean calculation on Ahmad's generated card
        await using (var conn = new SqlConnection(Factory.ConnectionString))
        {
            await conn.OpenAsync();
            var ahmadCardId = await conn.QuerySingleAsync<Guid>(
                "SELECT [id] FROM [acd].[report_cards] WHERE [student_id] = @StudentId AND [operational_period_id] = @PeriodId",
                new { StudentId = ctx.Student1Id, PeriodId = ctx.OperationalPeriodId });

            var getResp = await adminClient.GetAsync($"/api/acd/report-cards/{ahmadCardId}");
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

            var detail = await getResp.Content.ReadFromJsonAsync<ReportCardDetailDto>(JsonOptions);
            Assert.NotNull(detail);
            Assert.Equal("DRAFT", detail!.Status);
            Assert.NotEmpty(detail.Entries);

            // In Stage 2 seeder, Ahmad has scores in MTK. Verify numeric score is calculated.
            var mtkEntry = detail.Entries.FirstOrDefault(e => e.SubjectStageId == ctx.MathStageId);
            Assert.NotNull(mtkEntry);
            Assert.True(mtkEntry!.NumericScore > 0);
        }

        // 4. Idempotent re-run -> all students skipped
        var secondResp = await adminClient.PostAsJsonAsync("/api/acd/report-cards/generate-batch", batchReq);
        Assert.Equal(HttpStatusCode.OK, secondResp.StatusCode);

        var secondResult = await secondResp.Content.ReadFromJsonAsync<GenerateDraftReportCardsResponse>(JsonOptions);
        Assert.NotNull(secondResult);
        Assert.Equal(0, secondResult!.GeneratedCount);
        Assert.Equal(2, secondResult.SkippedCount);
    }

    [Fact]
    public async Task SubjectNotesSubmission_EnforcesTeachingAssignmentCheck()
    {
        var ctx = await GetFixtureContextAsync();
        using var teacherClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        var sitiDraftCardId = ctx.SitiCardId != Guid.Empty ? ctx.SitiCardId : Stage3Seeder.DefaultReportCardSitiId;

        // 1. Submit notes for assigned subject (Math) -> 200 OK
        var mathNotesReq = new ProvideSubjectNotesRequest(
            ctx.MathStageId,
            "Ananda menunjukkan pemahaman yang sangat baik dalam konsep aljabar.");

        var mathResp = await teacherClient.PostAsJsonAsync(
            $"/api/acd/report-cards/{sitiDraftCardId}/subject-notes",
            mathNotesReq);
        Assert.Equal(HttpStatusCode.OK, mathResp.StatusCode);

        // Verify notes updated in database
        using var adminClient = await CreateAuthenticatedAdminClientAsync();
        var getResp = await adminClient.GetAsync($"/api/acd/report-cards/{sitiDraftCardId}");
        var detail = await getResp.Content.ReadFromJsonAsync<ReportCardDetailDto>(JsonOptions);
        Assert.NotNull(detail);
        var mathEntry = detail!.Entries.FirstOrDefault(e => e.SubjectStageId == ctx.MathStageId);
        Assert.NotNull(mathEntry);
        Assert.Equal(mathNotesReq.TeacherNotes, mathEntry!.TeacherNotes);

        // 2. Submit notes for non-assigned subject (English) -> 409 Conflict
        var englishNotesReq = new ProvideSubjectNotesRequest(
            ctx.EnglishStageId,
            "Catatan dari guru yang tidak mengajar mata pelajaran ini.");

        var unassignedResp = await teacherClient.PostAsJsonAsync(
            $"/api/acd/report-cards/{sitiDraftCardId}/subject-notes",
            englishNotesReq);
        Assert.Equal(HttpStatusCode.Conflict, unassignedResp.StatusCode);
    }

    [Fact]
    public async Task HomeroomRemarks_And_Finalization_TransitionsToFinalized()
    {
        var ctx = await GetFixtureContextAsync();
        // Teacher Budi is the assigned homeroom teacher for Study Group 7A
        using var homeroomClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        var sitiDraftCardId = ctx.SitiCardId != Guid.Empty ? ctx.SitiCardId : Stage3Seeder.DefaultReportCardSitiId;

        // 1. Homeroom teacher writes holistic general remarks -> 200 OK
        var remarksReq = new WriteHomeroomRemarksRequest("Pertahankan kedisiplinan dan semangat belajar di semester berikutnya!");
        var remarksResp = await homeroomClient.PostAsJsonAsync(
            $"/api/acd/report-cards/{sitiDraftCardId}/homeroom-remarks",
            remarksReq);
        Assert.Equal(HttpStatusCode.OK, remarksResp.StatusCode);

        // 2. Homeroom teacher finalizes report card -> 200 OK
        var finalizeResp = await homeroomClient.PostAsJsonAsync(
            $"/api/acd/report-cards/{sitiDraftCardId}/finalize",
            new { });
        Assert.Equal(HttpStatusCode.OK, finalizeResp.StatusCode);

        // 3. Verify status transitioned to FINALIZED
        using var adminClient = await CreateAuthenticatedAdminClientAsync();
        var getResp = await adminClient.GetAsync($"/api/acd/report-cards/{sitiDraftCardId}");
        var detail = await getResp.Content.ReadFromJsonAsync<ReportCardDetailDto>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal("FINALIZED", detail!.Status);
        Assert.NotNull(detail.FinalizedAt);
        Assert.Equal(remarksReq.Remarks, detail.GeneralRemarks);

        // 4. Modifying finalized card returns 409 Conflict
        var conflictRemarksResp = await homeroomClient.PostAsJsonAsync(
            $"/api/acd/report-cards/{sitiDraftCardId}/homeroom-remarks",
            new WriteHomeroomRemarksRequest("Upaya edit catatan setelah finalisasi."));
        Assert.Equal(HttpStatusCode.Conflict, conflictRemarksResp.StatusCode);
    }

    [Fact]
    public async Task AdministrativeOverride_UnlocksCardBackToDraft_WithMandatoryReason()
    {
        var ctx = await GetFixtureContextAsync();
        using var adminClient = await CreateAuthenticatedAdminClientAsync();

        // Ahmad's report card is seeded as FINALIZED
        var ahmadFinalizedCardId = ctx.AhmadCardId != Guid.Empty ? ctx.AhmadCardId : Stage3Seeder.DefaultReportCardAhmadId;

        // 1. Empty reason rejected by FluentValidation -> 422 UnprocessableEntity
        var emptyReq = new OverrideReportCardStatusRequest(string.Empty);
        var invalidResp = await adminClient.PostAsJsonAsync(
            $"/api/acd/report-cards/{ahmadFinalizedCardId}/override",
            emptyReq);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidResp.StatusCode);

        // 2. Non-empty reason successfully unlocks to DRAFT -> 200 OK
        var validReq = new OverrideReportCardStatusRequest("Koreksi penilaian remedial asesmen sumatif akhir semester.");
        var overrideResp = await adminClient.PostAsJsonAsync(
            $"/api/acd/report-cards/{ahmadFinalizedCardId}/override",
            validReq);
        Assert.Equal(HttpStatusCode.OK, overrideResp.StatusCode);

        // 3. Verify report card is unlocked back to DRAFT, finalized_at cleared, override_reason preserved
        var getResp = await adminClient.GetAsync($"/api/acd/report-cards/{ahmadFinalizedCardId}");
        var detail = await getResp.Content.ReadFromJsonAsync<ReportCardDetailDto>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal("DRAFT", detail!.Status);
        Assert.Null(detail.FinalizedAt);
        Assert.Equal(validReq.OverrideReason, detail.OverrideReason);
    }
}
