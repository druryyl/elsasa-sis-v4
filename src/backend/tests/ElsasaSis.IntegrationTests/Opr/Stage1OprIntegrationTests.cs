using System.Net;
using System.Net.Http.Json;
using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Opr.Features.Announcements;
using ElsasaSis.Modules.Opr.Features.Announcements.CreateAnnouncement;
using ElsasaSis.Modules.Opr.Features.Announcements.UpdateAnnouncement;
using ElsasaSis.Modules.Opr.Features.CalendarEntries;
using ElsasaSis.Modules.Opr.Features.CalendarEntries.CreateCalendarEntry;
using ElsasaSis.Modules.Opr.Features.CalendarEntries.UpdateCalendarEntry;
using ElsasaSis.Modules.Opr.Features.Rooms;
using ElsasaSis.Modules.Opr.Features.Rooms.CreateRoom;
using ElsasaSis.Modules.Opr.Features.Rooms.UpdateRoom;
using ElsasaSis.SharedKernel.Pagination;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests.Opr;

public sealed class Stage1OprIntegrationTests : IntegrationTestBase
{
    public Stage1OprIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedStage1FixturesAsync();
    }

    private async Task<Guid> GetActiveAcademicYearIdAsync()
    {
        await using var connection = new SqlConnection(Factory.ConnectionString);
        return await connection.ExecuteScalarAsync<Guid>(
            "SELECT TOP (1) [id] FROM [opr].[academic_years] WHERE [status] = 'ACTIVE' ORDER BY [created_at] ASC;");
    }

    [Fact]
    public async Task CalendarEntries_Crud_And_OutOfBounds_And_DuplicateDate_Validation()
    {
        using var client = await CreateAuthenticatedAdminClientAsync();
        var academicYearId = await GetActiveAcademicYearIdAsync();

        // 1. Create calendar entry within academic year (2026-08-17)
        var createRequest = new CreateCalendarEntryRequest(
            academicYearId,
            new DateOnly(2026, 8, 17),
            "HOLIDAY",
            "Hari Kemerdekaan RI");

        var createResponse = await client.PostAsJsonAsync("/api/opr/calendar-entries", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CalendarEntryDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(new DateOnly(2026, 8, 17), created!.EntryDate);
        Assert.Equal("HOLIDAY", created.DayType);
        Assert.Equal("Hari Kemerdekaan RI", created.Description);

        // 2. BR-OPS-008: Duplicate date in same academic year returns 409 Conflict
        var duplicateResponse = await client.PostAsJsonAsync("/api/opr/calendar-entries", createRequest);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        // 3. BR-OPS-007: Out-of-bounds date (e.g. 2025-01-01 is prior to 2026-07-01 start date) returns 422 Unprocessable Entity
        var outOfBoundsRequest = new CreateCalendarEntryRequest(
            academicYearId,
            new DateOnly(2025, 1, 1),
            "HOLIDAY",
            "Out of Bounds Holiday");

        var outOfBoundsResponse = await client.PostAsJsonAsync("/api/opr/calendar-entries", outOfBoundsRequest);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, outOfBoundsResponse.StatusCode);

        // 4. Update calendar entry
        var updateRequest = new UpdateCalendarEntryRequest(
            new DateOnly(2026, 8, 17),
            "HOLIDAY",
            "Hari Kemerdekaan RI - Upacara Bendera");

        var updateResponse = await client.PutAsJsonAsync($"/api/opr/calendar-entries/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CalendarEntryDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Hari Kemerdekaan RI - Upacara Bendera", updated!.Description);

        // 5. Query calendar entries
        var getResponse = await client.GetAsync($"/api/opr/calendar-entries?academicYearId={academicYearId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var list = await getResponse.Content.ReadFromJsonAsync<PaginatedResponse<CalendarEntryDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.Contains(list!.Items, e => e.Id == created.Id);

        // 6. Delete calendar entry
        var deleteResponse = await client.DeleteAsync($"/api/opr/calendar-entries/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deleted from DB
        await using var connection = new SqlConnection(Factory.ConnectionString);
        var exists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [opr].[calendar_entries] WHERE [id] = @Id;",
            new { created.Id });
        Assert.Equal(0, exists);
    }

    [Fact]
    public async Task Rooms_Crud_And_DuplicateName_And_NonPositiveCapacity_Validation()
    {
        using var client = await CreateAuthenticatedAdminClientAsync();

        // 1. Create room
        var createRequest = new CreateRoomRequest("Lab Komputer 1", "LABORATORY", 35);
        var createResponse = await client.PostAsJsonAsync("/api/opr/rooms", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<RoomDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Lab Komputer 1", created!.Name);
        Assert.Equal("LABORATORY", created.RoomType);
        Assert.Equal(35, created.Capacity);

        // 2. Duplicate room name returns 409 Conflict
        var duplicateResponse = await client.PostAsJsonAsync("/api/opr/rooms", createRequest);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        // 3. Non-positive capacity returns 422 Unprocessable Entity
        var invalidCapacityRequest = new CreateRoomRequest("Lab Bahasa", "LABORATORY", 0);
        var invalidCapacityResponse = await client.PostAsJsonAsync("/api/opr/rooms", invalidCapacityRequest);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidCapacityResponse.StatusCode);

        // 4. Update room
        var updateRequest = new UpdateRoomRequest("Lab Komputer Multimedia", "LABORATORY", 40);
        var updateResponse = await client.PutAsJsonAsync($"/api/opr/rooms/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<RoomDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Lab Komputer Multimedia", updated!.Name);
        Assert.Equal(40, updated.Capacity);

        // 5. Query rooms list
        var listResponse = await client.GetAsync("/api/opr/rooms");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var rooms = await listResponse.Content.ReadFromJsonAsync<PaginatedResponse<RoomDto>>(JsonOptions);
        Assert.NotNull(rooms);
        Assert.Contains(rooms!.Items, r => r.Id == created.Id && r.Name == "Lab Komputer Multimedia");
    }

    [Fact]
    public async Task Announcements_Draft_Update_Publish_DuplicatePublish_And_RoleAudienceFiltering()
    {
        using var adminClient = await CreateAuthenticatedAdminClientAsync();

        // 1. Create 3 draft announcements with distinct target audiences
        var draftTeachersReq = new CreateAnnouncementRequest(null, "Rapat Dewan Guru", "Agenda rapat semester ganjil", "TEACHERS");
        var draftParentsReq = new CreateAnnouncementRequest(null, "Pertemuan Orang Tua Siswa", "Sosialisasi program sekolah", "PARENTS");
        var draftAllReq = new CreateAnnouncementRequest(null, "Upacara Hari Kemerdekaan", "Seluruh warga sekolah wajib hadir", "ALL");

        var teachersResp = await adminClient.PostAsJsonAsync("/api/opr/announcements", draftTeachersReq);
        Assert.Equal(HttpStatusCode.Created, teachersResp.StatusCode);
        var teachersAnn = await teachersResp.Content.ReadFromJsonAsync<AnnouncementDto>(JsonOptions);
        Assert.NotNull(teachersAnn);
        Assert.Equal("DRAFT", teachersAnn!.Status);

        var parentsResp = await adminClient.PostAsJsonAsync("/api/opr/announcements", draftParentsReq);
        Assert.Equal(HttpStatusCode.Created, parentsResp.StatusCode);
        var parentsAnn = await parentsResp.Content.ReadFromJsonAsync<AnnouncementDto>(JsonOptions);
        Assert.NotNull(parentsAnn);

        var allResp = await adminClient.PostAsJsonAsync("/api/opr/announcements", draftAllReq);
        Assert.Equal(HttpStatusCode.Created, allResp.StatusCode);
        var allAnn = await allResp.Content.ReadFromJsonAsync<AnnouncementDto>(JsonOptions);
        Assert.NotNull(allAnn);

        // 2. Update draft announcement
        var updateTeachersReq = new UpdateAnnouncementRequest("Rapat Dewan Guru Terjadwal", "Agenda rapat semester ganjil ruang guru", "TEACHERS");
        var updateResp = await adminClient.PutAsJsonAsync($"/api/opr/announcements/{teachersAnn.Id}", updateTeachersReq);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updatedTeachersAnn = await updateResp.Content.ReadFromJsonAsync<AnnouncementDto>(JsonOptions);
        Assert.Equal("Rapat Dewan Guru Terjadwal", updatedTeachersAnn!.Title);

        // 3. Publish all 3 announcements
        var pubTeachersResp = await adminClient.PostAsync($"/api/opr/announcements/{teachersAnn.Id}/publish", null);
        Assert.Equal(HttpStatusCode.OK, pubTeachersResp.StatusCode);
        var publishedTeachers = await pubTeachersResp.Content.ReadFromJsonAsync<AnnouncementDto>(JsonOptions);
        Assert.Equal("PUBLISHED", publishedTeachers!.Status);

        var pubParentsResp = await adminClient.PostAsync($"/api/opr/announcements/{parentsAnn!.Id}/publish", null);
        Assert.Equal(HttpStatusCode.OK, pubParentsResp.StatusCode);

        var pubAllResp = await adminClient.PostAsync($"/api/opr/announcements/{allAnn!.Id}/publish", null);
        Assert.Equal(HttpStatusCode.OK, pubAllResp.StatusCode);

        // 4. Duplicate publish on already published announcement returns 409 Conflict
        var dupPublishResp = await adminClient.PostAsync($"/api/opr/announcements/{teachersAnn.Id}/publish", null);
        Assert.Equal(HttpStatusCode.Conflict, dupPublishResp.StatusCode);

        // 5. Verify teacher.budi audience filtering (strictly scoped to ALL and TEACHERS)
        using var teacherClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultTeacherUsername,
            Stage1Seeder.DefaultTeacherPassword);

        var teacherAnnResp = await teacherClient.GetAsync("/api/opr/announcements");
        Assert.Equal(HttpStatusCode.OK, teacherAnnResp.StatusCode);
        var teacherAnnList = await teacherAnnResp.Content.ReadFromJsonAsync<PaginatedResponse<AnnouncementDto>>(JsonOptions);
        Assert.NotNull(teacherAnnList);

        // Teacher CAN see TEACHERS and ALL announcements
        Assert.Contains(teacherAnnList!.Items, a => a.Id == teachersAnn.Id);
        Assert.Contains(teacherAnnList.Items, a => a.Id == allAnn.Id);
        // Teacher CANNOT see PARENTS announcement
        Assert.DoesNotContain(teacherAnnList.Items, a => a.Id == parentsAnn.Id);

        // 6. Verify principal audience filtering (elevated staff role: can view all published announcements across audiences)
        using var principalClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultPrincipalUsername,
            Stage1Seeder.DefaultPrincipalPassword);

        var principalAnnResp = await principalClient.GetAsync("/api/opr/announcements");
        Assert.Equal(HttpStatusCode.OK, principalAnnResp.StatusCode);
        var principalAnnList = await principalAnnResp.Content.ReadFromJsonAsync<PaginatedResponse<AnnouncementDto>>(JsonOptions);
        Assert.NotNull(principalAnnList);

        Assert.Contains(principalAnnList!.Items, a => a.Id == teachersAnn.Id);
        Assert.Contains(principalAnnList.Items, a => a.Id == allAnn.Id);
        Assert.Contains(principalAnnList.Items, a => a.Id == parentsAnn.Id);
    }
}
