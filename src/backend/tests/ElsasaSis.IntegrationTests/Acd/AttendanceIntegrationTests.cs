using System.Net;
using System.Net.Http.Json;
using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Acd.Features.Attendance.GetSubjectAttendanceSheet;
using ElsasaSis.Modules.Acd.Features.Attendance.RecordSubjectAttendance;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests.Acd;

public sealed class AttendanceIntegrationTests : IntegrationTestBase
{
    public AttendanceIntegrationTests(IntegrationTestWebApplicationFactory factory)
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
    public async Task GetSubjectAttendanceSheet_WhenSeededRecordsExist_ReturnsRosterWithAttendanceStatuses()
    {
        var (subjectId, studyGroupId, periodId, teacherId) = await GetFixtureContextAsync();

        using var client = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var url = $"/api/acd/attendance?subjectId={subjectId}&studyGroupId={studyGroupId}&attendanceDate={yesterday:yyyy-MM-dd}&operationalPeriodId={periodId}";

        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sheet = await response.Content.ReadFromJsonAsync<SubjectAttendanceSheetResponse>(JsonOptions);
        Assert.NotNull(sheet);
        Assert.Equal(subjectId, sheet!.SubjectId);
        Assert.Equal(studyGroupId, sheet.StudyGroupId);
        Assert.Equal(yesterday, sheet.AttendanceDate);
        Assert.Equal(periodId, sheet.OperationalPeriodId);

        // Seeded 7A has 2 placed students with active participations
        Assert.Equal(2, sheet.Items.Count);

        var ahmad = sheet.Items.First(s => s.StudentName == "Ahmad Dahlan");
        Assert.Equal("PRESENT", ahmad.Status);
        Assert.Equal(teacherId, ahmad.RecordedByTeacherId);

        var siti = sheet.Items.First(s => s.StudentName == "Siti Nurhaliza");
        Assert.Equal("PRESENT", siti.Status);
        Assert.Equal(teacherId, siti.RecordedByTeacherId);
    }

    [Fact]
    public async Task RecordSubjectAttendance_BatchUpsert_SavesAndUpdatesSheetDeterministically()
    {
        var (subjectId, studyGroupId, periodId, teacherId) = await GetFixtureContextAsync();

        using var teacherClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Initial batch insert for today: Ahmad PRESENT, Siti SICK
        var request = new RecordAttendanceRequest(
            subjectId,
            studyGroupId,
            today,
            periodId,
            [
                new RecordAttendanceItemRequest(Stage2Seeder.DefaultStudent1Id, "PRESENT", null),
                new RecordAttendanceItemRequest(Stage2Seeder.DefaultStudent2Id, "SICK", "Sakit demam")
            ]);

        var postResponse = await teacherClient.PostAsJsonAsync("/api/acd/attendance", request);
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var recordResult = await postResponse.Content.ReadFromJsonAsync<RecordAttendanceResponse>(JsonOptions);
        Assert.NotNull(recordResult);
        Assert.Equal(2, recordResult!.RecordedCount);

        // 2. Query today's sheet
        var sheetUrl = $"/api/acd/attendance?subjectId={subjectId}&studyGroupId={studyGroupId}&attendanceDate={today:yyyy-MM-dd}&operationalPeriodId={periodId}";
        var sheetResponse = await teacherClient.GetAsync(sheetUrl);
        Assert.Equal(HttpStatusCode.OK, sheetResponse.StatusCode);

        var sheet = await sheetResponse.Content.ReadFromJsonAsync<SubjectAttendanceSheetResponse>(JsonOptions);
        Assert.NotNull(sheet);
        Assert.Equal(2, sheet!.Items.Count);

        var ahmad = sheet.Items.First(s => s.StudentId == Stage2Seeder.DefaultStudent1Id);
        Assert.Equal("PRESENT", ahmad.Status);
        Assert.Null(ahmad.Notes);
        Assert.Equal(teacherId, ahmad.RecordedByTeacherId);
        Assert.Equal(Stage1Seeder.DefaultTeacherDisplayName, ahmad.RecordedByTeacherName);

        var siti = sheet.Items.First(s => s.StudentId == Stage2Seeder.DefaultStudent2Id);
        Assert.Equal("SICK", siti.Status);
        Assert.Equal("Sakit demam", siti.Notes);
        Assert.Equal(teacherId, siti.RecordedByTeacherId);

        // 3. Update Siti to EXCUSED (batch upsert idempotency)
        var updateRequest = new RecordAttendanceRequest(
            subjectId,
            studyGroupId,
            today,
            periodId,
            [
                new RecordAttendanceItemRequest(Stage2Seeder.DefaultStudent1Id, "PRESENT", null),
                new RecordAttendanceItemRequest(Stage2Seeder.DefaultStudent2Id, "EXCUSED", "Izin keluarga")
            ]);

        var updateResponse = await teacherClient.PostAsJsonAsync("/api/acd/attendance", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Re-query sheet and verify update
        var updatedSheetResponse = await teacherClient.GetAsync(sheetUrl);
        var updatedSheet = await updatedSheetResponse.Content.ReadFromJsonAsync<SubjectAttendanceSheetResponse>(JsonOptions);
        Assert.NotNull(updatedSheet);
        Assert.Equal(2, updatedSheet!.Items.Count);

        var sitiUpdated = updatedSheet.Items.First(s => s.StudentId == Stage2Seeder.DefaultStudent2Id);
        Assert.Equal("EXCUSED", sitiUpdated.Status);
        Assert.Equal("Izin keluarga", sitiUpdated.Notes);
    }

    [Fact]
    public async Task AttendanceEndpoints_WhenAnonymous_Return401Unauthorized()
    {
        var (subjectId, studyGroupId, periodId, _) = await GetFixtureContextAsync();

        using var client = CreateAnonymousClient();

        var getResponse = await client.GetAsync(
            $"/api/acd/attendance?subjectId={subjectId}&studyGroupId={studyGroupId}&attendanceDate=2026-11-19&operationalPeriodId={periodId}");
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);

        var postResponse = await client.PostAsJsonAsync("/api/acd/attendance", new RecordAttendanceRequest(
            subjectId,
            studyGroupId,
            new DateOnly(2026, 11, 19),
            periodId,
            []));
        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);
    }

    private async Task<(Guid SubjectId, Guid StudyGroupId, Guid PeriodId, Guid TeacherId)> GetFixtureContextAsync()
    {
        await using var connection = new SqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();

        var subjectId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [acd].[subjects] WHERE [code] = 'MTK' OR [name] = 'Matematika'");

        var studyGroupId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[study_groups] WHERE [name] = '7A'");

        var periodId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [opr].[operational_periods] WHERE [status] = 'ACTIVE'");

        var teacherId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [tch].[teachers] WHERE [nip] = @Nip",
            new { Nip = Stage1Seeder.DefaultTeacherNip });

        return (subjectId, studyGroupId, periodId, teacherId);
    }
}

