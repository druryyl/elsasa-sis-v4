using System.Net;
using System.Net.Http.Json;
using Dapper;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Opr.Features.AcademicYears;
using ElsasaSis.Modules.Opr.Features.AcademicYears.CreateAcademicYear;
using ElsasaSis.Modules.Opr.Features.AcademicYears.UpdateAcademicYear;
using ElsasaSis.SharedKernel.Pagination;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests.Opr;

public sealed class AcademicYearsIntegrationTests : IntegrationTestBase
{
    public AcademicYearsIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Endpoints_WhenUnauthenticated_Return401Unauthorized()
    {
        using var client = CreateAnonymousClient();
        var id = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        var responses = await Task.WhenAll(
            client.GetAsync("/api/opr/academic-years"),
            client.GetAsync($"/api/opr/academic-years/{id}"),
            client.PostAsJsonAsync("/api/opr/academic-years", BuildValidCreateRequest("2026/2027")),
            client.PutAsJsonAsync($"/api/opr/academic-years/{id}", new UpdateAcademicYearRequest(
                "2026/2027",
                new DateOnly(2026, 7, 1),
                new DateOnly(2027, 6, 30),
                [new UpdateOperationalPeriodRequest(null, "Semester 1", new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 20))])),
            client.PostAsync($"/api/opr/academic-years/{id}/activate", null),
            client.PostAsync($"/api/opr/academic-years/{id}/periods/{periodId}/activate", null));

        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task CreateAcademicYear_WithValidPayload_PersistsYearAndPeriodsAndReturns201()
    {
        using var client = await CreateAuthenticatedAdminClientAsync();
        var request = BuildValidCreateRequest("2026/2027");

        var response = await client.PostAsJsonAsync("/api/opr/academic-years", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<AcademicYearDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("2026/2027", created!.Name);
        Assert.Equal("INACTIVE", created.Status);
        Assert.Equal(2, created.Periods.Count);
        Assert.All(created.Periods, p => Assert.Equal("INACTIVE", p.Status));

        await using var connection = new SqlConnection(Factory.ConnectionString);
        var yearCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM [opr].[academic_years] WHERE [id] = @Id;",
            new { created.Id });
        var periodCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM [opr].[operational_periods] WHERE [academic_year_id] = @Id;",
            new { created.Id });

        Assert.Equal(1, yearCount);
        Assert.Equal(2, periodCount);
    }

    [Fact]
    public async Task CreateAcademicYear_WithInvalidPayloads_ReturnsRfc7807With422AndCamelCaseErrors()
    {
        using var client = await CreateAuthenticatedAdminClientAsync();

        // 1. FluentValidation failure (empty name) -> camelCase "name"
        var emptyNameRequest = new CreateAcademicYearRequest(
            "",
            new DateOnly(2026, 7, 1),
            new DateOnly(2027, 6, 30),
            [new CreateOperationalPeriodRequest("Semester 1", new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 20))]);

        var emptyNameResponse = await client.PostAsJsonAsync("/api/opr/academic-years", emptyNameRequest);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, emptyNameResponse.StatusCode);
        var emptyNameJson = await ReadJsonAsync(emptyNameResponse);
        Assert.Equal("Request.ValidationFailed", emptyNameJson.GetProperty("errorCode").GetString());
        Assert.True(emptyNameJson.GetProperty("errors").TryGetProperty("name", out _));

        // 2. Domain validation: endDate <= startDate -> camelCase "endDate"
        var invalidDateRangeRequest = new CreateAcademicYearRequest(
            "2026/2027",
            new DateOnly(2027, 6, 30),
            new DateOnly(2026, 7, 1),
            [new CreateOperationalPeriodRequest("Semester 1", new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 20))]);

        var invalidDateResponse = await client.PostAsJsonAsync("/api/opr/academic-years", invalidDateRangeRequest);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidDateResponse.StatusCode);
        var invalidDateJson = await ReadJsonAsync(invalidDateResponse);
        Assert.Equal("Opr.DateRange.Invalid", invalidDateJson.GetProperty("errorCode").GetString());
        Assert.True(invalidDateJson.GetProperty("errors").TryGetProperty("endDate", out _));

        // 3. Domain validation: overlapping periods -> camelCase "periods"
        var overlappingRequest = new CreateAcademicYearRequest(
            "2026/2027",
            new DateOnly(2026, 7, 1),
            new DateOnly(2027, 6, 30),
            [
                new CreateOperationalPeriodRequest("Semester 1", new DateOnly(2026, 7, 1), new DateOnly(2027, 1, 15)),
                new CreateOperationalPeriodRequest("Semester 2", new DateOnly(2027, 1, 10), new DateOnly(2027, 6, 30))
            ]);

        var overlappingResponse = await client.PostAsJsonAsync("/api/opr/academic-years", overlappingRequest);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, overlappingResponse.StatusCode);
        var overlappingJson = await ReadJsonAsync(overlappingResponse);
        Assert.Equal("Opr.OperationalPeriod.Overlapping", overlappingJson.GetProperty("errorCode").GetString());
        Assert.True(overlappingJson.GetProperty("errors").TryGetProperty("periods", out _));

        // 4. Domain validation: out-of-bounds period -> camelCase "periods"
        var outOfBoundsRequest = new CreateAcademicYearRequest(
            "2026/2027",
            new DateOnly(2026, 7, 1),
            new DateOnly(2027, 6, 30),
            [
                new CreateOperationalPeriodRequest("Semester 1", new DateOnly(2026, 6, 15), new DateOnly(2026, 12, 20))
            ]);

        var outOfBoundsResponse = await client.PostAsJsonAsync("/api/opr/academic-years", outOfBoundsRequest);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, outOfBoundsResponse.StatusCode);
        var outOfBoundsJson = await ReadJsonAsync(outOfBoundsResponse);
        Assert.Equal("Opr.OperationalPeriod.OutOfBounds", outOfBoundsJson.GetProperty("errorCode").GetString());
        Assert.True(outOfBoundsJson.GetProperty("errors").TryGetProperty("periods", out _));
    }

    [Fact]
    public async Task GetAcademicYears_And_GetById_ReturnPaginatedWithPeriodsAnd404WhenMissing()
    {
        using var client = await CreateAuthenticatedAdminClientAsync();

        var created1 = await CreateYearAsync(client, "2025/2026", 2025);
        var created2 = await CreateYearAsync(client, "2026/2027", 2026);

        var listResponse = await client.GetAsync("/api/opr/academic-years?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var page = await listResponse.Content.ReadFromJsonAsync<PaginatedResponse<AcademicYearDto>>(JsonOptions);
        Assert.NotNull(page);
        Assert.Equal(2, page!.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.All(page.Items, y => Assert.Equal(2, y.Periods.Count));

        var detailResponse = await client.GetAsync($"/api/opr/academic-years/{created1.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<AcademicYearDto>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(created1.Id, detail!.Id);
        Assert.Equal(2, detail.Periods.Count);

        var missingResponse = await client.GetAsync($"/api/opr/academic-years/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateAcademicYear_SupportsInactiveAndActiveYears_AndRejectsRemovingActivePeriodWith409()
    {
        using var client = await CreateAuthenticatedAdminClientAsync();
        var created = await CreateYearAsync(client, "2026/2027", 2026);
        var semester1 = created.Periods[0];
        var semester2 = created.Periods[1];

        // Update while INACTIVE.
        var updateInactiveRequest = new UpdateAcademicYearRequest(
            "2026/2027 Revised",
            new DateOnly(2026, 7, 1),
            new DateOnly(2027, 6, 30),
            [
                new UpdateOperationalPeriodRequest(semester1.Id, "Term 1", semester1.StartDate, semester1.EndDate),
                new UpdateOperationalPeriodRequest(semester2.Id, "Term 2", semester2.StartDate, semester2.EndDate)
            ]);

        var updateInactiveResponse = await client.PutAsJsonAsync($"/api/opr/academic-years/{created.Id}", updateInactiveRequest);
        Assert.Equal(HttpStatusCode.OK, updateInactiveResponse.StatusCode);
        var updatedInactive = await updateInactiveResponse.Content.ReadFromJsonAsync<AcademicYearDto>(JsonOptions);
        Assert.NotNull(updatedInactive);
        Assert.Equal("2026/2027 Revised", updatedInactive!.Name);
        Assert.Equal("INACTIVE", updatedInactive.Status);

        // Activate year -> earliest period (Term 1) becomes ACTIVE.
        var activateResponse = await client.PostAsync($"/api/opr/academic-years/{created.Id}/activate", null);
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);

        // Update while ACTIVE (keeping active Term 1) -> preserves ACTIVE statuses.
        var updateActiveRequest = new UpdateAcademicYearRequest(
            "2026/2027 Active",
            new DateOnly(2026, 7, 1),
            new DateOnly(2027, 6, 30),
            [
                new UpdateOperationalPeriodRequest(semester1.Id, "Term 1 Updated", semester1.StartDate, semester1.EndDate),
                new UpdateOperationalPeriodRequest(semester2.Id, "Term 2 Updated", semester2.StartDate, semester2.EndDate)
            ]);

        var updateActiveResponse = await client.PutAsJsonAsync($"/api/opr/academic-years/{created.Id}", updateActiveRequest);
        Assert.Equal(HttpStatusCode.OK, updateActiveResponse.StatusCode);
        var updatedActive = await updateActiveResponse.Content.ReadFromJsonAsync<AcademicYearDto>(JsonOptions);
        Assert.NotNull(updatedActive);
        Assert.Equal("ACTIVE", updatedActive!.Status);
        Assert.Equal("ACTIVE", updatedActive.Periods.Single(p => p.Id == semester1.Id).Status);
        Assert.Equal("INACTIVE", updatedActive.Periods.Single(p => p.Id == semester2.Id).Status);

        // Attempt to remove currently ACTIVE period -> 409 Conflict.
        var removeActivePeriodRequest = new UpdateAcademicYearRequest(
            "2026/2027 Active",
            new DateOnly(2026, 7, 1),
            new DateOnly(2027, 6, 30),
            [
                new UpdateOperationalPeriodRequest(semester2.Id, "Term 2 Only", semester2.StartDate, semester2.EndDate)
            ]);

        var removeActiveResponse = await client.PutAsJsonAsync($"/api/opr/academic-years/{created.Id}", removeActivePeriodRequest);
        Assert.Equal(HttpStatusCode.Conflict, removeActiveResponse.StatusCode);
    }

    [Fact]
    public async Task ActivateAcademicYear_DeactivatesPriorActiveYearAndActivatesEarliestPeriodInSql()
    {
        using var client = await CreateAuthenticatedAdminClientAsync();
        var year1 = await CreateYearAsync(client, "2025/2026", 2025);
        var year2 = await CreateYearAsync(client, "2026/2027", 2026);

        var activate1 = await client.PostAsync($"/api/opr/academic-years/{year1.Id}/activate", null);
        Assert.Equal(HttpStatusCode.OK, activate1.StatusCode);

        var activate2 = await client.PostAsync($"/api/opr/academic-years/{year2.Id}/activate", null);
        Assert.Equal(HttpStatusCode.OK, activate2.StatusCode);

        var activated2 = await activate2.Content.ReadFromJsonAsync<AcademicYearDto>(JsonOptions);
        Assert.NotNull(activated2);
        Assert.Equal("ACTIVE", activated2!.Status);
        Assert.Equal("ACTIVE", activated2.Periods[0].Status);
        Assert.Equal("INACTIVE", activated2.Periods[1].Status);

        await using var connection = new SqlConnection(Factory.ConnectionString);
        var activeYears = (await connection.QueryAsync<Guid>(
            "SELECT [id] FROM [opr].[academic_years] WHERE [status] = 'ACTIVE';")).ToArray();
        var activePeriods = (await connection.QueryAsync<Guid>(
            "SELECT [id] FROM [opr].[operational_periods] WHERE [status] = 'ACTIVE';")).ToArray();

        Assert.Single(activeYears);
        Assert.Equal(year2.Id, activeYears[0]);
        Assert.Single(activePeriods);
        Assert.Equal(activated2.Periods[0].Id, activePeriods[0]);
    }

    [Fact]
    public async Task ActivateOperationalPeriod_RequiresActiveParentYearAndSwitchesActivePeriod()
    {
        using var client = await CreateAuthenticatedAdminClientAsync();
        var year = await CreateYearAsync(client, "2026/2027", 2026);
        var semester1 = year.Periods[0];
        var semester2 = year.Periods[1];

        // Parent year is INACTIVE -> 409 Conflict.
        var inactiveYearAttempt = await client.PostAsync(
            $"/api/opr/academic-years/{year.Id}/periods/{semester2.Id}/activate",
            null);
        Assert.Equal(HttpStatusCode.Conflict, inactiveYearAttempt.StatusCode);

        // Activate parent year (activates Semester 1).
        var activateYearResponse = await client.PostAsync($"/api/opr/academic-years/{year.Id}/activate", null);
        Assert.Equal(HttpStatusCode.OK, activateYearResponse.StatusCode);

        // Now activate Semester 2 -> 200 OK, Semester 1 becomes INACTIVE, Semester 2 becomes ACTIVE.
        var activatePeriod2Response = await client.PostAsync(
            $"/api/opr/academic-years/{year.Id}/periods/{semester2.Id}/activate",
            null);
        Assert.Equal(HttpStatusCode.OK, activatePeriod2Response.StatusCode);

        var updated = await activatePeriod2Response.Content.ReadFromJsonAsync<AcademicYearDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("INACTIVE", updated!.Periods.Single(p => p.Id == semester1.Id).Status);
        Assert.Equal("ACTIVE", updated.Periods.Single(p => p.Id == semester2.Id).Status);
    }

    private static async Task<AcademicYearDto> CreateYearAsync(HttpClient client, string name, int startYear)
    {
        var request = BuildValidCreateRequest(name, startYear);
        var response = await client.PostAsJsonAsync("/api/opr/academic-years", request);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<AcademicYearDto>(JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private static CreateAcademicYearRequest BuildValidCreateRequest(string name, int startYear = 2026) =>
        new(
            name,
            new DateOnly(startYear, 7, 1),
            new DateOnly(startYear + 1, 6, 30),
            [
                new CreateOperationalPeriodRequest(
                    "Semester 1",
                    new DateOnly(startYear, 7, 1),
                    new DateOnly(startYear, 12, 20)),
                new CreateOperationalPeriodRequest(
                    "Semester 2",
                    new DateOnly(startYear + 1, 1, 5),
                    new DateOnly(startYear + 1, 6, 30))
            ]);
}
