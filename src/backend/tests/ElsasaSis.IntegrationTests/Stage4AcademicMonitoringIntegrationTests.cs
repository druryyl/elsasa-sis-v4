using System.Net;
using System.Net.Http.Json;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Acd.Features.Monitoring.GetAcademicMonitoringProgress;

namespace ElsasaSis.IntegrationTests;

public sealed class Stage4AcademicMonitoringIntegrationTests : IntegrationTestBase
{
    public Stage4AcademicMonitoringIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedStage4FixturesAsync();
    }

    [Fact]
    public async Task GetAcademicMonitoringProgress_AsAdminAndPrincipal_ReturnsKpisExceptionsAndStudyGroups()
    {
        // 1. Authenticate as Principal
        using var principalClient = await CreateAuthenticatedClientAsync(
            Stage1Seeder.DefaultPrincipalUsername,
            Stage1Seeder.DefaultPrincipalPassword);

        var principalResponse = await principalClient.GetAsync("/api/acd/monitoring/progress");
        Assert.Equal(HttpStatusCode.OK, principalResponse.StatusCode);

        var progressFromPrincipal = await principalResponse.Content.ReadFromJsonAsync<AcademicMonitoringProgressDto>(JsonOptions);
        Assert.NotNull(progressFromPrincipal);

        // Verify Overview KPIs
        Assert.NotNull(progressFromPrincipal!.Overview);
        Assert.True(progressFromPrincipal.Overview.AttendanceRateTodayPercent >= 0 && progressFromPrincipal.Overview.AttendanceRateTodayPercent <= 100);
        Assert.True(progressFromPrincipal.Overview.GradingCompletenessPercent >= 0 && progressFromPrincipal.Overview.GradingCompletenessPercent <= 100);
        Assert.True(progressFromPrincipal.Overview.ReportCardsFinalizedPercent >= 0 && progressFromPrincipal.Overview.ReportCardsFinalizedPercent <= 100);
        Assert.True(progressFromPrincipal.Overview.TotalPlacedStudentsCount >= 0);

        // Verify Operational Exception Alerts
        Assert.NotNull(progressFromPrincipal.Exceptions);
        Assert.NotNull(progressFromPrincipal.Exceptions.MissingAttendanceStudyGroups);
        Assert.NotNull(progressFromPrincipal.Exceptions.OverdueGradingAssignments);
        Assert.NotNull(progressFromPrincipal.Exceptions.PendingReportCards);

        // Verify Study Group Drill-down Matrix
        Assert.NotNull(progressFromPrincipal.StudyGroups);
        Assert.NotEmpty(progressFromPrincipal.StudyGroups);
        foreach (var sg in progressFromPrincipal.StudyGroups)
        {
            Assert.NotEqual(Guid.Empty, sg.StudyGroupId);
            Assert.False(string.IsNullOrWhiteSpace(sg.StudyGroupName));
            Assert.False(string.IsNullOrWhiteSpace(sg.SchoolLevel));
            Assert.False(string.IsNullOrWhiteSpace(sg.Grade));
            Assert.True(sg.AttendanceTodayPercent >= 0 && sg.AttendanceTodayPercent <= 100);
            Assert.True(sg.GradingCompletenessPercent >= 0 && sg.GradingCompletenessPercent <= 100);
            Assert.False(string.IsNullOrWhiteSpace(sg.ReportCardsFinalizedRatio));
        }

        // 2. Authenticate as Admin
        using var adminClient = await CreateAuthenticatedAdminClientAsync();
        var adminResponse = await adminClient.GetAsync("/api/acd/monitoring/progress");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        var progressFromAdmin = await adminResponse.Content.ReadFromJsonAsync<AcademicMonitoringProgressDto>(JsonOptions);
        Assert.NotNull(progressFromAdmin);
        Assert.Equal(progressFromPrincipal.StudyGroups.Count, progressFromAdmin!.StudyGroups.Count);
    }

    [Fact]
    public async Task GetAcademicMonitoringProgress_AsParentOrAnonymous_ReturnsForbiddenOrUnauthorized()
    {
        // Anonymous request -> 401 Unauthorized
        using var anonymousClient = CreateAnonymousClient();
        var anonResponse = await anonymousClient.GetAsync("/api/acd/monitoring/progress");
        Assert.Equal(HttpStatusCode.Unauthorized, anonResponse.StatusCode);

        // Parent request -> 403 Forbidden
        using var parentClient = await CreateAuthenticatedClientAsync(
            Stage4Seeder.DefaultParentUsername,
            Stage4Seeder.DefaultParentPassword);
        var parentResponse = await parentClient.GetAsync("/api/acd/monitoring/progress");
        Assert.Equal(HttpStatusCode.Forbidden, parentResponse.StatusCode);
    }
}
