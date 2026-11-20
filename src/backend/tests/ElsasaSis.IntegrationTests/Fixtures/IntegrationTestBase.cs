using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ElsasaSis.Database;
using ElsasaSis.Modules.Auth.Features.Auth.Login;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ElsasaSis.IntegrationTests.Fixtures;

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected IntegrationTestWebApplicationFactory Factory { get; }

    protected IntegrationTestBase(IntegrationTestWebApplicationFactory factory)
    {
        Factory = factory;
    }

    public virtual async Task InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected HttpClient CreateAnonymousClient(bool handleCookies = false)
    {
        return Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = handleCookies
        });
    }

    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string username, string password)
    {
        var client = CreateAnonymousClient(handleCookies: true);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, password));

        loginResponse.EnsureSuccessStatusCode();

        var payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(payload);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", payload!.AccessToken);

        return client;
    }

    protected Task<HttpClient> CreateAuthenticatedAdminClientAsync() =>
        CreateAuthenticatedClientAsync(AdminSeeder.DefaultAdminUsername, IntegrationTestWebApplicationFactory.TestAdminPassword);

    protected async Task SeedStage1FixturesAsync(CancellationToken cancellationToken = default)
    {
        await Stage1Seeder.SeedRoleAccountsAndFixturesAsync(Factory.ConnectionString, cancellationToken);
    }

    protected async Task SeedStage2FixturesAsync(CancellationToken cancellationToken = default)
    {
        await Factory.SeedStage2FixturesAsync(cancellationToken);
    }

    protected async Task SeedStage3FixturesAsync(CancellationToken cancellationToken = default)
    {
        await Factory.SeedStage3FixturesAsync(cancellationToken);
    }

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.Clone();
    }
}
