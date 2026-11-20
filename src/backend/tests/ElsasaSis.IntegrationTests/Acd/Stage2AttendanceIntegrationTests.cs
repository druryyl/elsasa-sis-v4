using System.Net;
using System.Net.Http.Json;
using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Acd.Features.Attendance.GetSubjectAttendanceSheet;
using ElsasaSis.Modules.Acd.Features.Attendance.RecordSubjectAttendance;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests.Acd;

public sealed class Stage2AttendanceIntegrationTests : IntegrationTestBase
{
    public Stage2AttendanceIntegrationTests(IntegrationTestWebApplicationFactory factory)
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
    public async Task Attendance_SheetRetrieval_BatchUpsert_And_StatusUpdate_SucceedsWithoutDuplicateRows()
    {
        var context = await GetAttendanceFixtureContextAsync();

        using var teacherClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        // 1. Retrieve seeded attendance sheet for subject + study group 7A (yesterday's date)
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var sheetUrl = $"/api/acd/attendance?subjectId={context.SubjectId}&studyGroupId={context.StudyGroupId}&attendanceDate={yesterday:yyyy-MM-dd}&operationalPeriodId={context.OperationalPeriodId}";

        var getSheetResp = await teacherClient.GetAsync(sheetUrl);
        Assert.Equal(HttpStatusCode.OK, getSheetResp.StatusCode);

        var sheet = await getSheetResp.Content.ReadFromJsonAsync<SubjectAttendanceSheetResponse>(JsonOptions);
        Assert.NotNull(sheet);
        Assert.Equal(context.SubjectId, sheet!.SubjectId);
        Assert.Equal(context.StudyGroupId, sheet.StudyGroupId);
        Assert.Equal(yesterday, sheet.AttendanceDate);
        Assert.Equal(2, sheet.Items.Count);

        // 2. Batch upsert attendance records for today (initial: Ahmad PRESENT, Siti PRESENT)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var initialBatch = new RecordAttendanceRequest(
            context.SubjectId,
            context.StudyGroupId,
            today,
            context.OperationalPeriodId,
            [
                new RecordAttendanceItemRequest(context.Student1Id, "PRESENT", null),
                new RecordAttendanceItemRequest(context.Student2Id, "PRESENT", null)
            ]);

        var postResp1 = await teacherClient.PostAsJsonAsync("/api/acd/attendance", initialBatch);
        Assert.Equal(HttpStatusCode.OK, postResp1.StatusCode);

        var postResult1 = await postResp1.Content.ReadFromJsonAsync<RecordAttendanceResponse>(JsonOptions);
        Assert.NotNull(postResult1);
        Assert.Equal(2, postResult1!.RecordedCount);

        // Verify in DB that exactly 2 records exist for today
        await using (var connection = new SqlConnection(Factory.ConnectionString))
        {
            await connection.OpenAsync();
            var rowCountBeforeUpdate = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM [acd].[attendance_records] WHERE [subject_id] = @SubId AND [study_group_id] = @GrpId AND [attendance_date] = @Date",
                new { SubId = context.SubjectId, GrpId = context.StudyGroupId, Date = today.ToDateTime(TimeOnly.MinValue) });
            Assert.Equal(2, rowCountBeforeUpdate);
        }

        // 3. Update an existing student's status from PRESENT to SICK with notes
        var updateBatch = new RecordAttendanceRequest(
            context.SubjectId,
            context.StudyGroupId,
            today,
            context.OperationalPeriodId,
            [
                new RecordAttendanceItemRequest(context.Student1Id, "PRESENT", null),
                new RecordAttendanceItemRequest(context.Student2Id, "SICK", "Sakit demam tinggi")
            ]);

        var postResp2 = await teacherClient.PostAsJsonAsync("/api/acd/attendance", updateBatch);
        Assert.Equal(HttpStatusCode.OK, postResp2.StatusCode);

        // Verify upsert semantics: exactly 2 rows remain in acd.attendance_records (NO duplicates inserted)
        await using (var connection = new SqlConnection(Factory.ConnectionString))
        {
            await connection.OpenAsync();
            var rowCountAfterUpdate = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM [acd].[attendance_records] WHERE [subject_id] = @SubId AND [study_group_id] = @GrpId AND [attendance_date] = @Date",
                new { SubId = context.SubjectId, GrpId = context.StudyGroupId, Date = today.ToDateTime(TimeOnly.MinValue) });
            Assert.Equal(2, rowCountAfterUpdate);

            var updatedStudentRecord = await connection.QuerySingleAsync(
                "SELECT [status], [notes] FROM [acd].[attendance_records] WHERE [subject_id] = @SubId AND [student_id] = @StuId AND [attendance_date] = @Date",
                new { SubId = context.SubjectId, StuId = context.Student2Id, Date = today.ToDateTime(TimeOnly.MinValue) });
            Assert.Equal("SICK", (string)updatedStudentRecord.status);
            Assert.Equal("Sakit demam tinggi", (string)updatedStudentRecord.notes);
        }

        // Verify sheet projection reflects updated status and notes
        var todaySheetUrl = $"/api/acd/attendance?subjectId={context.SubjectId}&studyGroupId={context.StudyGroupId}&attendanceDate={today:yyyy-MM-dd}&operationalPeriodId={context.OperationalPeriodId}";
        var todaySheetResp = await teacherClient.GetAsync(todaySheetUrl);
        Assert.Equal(HttpStatusCode.OK, todaySheetResp.StatusCode);

        var todaySheet = await todaySheetResp.Content.ReadFromJsonAsync<SubjectAttendanceSheetResponse>(JsonOptions);
        Assert.NotNull(todaySheet);
        var sitiRecord = todaySheet!.Items.First(i => i.StudentId == context.Student2Id);
        Assert.Equal("SICK", sitiRecord.Status);
        Assert.Equal("Sakit demam tinggi", sitiRecord.Notes);
    }

    [Fact]
    public async Task AttendanceEndpoints_Anonymous_Return401Unauthorized()
    {
        var context = await GetAttendanceFixtureContextAsync();
        using var client = CreateAnonymousClient();

        var getResp = await client.GetAsync(
            $"/api/acd/attendance?subjectId={context.SubjectId}&studyGroupId={context.StudyGroupId}&attendanceDate=2026-11-20&operationalPeriodId={context.OperationalPeriodId}");
        Assert.Equal(HttpStatusCode.Unauthorized, getResp.StatusCode);

        var postResp = await client.PostAsJsonAsync("/api/acd/attendance", new RecordAttendanceRequest(
            context.SubjectId,
            context.StudyGroupId,
            new DateOnly(2026, 11, 20),
            context.OperationalPeriodId,
            []));
        Assert.Equal(HttpStatusCode.Unauthorized, postResp.StatusCode);
    }

    private async Task<AttendanceFixtureContext> GetAttendanceFixtureContextAsync()
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

        var student1Id = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[students] WHERE [nis] = @Nis",
            new { Nis = Stage2Seeder.DefaultStudent1Nis });

        var student2Id = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[students] WHERE [nis] = @Nis",
            new { Nis = Stage2Seeder.DefaultStudent2Nis });

        return new AttendanceFixtureContext(
            subjectId,
            studyGroupId,
            periodId,
            teacherId,
            student1Id,
            student2Id);
    }

    private sealed record AttendanceFixtureContext(
        Guid SubjectId,
        Guid StudyGroupId,
        Guid OperationalPeriodId,
        Guid TeacherId,
        Guid Student1Id,
        Guid Student2Id);
}
