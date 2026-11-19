using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Auth;
using ElsasaSis.Modules.Auth.Features.Auth;
using ElsasaSis.Modules.Auth.Features.Auth.Login;
using ElsasaSis.Modules.Auth.Features.Auth.Refresh;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests.Auth;

public sealed class AuthIntegrationTests : IntegrationTestBase
{
    private const string RefreshCookieName = "elsasa_refresh_token";

    public AuthIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsJwtWithExpectedClaimsAndHttpOnlyRefreshCookie()
    {
        using var client = CreateAnonymousClient(handleCookies: false);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(AdminSeeder.DefaultAdminUsername, IntegrationTestWebApplicationFactory.TestAdminPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
        Assert.Equal(AdminSeeder.DefaultAdminUsername, payload.User.Username);
        Assert.Contains(AdminSeeder.DefaultAdminRole, payload.User.Roles);
        Assert.Contains(Permissions.Operations.Calendar.View, payload.User.Permissions);
        Assert.Contains(Permissions.Operations.Calendar.Manage, payload.User.Permissions);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(payload.AccessToken);
        Assert.Contains(jwt.Claims, c => c.Type == "sub" && Guid.TryParse(c.Value, out _));
        Assert.Contains(jwt.Claims, c => c.Type == "preferred_username" && c.Value == AdminSeeder.DefaultAdminUsername);
        Assert.Contains(jwt.Claims, c => c.Type == "name" && !string.IsNullOrWhiteSpace(c.Value));
        Assert.Contains(jwt.Claims, c => c.Type == "roles" && c.Value == AdminSeeder.DefaultAdminRole);
        Assert.Contains(jwt.Claims, c => c.Type == "permissions" && c.Value == Permissions.Operations.Calendar.View);
        Assert.Contains(jwt.Claims, c => c.Type == "permissions" && c.Value == Permissions.Operations.Calendar.Manage);

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookies));
        var refreshCookie = Assert.Single(setCookies, c => c.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal));
        Assert.Contains("httponly", refreshCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", refreshCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401Unauthorized()
    {
        using var client = CreateAnonymousClient(handleCookies: false);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(AdminSeeder.DefaultAdminUsername, "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidCookie_IssuesNewJwtAndRotatesRefreshToken()
    {
        using var client = CreateAnonymousClient(handleCookies: false);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(AdminSeeder.DefaultAdminUsername, IntegrationTestWebApplicationFactory.TestAdminPassword));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var initialCookieHeader = ExtractCookiePair(loginResponse, RefreshCookieName);

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", initialCookieHeader);

        var refreshResponse = await client.SendAsync(refreshRequest);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<RefreshResponse>(JsonOptions);
        Assert.NotNull(refreshPayload);
        Assert.False(string.IsNullOrWhiteSpace(refreshPayload!.AccessToken));
        Assert.Equal(AdminSeeder.DefaultAdminUsername, refreshPayload.User.Username);

        var rotatedCookieHeader = ExtractCookiePair(refreshResponse, RefreshCookieName);
        Assert.NotEqual(initialCookieHeader, rotatedCookieHeader);

        // Verify SQL state: 2 tokens exist, old one is revoked, new one is active.
        await using var connection = new SqlConnection(Factory.ConnectionString);
        var tokens = (await connection.QueryAsync<(Guid Id, DateTime? RevokedAt)>(
            "SELECT [id] AS Id, [revoked_at] AS RevokedAt FROM [auth].[refresh_tokens] ORDER BY [created_at] ASC;"))
            .ToArray();

        Assert.Equal(2, tokens.Length);
        Assert.NotNull(tokens[0].RevokedAt);
        Assert.Null(tokens[1].RevokedAt);

        // Reusing the old refresh token must be rejected with 401.
        using var reuseOldRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        reuseOldRequest.Headers.Add("Cookie", initialCookieHeader);

        var reuseOldResponse = await client.SendAsync(reuseOldRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseOldResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshTokenAndClearsCookie()
    {
        using var client = CreateAnonymousClient(handleCookies: false);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(AdminSeeder.DefaultAdminUsername, IntegrationTestWebApplicationFactory.TestAdminPassword));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var cookieHeader = ExtractCookiePair(loginResponse, RefreshCookieName);

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add("Cookie", cookieHeader);

        var logoutResponse = await client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        Assert.True(logoutResponse.Headers.TryGetValues("Set-Cookie", out var setCookies));
        var clearedCookie = Assert.Single(setCookies, c => c.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal));
        Assert.StartsWith($"{RefreshCookieName}=;", clearedCookie, StringComparison.Ordinal);

        await using var connection = new SqlConnection(Factory.ConnectionString);
        var revokedCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM [auth].[refresh_tokens] WHERE [revoked_at] IS NOT NULL;");
        Assert.Equal(1, revokedCount);

        using var refreshAfterLogoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshAfterLogoutRequest.Headers.Add("Cookie", cookieHeader);

        var refreshAfterLogoutResponse = await client.SendAsync(refreshAfterLogoutRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogoutResponse.StatusCode);
    }

    [Fact]
    public async Task GetMe_ReturnsProfileWhenAuthenticatedAnd401WhenAnonymous()
    {
        using var anonymousClient = CreateAnonymousClient();
        var anonResponse = await anonymousClient.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonResponse.StatusCode);

        using var adminClient = await CreateAuthenticatedAdminClientAsync();
        var meResponse = await adminClient.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var profile = await meResponse.Content.ReadFromJsonAsync<AuthUserDto>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal(AdminSeeder.DefaultAdminUsername, profile!.Username);
        Assert.Contains(AdminSeeder.DefaultAdminRole, profile.Roles);
        Assert.Contains(Permissions.Operations.Calendar.Manage, profile.Permissions);
    }

    private static string ExtractCookiePair(HttpResponseMessage response, string cookieName)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookies));
        var matching = Assert.Single(setCookies, c => c.StartsWith($"{cookieName}=", StringComparison.Ordinal));
        var separatorIndex = matching.IndexOf(';');
        return separatorIndex >= 0 ? matching[..separatorIndex] : matching;
    }
}
