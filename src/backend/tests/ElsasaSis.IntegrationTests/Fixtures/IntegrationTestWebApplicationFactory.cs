using System.Text;
using ElsasaSis.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Respawn;
using Respawn.Graph;

namespace ElsasaSis.IntegrationTests.Fixtures;

public sealed class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DefaultTestConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=ElsasaSis_Test;Trusted_Connection=True;TrustServerCertificate=True;";

    public const string TestJwtSigningKey = "ElsasaSis-IntegrationTests-SigningKey-Min32Chars!";
    public const string TestAdminPassword = AdminSeeder.DevelopmentDefaultPassword;

    private Respawner? _respawner;

    public string ConnectionString { get; } = ResolveConnectionString();

    private static string ResolveConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Test.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return configuration["IntegrationTests:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("INTEGRATION_TESTS_CONNECTION_STRING")
            ?? DefaultTestConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Jwt:Issuer"] = "ElsasaSis",
                ["Jwt:Audience"] = "ElsasaSis.Client",
                ["Jwt:SigningKey"] = TestJwtSigningKey,
                ["Jwt:RefreshCookieSecure"] = "false",
                ["Seed:AdminPassword"] = TestAdminPassword
            });
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidIssuer = "ElsasaSis";
                options.TokenValidationParameters.ValidAudience = "ElsasaSis.Client";
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSigningKey));
            });
        });
    }

    public async Task InitializeAsync()
    {
        await DatabaseMigrator.MigrateAndSeedAsync(
            ConnectionString,
            TestAdminPassword,
            isDevelopment: true);

        // Ensure the WebApplicationFactory host builds cleanly before Respawn initialization.
        _ = Services;

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore =
            [
                new Table("dbo", "SchemaVersions"),
                new Table("acd", "subjects")
            ],
            WithReseed = true
        });
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null)
        {
            throw new InvalidOperationException("Respawner has not been initialized.");
        }

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await _respawner.ResetAsync(connection);

        await AdminSeeder.SeedAsync(
            ConnectionString,
            TestAdminPassword,
            isDevelopment: true);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
