using System.Net;
using System.Net.Http.Json;
using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Acd.Features.ReportCards.GetReportCardById;
using ElsasaSis.Modules.Acd.Features.ReportCards.GetStudentReportCards;
using ElsasaSis.Modules.Fin.Features.Payments;
using ElsasaSis.Modules.Opr.Features.Announcements;
using ElsasaSis.Modules.Opr.Features.Announcements.CreateAnnouncement;
using ElsasaSis.Modules.Opr.Features.Timetables.GetPersonalStudentTimetable;
using ElsasaSis.Modules.Std.Features.Students.GetMyChildren;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Pagination;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests;

public sealed class Stage4ConsumerPortalsIntegrationTests : IntegrationTestBase
{
    public Stage4ConsumerPortalsIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedStage4FixturesAsync();
    }

    [Fact]
    public async Task ParentPortalFlow_LinkedChildren_ReportCards_Statements_Receipts_AudienceBulletins_AndForbiddenGuards()
    {
        // 1. Authenticate as parent.hendra
        using var parentClient = await CreateAuthenticatedClientAsync(
            Stage4Seeder.DefaultParentUsername,
            Stage4Seeder.DefaultParentPassword);

        // 2. GET /api/std/students/my-children returns 2 linked children (Ahmad Dahlan and Siti Nurhaliza)
        var childrenResponse = await parentClient.GetAsync("/api/std/students/my-children");
        Assert.Equal(HttpStatusCode.OK, childrenResponse.StatusCode);

        var children = await childrenResponse.Content.ReadFromJsonAsync<IReadOnlyList<LinkedChildDto>>(JsonOptions);
        Assert.NotNull(children);
        Assert.Equal(2, children!.Count);
        Assert.Contains(children, c => c.StudentId == Stage4Seeder.DefaultStudentAhmadId && c.FullName == "Ahmad Dahlan");
        Assert.Contains(children, c => c.StudentId == Stage4Seeder.DefaultStudentSitiId && c.FullName == "Siti Nurhaliza");

        // 3. GET /api/acd/report-cards/student/{studentId}
        // Ahmad Dahlan has 1 FINALIZED report card -> returns 1
        var ahmadCardsResponse = await parentClient.GetAsync($"/api/acd/report-cards/student/{Stage4Seeder.DefaultStudentAhmadId}");
        Assert.Equal(HttpStatusCode.OK, ahmadCardsResponse.StatusCode);
        var ahmadCards = await ahmadCardsResponse.Content.ReadFromJsonAsync<IReadOnlyList<StudentReportCardSummaryDto>>(JsonOptions);
        Assert.NotNull(ahmadCards);
        Assert.Single(ahmadCards!);
        Assert.Equal("FINALIZED", ahmadCards![0].Status);

        // Siti Nurhaliza has 1 DRAFT report card -> draft cards are excluded, returns 0
        var sitiCardsResponse = await parentClient.GetAsync($"/api/acd/report-cards/student/{Stage4Seeder.DefaultStudentSitiId}");
        Assert.Equal(HttpStatusCode.OK, sitiCardsResponse.StatusCode);
        var sitiCards = await sitiCardsResponse.Content.ReadFromJsonAsync<IReadOnlyList<StudentReportCardSummaryDto>>(JsonOptions);
        Assert.NotNull(sitiCards);
        Assert.Empty(sitiCards!);

        // 4. GET /api/acd/report-cards/{draftId} returns 404 Not Found for parent
        var draftCardResponse = await parentClient.GetAsync($"/api/acd/report-cards/{Stage3Seeder.DefaultReportCardSitiId}");
        Assert.Equal(HttpStatusCode.NotFound, draftCardResponse.StatusCode);

        // Finalized card returns 200 OK for parent
        var finalizedCardResponse = await parentClient.GetAsync($"/api/acd/report-cards/{Stage3Seeder.DefaultReportCardAhmadId}");
        Assert.Equal(HttpStatusCode.OK, finalizedCardResponse.StatusCode);
        var finalizedCard = await finalizedCardResponse.Content.ReadFromJsonAsync<ReportCardDetailDto>(JsonOptions);
        Assert.NotNull(finalizedCard);
        Assert.Equal("FINALIZED", finalizedCard!.Status);

        // 5. GET /api/fin/students/{studentId}/statement returns fee ledger and advance deposit balance
        var statementResponse = await parentClient.GetAsync($"/api/fin/students/{Stage4Seeder.DefaultStudentAhmadId}/statement");
        Assert.Equal(HttpStatusCode.OK, statementResponse.StatusCode);
        var statement = await statementResponse.Content.ReadFromJsonAsync<StudentStatementDto>(JsonOptions);
        Assert.NotNull(statement);
        Assert.Equal(Stage4Seeder.DefaultStudentAhmadId, statement!.StudentId);
        Assert.NotNull(statement.Charges);
        Assert.NotNull(statement.Receipts);
        Assert.True(statement.TotalBilled >= 0);
        Assert.True(statement.TotalDepositBalance >= 0);

        // 6. GET /api/fin/payments/{receiptId} returns receipt breakdown
        var receiptResponse = await parentClient.GetAsync($"/api/fin/payments/{Stage3Seeder.DefaultReceipt1Id}");
        Assert.Equal(HttpStatusCode.OK, receiptResponse.StatusCode);
        var receipt = await receiptResponse.Content.ReadFromJsonAsync<PaymentReceiptDetailDto>(JsonOptions);
        Assert.NotNull(receipt);
        Assert.Equal(Stage3Seeder.DefaultReceipt1Id, receipt!.Id);
        Assert.Equal(Stage4Seeder.DefaultStudentAhmadId, receipt.StudentId);
        Assert.NotNull(receipt.Allocations);

        // 7. Querying an unlinked student ID on any endpoint returns 403 Forbidden
        var unlinkedStudentId = Guid.Parse("01A1F11A-9999-9999-8000-000000000099");

        // Report cards by unlinked student -> 403
        var unlinkedReportCardsResponse = await parentClient.GetAsync($"/api/acd/report-cards/student/{unlinkedStudentId}");
        Assert.Equal(HttpStatusCode.Forbidden, unlinkedReportCardsResponse.StatusCode);

        // Statement by unlinked student -> 403
        var unlinkedStatementResponse = await parentClient.GetAsync($"/api/fin/students/{unlinkedStudentId}/statement");
        Assert.Equal(HttpStatusCode.Forbidden, unlinkedStatementResponse.StatusCode);

        // Insert unlinked student and a payment receipt for that unlinked student -> query receipt -> 403
        var unlinkedReceiptId = Guid.Parse("01A1F11A-9998-9998-8000-000000000098");
        await using (var connection = new SqlConnection(Factory.ConnectionString))
        {
            await connection.OpenAsync();
            var academicYearId = await connection.ExecuteScalarAsync<Guid>("SELECT TOP (1) id FROM opr.academic_years WHERE status = 'ACTIVE'");

            await connection.ExecuteAsync("""
                INSERT INTO std.students (id, candidate_id, nis, full_name, date_of_birth, gender, enrollment_date, status, created_at, updated_at)
                VALUES (@StudentId, @CandidateId, '999999999', 'Unlinked Student', '2010-01-01', 'M', '2026-07-01', 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME());

                INSERT INTO fin.payment_receipts (
                    id, receipt_number, academic_year_id, academic_year_name, student_id, student_name, student_nis,
                    payment_date, payment_method, total_amount, allocated_amount, unallocated_amount,
                    received_by_id, received_by_name, created_at, updated_at)
                VALUES (
                    @ReceiptId, 'REC/2026/07/9999', @AcademicYearId, '2026/2027', @StudentId, 'Unlinked Student', '999999999',
                    CAST(SYSUTCDATETIME() AS DATE), 'CASH', 500000, 500000, 0,
                    @ReceivedById, 'System Admin', SYSUTCDATETIME(), SYSUTCDATETIME());
                """, new
            {
                StudentId = unlinkedStudentId,
                CandidateId = Guid.NewGuid(),
                ReceiptId = unlinkedReceiptId,
                AcademicYearId = academicYearId,
                ReceivedById = Guid.NewGuid()
            });
        }

        var unlinkedReceiptResponse = await parentClient.GetAsync($"/api/fin/payments/{unlinkedReceiptId}");
        Assert.Equal(HttpStatusCode.Forbidden, unlinkedReceiptResponse.StatusCode);

        // 8. Bulletins: GET /api/opr/announcements returns bulletins with target audience ALL and PARENTS, excluding STUDENTS and TEACHERS
        using var adminClient = await CreateAuthenticatedAdminClientAsync();

        // Create 4 announcements for different audiences and publish them
        var audienceTargets = new[] { "ALL", "PARENTS", "STUDENTS", "TEACHERS" };
        foreach (var target in audienceTargets)
        {
            var createResp = await adminClient.PostAsJsonAsync("/api/opr/announcements", new CreateAnnouncementRequest(
                null,
                $"Announcement for {target}",
                $"Content for audience {target}",
                target));
            Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
            var createdAnn = await createResp.Content.ReadFromJsonAsync<AnnouncementDto>(JsonOptions);

            var publishResp = await adminClient.PostAsync($"/api/opr/announcements/{createdAnn!.Id}/publish", null);
            Assert.Equal(HttpStatusCode.OK, publishResp.StatusCode);
        }

        var parentAnnResponse = await parentClient.GetAsync("/api/opr/announcements");
        Assert.Equal(HttpStatusCode.OK, parentAnnResponse.StatusCode);
        var parentAnnouncements = await parentAnnResponse.Content.ReadFromJsonAsync<PaginatedResponse<AnnouncementDto>>(JsonOptions);
        Assert.NotNull(parentAnnouncements);
        Assert.NotEmpty(parentAnnouncements!.Items);

        // Assert all returned items are strictly ALL or PARENTS, and zero STUDENTS or TEACHERS
        foreach (var item in parentAnnouncements.Items)
        {
            Assert.Contains(item.TargetAudience, new[] { "ALL", "PARENTS" });
            Assert.DoesNotContain(item.TargetAudience, new[] { "STUDENTS", "TEACHERS" });
        }
    }

    [Fact]
    public async Task StudentPortalFlow_Timetable_And_AudienceScoping()
    {
        // 1. Authenticate as student.ahmad
        using var studentClient = await CreateAuthenticatedClientAsync(
            Stage4Seeder.DefaultStudentAhmadUsername,
            Stage4Seeder.DefaultStudentAhmadPassword);

        // 2. GET /api/opr/timetables/my returns student info, active SMK cycle badge, Today's Agenda, and Weekly Timetable
        var timetableResponse = await studentClient.GetAsync("/api/opr/timetables/my");
        Assert.Equal(HttpStatusCode.OK, timetableResponse.StatusCode);

        var schedule = await timetableResponse.Content.ReadFromJsonAsync<StudentPersonalScheduleResponse>(JsonOptions);
        Assert.NotNull(schedule);

        // Student Info
        Assert.NotNull(schedule!.StudentInfo);
        Assert.Equal(Stage4Seeder.DefaultStudentAhmadId, schedule.StudentInfo.StudentId);
        Assert.Equal("Ahmad Dahlan", schedule.StudentInfo.FullName);
        Assert.Equal("7A", schedule.StudentInfo.StudyGroupName);

        // Active SMK cycle badge
        Assert.NotNull(schedule.ActiveCycle);
        Assert.False(string.IsNullOrWhiteSpace(schedule.ActiveCycle.ScheduleType));

        // Agenda & Weekly Timetable
        Assert.NotNull(schedule.TodayAgenda);
        Assert.NotNull(schedule.WeeklySchedule);
        Assert.NotEmpty(schedule.WeeklySchedule);
        Assert.Contains(schedule.WeeklySchedule, s => s.DayOfWeek == "Monday");

        // 3. Bulletins: GET /api/opr/announcements returns bulletins with target audience ALL and STUDENTS, excluding PARENTS and TEACHERS
        using var adminClient = await CreateAuthenticatedAdminClientAsync();

        var audienceTargets = new[] { "ALL", "PARENTS", "STUDENTS", "TEACHERS" };
        foreach (var target in audienceTargets)
        {
            var createResp = await adminClient.PostAsJsonAsync("/api/opr/announcements", new CreateAnnouncementRequest(
                null,
                $"Student Test Bulletin for {target}",
                $"Content for audience {target}",
                target));
            Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
            var createdAnn = await createResp.Content.ReadFromJsonAsync<AnnouncementDto>(JsonOptions);

            var publishResp = await adminClient.PostAsync($"/api/opr/announcements/{createdAnn!.Id}/publish", null);
            Assert.Equal(HttpStatusCode.OK, publishResp.StatusCode);
        }

        var studentAnnResponse = await studentClient.GetAsync("/api/opr/announcements");
        Assert.Equal(HttpStatusCode.OK, studentAnnResponse.StatusCode);
        var studentAnnouncements = await studentAnnResponse.Content.ReadFromJsonAsync<PaginatedResponse<AnnouncementDto>>(JsonOptions);
        Assert.NotNull(studentAnnouncements);
        Assert.NotEmpty(studentAnnouncements!.Items);

        // Assert all returned items are strictly ALL or STUDENTS, and zero PARENTS or TEACHERS
        foreach (var item in studentAnnouncements.Items)
        {
            Assert.Contains(item.TargetAudience, new[] { "ALL", "STUDENTS" });
            Assert.DoesNotContain(item.TargetAudience, new[] { "PARENTS", "TEACHERS" });
        }
    }
}
