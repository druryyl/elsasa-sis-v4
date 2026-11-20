using System.Net;
using System.Net.Http.Json;
using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Dev.Features.Achievements;
using ElsasaSis.Modules.Dev.Features.Behaviour;
using ElsasaSis.Modules.Dev.Features.Discipline;
using ElsasaSis.Modules.Dev.Features.Dossier;
using ElsasaSis.Modules.Dev.Features.Extracurricular;
using ElsasaSis.Modules.Dev.Features.Extracurricular.CreateExtracurricularActivity;
using ElsasaSis.Modules.Dev.Features.Extracurricular.EnrollExtracurricularParticipant;
using ElsasaSis.Modules.Dev.Features.Extracurricular.RecordExtracurricularSessionLog;
using Microsoft.Data.SqlClient;
using ExtracurricularParticipationDto = ElsasaSis.Modules.Dev.Features.Extracurricular.ExtracurricularParticipationDto;

namespace ElsasaSis.IntegrationTests;

public sealed class Stage3DevPastoralTests : IntegrationTestBase
{
    public Stage3DevPastoralTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedStage1FixturesAsync();
        await SeedStage3FixturesAsync();
    }

    private async Task<(Guid AcademicYearId, string AcademicYearName, Guid BudiTeacherId, Guid Student1Id, string Student1Name, string Student1Nis, Guid Student2Id, string Student2Name, string Student2Nis)> GetFixtureContextAsync()
    {
        await using var conn = new SqlConnection(Factory.ConnectionString);
        await conn.OpenAsync();

        var (academicYearId, academicYearName) = await conn.QuerySingleAsync<(Guid, string)>(
            "SELECT TOP (1) [id], [name] FROM [opr].[academic_years] WHERE [status] = 'ACTIVE' ORDER BY [created_at] ASC");

        var budiTeacherId = await conn.QuerySingleAsync<Guid>(
            "SELECT TOP (1) [id] FROM [tch].[teachers] WHERE [nip] = '198501012010011001'");

        var (student1Id, student1Name, student1Nis) = await conn.QuerySingleAsync<(Guid, string, string)>(
            "SELECT TOP (1) [id], [full_name], [nis] FROM [std].[students] WHERE [nis] = '202607001' OR [full_name] = N'Ahmad Dahlan'");

        var (student2Id, student2Name, student2Nis) = await conn.QuerySingleAsync<(Guid, string, string)>(
            "SELECT TOP (1) [id], [full_name], [nis] FROM [std].[students] WHERE [nis] = '202607002' OR [full_name] = N'Siti Nurhaliza'");

        return (academicYearId, academicYearName, budiTeacherId, student1Id, student1Name, student1Nis, student2Id, student2Name, student2Nis);
    }

    [Fact]
    public async Task BehaviourNotes_Recording_Succeeds()
    {
        var ctx = await GetFixtureContextAsync();
        using var teacherClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var req = new RecordBehaviourNoteRequest(
            ctx.Student1Id,
            ctx.Student1Name,
            ctx.Student1Nis,
            ctx.AcademicYearId,
            ctx.AcademicYearName,
            "POSITIVE",
            today,
            "Membantu merapikan bangku dan perangkat proyektor setelah jam pelajaran selesai.");

        var postResp = await teacherClient.PostAsJsonAsync("/api/dev/behaviour", req);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);

        var created = await postResp.Content.ReadFromJsonAsync<BehaviourNoteDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(ctx.Student1Id, created!.StudentId);
        Assert.Equal("POSITIVE", created.BehaviourType);
        Assert.Equal(req.Description, created.Description);
        Assert.Equal(today, created.ObservedDate);
    }

    [Fact]
    public async Task Discipline_ClassroomWarning_Succeeds_AndRejectsEscalatedSanctions()
    {
        var ctx = await GetFixtureContextAsync();
        using var teacherClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Authorized classroom warning (VERBAL_WARNING) -> 201 Created
        var warningReq = new RecordDisciplineIncidentRequest(
            ctx.Student1Id,
            ctx.Student1Name,
            ctx.Student1Nis,
            ctx.AcademicYearId,
            ctx.AcademicYearName,
            today,
            "Terlambat",
            "Terlambat masuk kelas 15 menit tanpa surat keterangan.",
            "VERBAL_WARNING");

        var warningResp = await teacherClient.PostAsJsonAsync("/api/dev/discipline", warningReq);
        Assert.Equal(HttpStatusCode.Created, warningResp.StatusCode);

        var created = await warningResp.Content.ReadFromJsonAsync<DisciplineRecordDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("VERBAL_WARNING", created!.Sanction);
        Assert.Equal("Terlambat", created.ViolationType);

        // 2. Reject escalated sanction (SUSPENSION) on classroom endpoint -> 422 UnprocessableEntity
        var illegalReq = new RecordDisciplineIncidentRequest(
            ctx.Student1Id,
            ctx.Student1Name,
            ctx.Student1Nis,
            ctx.AcademicYearId,
            ctx.AcademicYearName,
            today,
            "Pelanggaran Berat",
            "Mencoba memberi skorsing langsung di tingkat guru.",
            "SUSPENSION");

        var illegalResp = await teacherClient.PostAsJsonAsync("/api/dev/discipline", illegalReq);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, illegalResp.StatusCode);
    }

    [Fact]
    public async Task Principal_EscalatedSanction_Succeeds_AndRejectsUnauthorizedCallers()
    {
        var ctx = await GetFixtureContextAsync();
        using var principalClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultPrincipalUsername,
            Stage1Seeder.DefaultPrincipalPassword);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Principal issues SUSPENSION -> 201 Created
        var escalatedReq = new RecordEscalatedSanctionRequest(
            ctx.Student2Id,
            ctx.Student2Name,
            ctx.Student2Nis,
            ctx.AcademicYearId,
            ctx.AcademicYearName,
            today,
            "Perkelahian",
            "Terlibat perkelahian fisik di kantin sekolah.",
            "SUSPENSION");

        var postResp = await principalClient.PostAsJsonAsync("/api/dev/discipline/escalated", escalatedReq);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);

        var created = await postResp.Content.ReadFromJsonAsync<DisciplineRecordDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("SUSPENSION", created!.Sanction);
        Assert.Equal("Perkelahian", created.ViolationType);

        // 2. Teacher caller attempting escalated endpoint -> 403 Forbidden
        using var teacherClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        var forbiddenResp = await teacherClient.PostAsJsonAsync("/api/dev/discipline/escalated", escalatedReq);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResp.StatusCode);
    }

    [Fact]
    public async Task Extracurricular_FullLifecycle_Create_Enroll_AndSupervisorSessionLogging()
    {
        var ctx = await GetFixtureContextAsync();
        using var adminClient = await CreateAuthenticatedAdminClientAsync();

        // 1. Admin creates new extracurricular club with Teacher Budi as assigned supervisor -> 201 Created
        var createActivityReq = new CreateExtracurricularActivityRequest(
            "Klub Robotik Elsasa",
            "Eksplorasi sensorik, IoT, dan pemrograman robot",
            ctx.BudiTeacherId);

        var createResp = await adminClient.PostAsJsonAsync("/api/dev/extracurriculars", createActivityReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var activity = await createResp.Content.ReadFromJsonAsync<ExtracurricularActivityDto>(JsonOptions);
        Assert.NotNull(activity);
        var activityId = activity!.Id;

        // 2. Admin enrolls Ahmad Dahlan into the club -> 201 Created
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var enrollReq = new EnrollExtracurricularParticipantRequest(
            ctx.Student1Id,
            ctx.Student1Name,
            ctx.Student1Nis,
            ctx.AcademicYearId,
            ctx.AcademicYearName,
            today);

        var enrollResp = await adminClient.PostAsJsonAsync(
            $"/api/dev/extracurriculars/{activityId}/participants",
            enrollReq);
        Assert.Equal(HttpStatusCode.Created, enrollResp.StatusCode);

        var participation = await enrollResp.Content.ReadFromJsonAsync<ExtracurricularParticipationDto>(JsonOptions);
        Assert.NotNull(participation);
        var participationId = participation!.Id;

        // 3. Assigned supervisor (Teacher Budi) records session meeting log -> 200 OK
        using var supervisorClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        var logReq = new RecordExtracurricularSessionLogRequest(
            today,
            "Materi kalibrasi sensor ultrasonik dan modul motor servo.");

        var logResp = await supervisorClient.PostAsJsonAsync(
            $"/api/dev/extracurriculars/{activityId}/participants/{participationId}/log",
            logReq);
        Assert.Equal(HttpStatusCode.OK, logResp.StatusCode);

        var updatedPart = await logResp.Content.ReadFromJsonAsync<ExtracurricularParticipationDto>(JsonOptions);
        Assert.NotNull(updatedPart);
        Assert.Equal(logReq.Log, updatedPart!.LastSessionLog);
        Assert.Equal(today, updatedPart.LastSessionDate);
    }

    [Fact]
    public async Task StudentDevelopmentDossier_ReturnsComposite360View()
    {
        var ctx = await GetFixtureContextAsync();
        using var adminClient = await CreateAuthenticatedAdminClientAsync();

        // Ahmad Dahlan has behaviour notes, discipline records, clubs, and achievements seeded in Stage 3
        var dossierResp = await adminClient.GetAsync($"/api/dev/students/{ctx.Student1Id}/dossier");
        Assert.Equal(HttpStatusCode.OK, dossierResp.StatusCode);

        var dossier = await dossierResp.Content.ReadFromJsonAsync<StudentDossierDto>(JsonOptions);
        Assert.NotNull(dossier);
        Assert.Equal(ctx.Student1Id, dossier!.StudentId);
        Assert.Equal(ctx.Student1Name, dossier.StudentName);

        // Assert 360-degree timeline collections are present
        Assert.NotEmpty(dossier.BehaviourNotes);
        Assert.NotEmpty(dossier.DisciplineRecords);
        Assert.NotEmpty(dossier.Extracurriculars);
        Assert.NotEmpty(dossier.Achievements);
        Assert.NotEmpty(dossier.Timeline);
    }
}
