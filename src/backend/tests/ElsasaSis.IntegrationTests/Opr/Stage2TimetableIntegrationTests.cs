using System.Net;
using System.Net.Http.Json;
using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Opr.Features.Timetables;
using ElsasaSis.Modules.Opr.Features.Timetables.ConfigureStudyGroupTimetable;
using ElsasaSis.Modules.Opr.Features.Timetables.ManageSmkBlockRotations;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests.Opr;

public sealed class Stage2TimetableIntegrationTests : IntegrationTestBase
{
    public Stage2TimetableIntegrationTests(IntegrationTestWebApplicationFactory factory)
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
    public async Task StudyGroupTimetable_Lifecycle_Conflicts_And_TeacherPersonalSchedule_Succeed()
    {
        var context = await GetTimetableFixtureContextAsync();

        using var adminClient = await CreateAuthenticatedAdminClientAsync();
        using var teacherClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        // 1. Admin configures study group 7A timetable slot (Tuesday Period 3)
        var slotRequest7A = new ConfigureStudyGroupTimetableRequest(
            context.OperationalPeriodId,
            "REGULAR",
            DayOfWeek: 2, // Tuesday
            PeriodSlot: 3,
            StartTime: new TimeOnly(9, 0),
            EndTime: new TimeOnly(9, 45),
            SubjectId: context.SubjectId,
            TeacherId: context.TeacherId,
            RoomId: context.RoomId);

        var configureResp7A = await adminClient.PostAsJsonAsync(
            $"/api/opr/study-groups/{context.StudyGroupId7A}/timetables",
            slotRequest7A);

        Assert.True(
            configureResp7A.StatusCode == HttpStatusCode.OK || configureResp7A.StatusCode == HttpStatusCode.Created,
            $"Expected 200 or 201 but got {configureResp7A.StatusCode}");

        var slot7A = await configureResp7A.Content.ReadFromJsonAsync<StudyGroupScheduleDto>(JsonOptions);
        Assert.NotNull(slot7A);
        Assert.Equal(context.StudyGroupId7A, slot7A!.StudyGroupId);
        Assert.Equal(2, slot7A.DayOfWeek);
        Assert.Equal(3, slot7A.PeriodSlot);
        Assert.Equal(context.TeacherId, slot7A.TeacherId);
        Assert.Equal(context.RoomId, slot7A.RoomId);

        // 2. Query timetable for 7A: teacherConflict = false and roomConflict = false
        var queryResp7A = await adminClient.GetAsync(
            $"/api/opr/study-groups/{context.StudyGroupId7A}/timetables?periodId={context.OperationalPeriodId}&scheduleType=REGULAR");
        Assert.Equal(HttpStatusCode.OK, queryResp7A.StatusCode);

        var slots7A = await queryResp7A.Content.ReadFromJsonAsync<List<StudyGroupTimetableSlotDto>>(JsonOptions);
        Assert.NotNull(slots7A);
        var queriedSlot7A = slots7A!.FirstOrDefault(s => s.DayOfWeek == 2 && s.PeriodSlot == 3);
        Assert.NotNull(queriedSlot7A);
        Assert.False(queriedSlot7A!.TeacherConflict, "Expected teacherConflict to be false before overlap");
        Assert.False(queriedSlot7A.RoomConflict, "Expected roomConflict to be false before overlap");

        // 3. Configure overlapping slot in another study group (7B) with the same teacher, room, day, and period slot
        var slotRequest7B = new ConfigureStudyGroupTimetableRequest(
            context.OperationalPeriodId,
            "REGULAR",
            DayOfWeek: 2,
            PeriodSlot: 3,
            StartTime: new TimeOnly(9, 0),
            EndTime: new TimeOnly(9, 45),
            SubjectId: context.SubjectId,
            TeacherId: context.TeacherId,
            RoomId: context.RoomId);

        var configureResp7B = await adminClient.PostAsJsonAsync(
            $"/api/opr/study-groups/{context.StudyGroupId7B}/timetables",
            slotRequest7B);

        // Conflict must NOT block save
        Assert.True(
            configureResp7B.StatusCode == HttpStatusCode.OK || configureResp7B.StatusCode == HttpStatusCode.Created,
            $"Expected save not to be blocked by conflict, got {configureResp7B.StatusCode}");

        var slot7B = await configureResp7B.Content.ReadFromJsonAsync<StudyGroupScheduleDto>(JsonOptions);
        Assert.NotNull(slot7B);

        // Re-query 7A and query 7B: both must now reflect conflict flags = true
        var reQueryResp7A = await adminClient.GetAsync(
            $"/api/opr/study-groups/{context.StudyGroupId7A}/timetables?periodId={context.OperationalPeriodId}&scheduleType=REGULAR");
        var reQueriedSlots7A = await reQueryResp7A.Content.ReadFromJsonAsync<List<StudyGroupTimetableSlotDto>>(JsonOptions);
        var conflictingSlot7A = reQueriedSlots7A!.First(s => s.DayOfWeek == 2 && s.PeriodSlot == 3);
        Assert.True(conflictingSlot7A.TeacherConflict, "Expected teacherConflict to be true on 7A");
        Assert.True(conflictingSlot7A.RoomConflict, "Expected roomConflict to be true on 7A");

        var queryResp7B = await adminClient.GetAsync(
            $"/api/opr/study-groups/{context.StudyGroupId7B}/timetables?periodId={context.OperationalPeriodId}&scheduleType=REGULAR");
        var queriedSlots7B = await queryResp7B.Content.ReadFromJsonAsync<List<StudyGroupTimetableSlotDto>>(JsonOptions);
        var conflictingSlot7B = queriedSlots7B!.First(s => s.DayOfWeek == 2 && s.PeriodSlot == 3);
        Assert.True(conflictingSlot7B.TeacherConflict, "Expected teacherConflict to be true on 7B");
        Assert.True(conflictingSlot7B.RoomConflict, "Expected roomConflict to be true on 7B");

        // 4. Delete overlapping slot in 7B -> returns 204 No Content
        var deleteResp7B = await adminClient.DeleteAsync(
            $"/api/opr/study-groups/{context.StudyGroupId7B}/timetables/{slot7B!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp7B.StatusCode);

        // Verify conflict flag on 7A reverts to false
        var postDeleteQuery7A = await adminClient.GetAsync(
            $"/api/opr/study-groups/{context.StudyGroupId7A}/timetables?periodId={context.OperationalPeriodId}&scheduleType=REGULAR");
        var postDeleteSlots7A = await postDeleteQuery7A.Content.ReadFromJsonAsync<List<StudyGroupTimetableSlotDto>>(JsonOptions);
        var resolvedSlot7A = postDeleteSlots7A!.First(s => s.DayOfWeek == 2 && s.PeriodSlot == 3);
        Assert.False(resolvedSlot7A.TeacherConflict, "Expected teacherConflict to revert to false after slot deletion");
        Assert.False(resolvedSlot7A.RoomConflict, "Expected roomConflict to revert to false after slot deletion");

        // Delete slot on 7A as cleanup
        var deleteResp7A = await adminClient.DeleteAsync(
            $"/api/opr/study-groups/{context.StudyGroupId7A}/timetables/{slot7A!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp7A.StatusCode);

        // 5. SMK Block date range CRUD
        var blockRequest = new CreateSmkBlockRequest(
            context.OperationalPeriodId,
            "VOCATIONAL",
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        var createBlockResp = await adminClient.PostAsJsonAsync(
            $"/api/opr/study-groups/{context.StudyGroupId7A}/blocks",
            blockRequest);
        Assert.True(
            createBlockResp.StatusCode == HttpStatusCode.OK || createBlockResp.StatusCode == HttpStatusCode.Created,
            $"Expected 200 or 201 for block creation, got {createBlockResp.StatusCode}");

        var createdBlock = await createBlockResp.Content.ReadFromJsonAsync<StudyGroupTimetableBlockDto>(JsonOptions);
        Assert.NotNull(createdBlock);
        Assert.Equal(context.StudyGroupId7A, createdBlock!.StudyGroupId);
        Assert.Equal("VOCATIONAL", createdBlock.ScheduleType);
        Assert.Equal(new DateOnly(2026, 9, 1), createdBlock.StartDate);
        Assert.Equal(new DateOnly(2026, 9, 30), createdBlock.EndDate);

        // GET blocks
        var getBlocksResp = await adminClient.GetAsync(
            $"/api/opr/study-groups/{context.StudyGroupId7A}/blocks?periodId={context.OperationalPeriodId}");
        Assert.Equal(HttpStatusCode.OK, getBlocksResp.StatusCode);

        var blocksList = await getBlocksResp.Content.ReadFromJsonAsync<List<StudyGroupTimetableBlockDto>>(JsonOptions);
        Assert.NotNull(blocksList);
        Assert.Contains(blocksList!, b => b.Id == createdBlock.Id);

        // DELETE block -> 204
        var deleteBlockResp = await adminClient.DeleteAsync(
            $"/api/opr/study-groups/{context.StudyGroupId7A}/blocks/{createdBlock.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteBlockResp.StatusCode);

        var getBlocksAfterDeleteResp = await adminClient.GetAsync(
            $"/api/opr/study-groups/{context.StudyGroupId7A}/blocks?periodId={context.OperationalPeriodId}");
        var blocksAfterDelete = await getBlocksAfterDeleteResp.Content.ReadFromJsonAsync<List<StudyGroupTimetableBlockDto>>(JsonOptions);
        Assert.DoesNotContain(blocksAfterDelete!, b => b.Id == createdBlock.Id);

        // 6. Teacher personal schedule query returns only assigned classes for authenticated teacher
        var teacherScheduleResp = await teacherClient.GetAsync(
            $"/api/opr/teacher-schedule?periodId={context.OperationalPeriodId}");
        Assert.Equal(HttpStatusCode.OK, teacherScheduleResp.StatusCode);

        var teacherSchedule = await teacherScheduleResp.Content.ReadFromJsonAsync<TeacherPersonalScheduleResponse>(JsonOptions);
        Assert.NotNull(teacherSchedule);
        Assert.Equal(context.TeacherId, teacherSchedule!.TeacherId);
        Assert.Equal(context.OperationalPeriodId, teacherSchedule.OperationalPeriodId);

        // Seeded 7A Monday slots 1 & 2 belong to Budi
        var regularGroup = teacherSchedule.ScheduleGroups.FirstOrDefault(g => g.ScheduleType == "REGULAR");
        Assert.NotNull(regularGroup);
        Assert.Contains(regularGroup!.Slots, s => s.DayOfWeek == 1 && s.PeriodSlot == 1 && s.TeacherId == context.TeacherId);
        Assert.Contains(regularGroup.Slots, s => s.DayOfWeek == 1 && s.PeriodSlot == 2 && s.TeacherId == context.TeacherId);
        Assert.All(regularGroup.Slots, s => Assert.Equal(context.TeacherId, s.TeacherId));
    }

    [Fact]
    public async Task TimetableEndpoints_Anonymous_Return401Unauthorized()
    {
        var context = await GetTimetableFixtureContextAsync();
        using var client = CreateAnonymousClient();

        var queryResp = await client.GetAsync(
            $"/api/opr/study-groups/{context.StudyGroupId7A}/timetables?periodId={context.OperationalPeriodId}");
        Assert.Equal(HttpStatusCode.Unauthorized, queryResp.StatusCode);

        var teacherScheduleResp = await client.GetAsync(
            $"/api/opr/teacher-schedule?periodId={context.OperationalPeriodId}");
        Assert.Equal(HttpStatusCode.Unauthorized, teacherScheduleResp.StatusCode);
    }

    private async Task<TimetableFixtureContext> GetTimetableFixtureContextAsync()
    {
        await using var connection = new SqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();

        var periodId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [opr].[operational_periods] WHERE [status] = 'ACTIVE'");

        var studyGroupId7A = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[study_groups] WHERE [name] = '7A'");

        var studyGroupId7B = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[study_groups] WHERE [name] = '7B'");

        var teacherId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [tch].[teachers] WHERE [nip] = @Nip",
            new { Nip = Stage1Seeder.DefaultTeacherNip });

        var subjectId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [acd].[subjects] WHERE [code] = 'MTK' OR [name] = 'Matematika'");

        var roomId = await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [opr].[rooms] WHERE [name] = 'Classroom 7A'");

        return new TimetableFixtureContext(
            periodId,
            studyGroupId7A,
            studyGroupId7B,
            teacherId,
            subjectId,
            roomId);
    }

    private sealed record TimetableFixtureContext(
        Guid OperationalPeriodId,
        Guid StudyGroupId7A,
        Guid StudyGroupId7B,
        Guid TeacherId,
        Guid SubjectId,
        Guid RoomId);
}
