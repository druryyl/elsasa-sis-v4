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
                new Table("dbo", "SchemaVersions")
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

        await ReseedCanonicalSubjectsAsync(connection);

        await AdminSeeder.SeedAsync(
            ConnectionString,
            TestAdminPassword,
            isDevelopment: true);
    }

    private static async Task ReseedCanonicalSubjectsAsync(SqlConnection connection)
    {
        const string sql = """
            DECLARE @Now DATETIME2(3) = CAST(SYSUTCDATETIME() AS DATETIME2(3));

            DELETE FROM [acd].[subjects];

            INSERT INTO [acd].[subjects] ([id], [name], [code], [created_at], [updated_at])
            VALUES
                ('01A1F11A-6001-7001-8000-000000000001', N'Pendidikan Agama dan Budi Pekerti', N'PA-BP', @Now, @Now),
                ('01A1F11A-6002-7002-8000-000000000002', N'Pendidikan Pancasila dan Kewarganegaraan', N'PPKN', @Now, @Now),
                ('01A1F11A-6003-7003-8000-000000000003', N'Bahasa Indonesia', N'BIND', @Now, @Now),
                ('01A1F11A-6004-7004-8000-000000000004', N'Matematika', N'MTK', @Now, @Now),
                ('01A1F11A-6005-7005-8000-000000000005', N'Ilmu Pengetahuan Alam', N'IPA', @Now, @Now),
                ('01A1F11A-6006-7006-8000-000000000006', N'Ilmu Pengetahuan Sosial', N'IPS', @Now, @Now),
                ('01A1F11A-6007-7007-8000-000000000007', N'Bahasa Inggris', N'BING', @Now, @Now),
                ('01A1F11A-6008-7008-8000-000000000008', N'Seni Budaya', N'SNB', @Now, @Now),
                ('01A1F11A-6009-7009-8000-000000000009', N'Pendidikan Jasmani, Olahraga, dan Kesehatan', N'PJOK', @Now, @Now),
                ('01A1F11A-600A-700A-8000-00000000000A', N'Informatika', N'INF', @Now, @Now);
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    public Task SeedStage2FixturesAsync(CancellationToken cancellationToken = default) =>
        Stage2Seeder.SeedStage2FixturesAsync(ConnectionString, cancellationToken);

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
