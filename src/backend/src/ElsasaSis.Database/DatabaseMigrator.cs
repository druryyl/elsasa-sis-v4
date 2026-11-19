using DbUp;

namespace ElsasaSis.Database;

/// <summary>
/// Executes embedded DbUp SQL migrations and idempotent initial data seeding.
/// </summary>
public static class DatabaseMigrator
{
    public static void Migrate(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        EnsureDatabase.For.SqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly)
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException("Database migration failed.", result.Error);
        }
    }

    public static async Task MigrateAndSeedAsync(
        string connectionString,
        string? adminPassword,
        bool isDevelopment,
        CancellationToken cancellationToken = default)
    {
        Migrate(connectionString);
        await AdminSeeder.SeedAsync(connectionString, adminPassword, isDevelopment, cancellationToken);
    }

    public static void MigrateAndSeed(
        string connectionString,
        string? adminPassword,
        bool isDevelopment)
    {
        MigrateAndSeedAsync(connectionString, adminPassword, isDevelopment).GetAwaiter().GetResult();
    }
}
