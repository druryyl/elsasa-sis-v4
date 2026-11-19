using ElsasaSis.Database;
using Microsoft.Extensions.Configuration;

var environmentName =
    Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? configuration["connection"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Error: ConnectionStrings:DefaultConnection (or --connection) is required.");
    return 1;
}

var isDevelopment = string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
var adminPassword = configuration["Seed:AdminPassword"];

try
{
    await DatabaseMigrator.MigrateAndSeedAsync(connectionString, adminPassword, isDevelopment);
    Console.WriteLine("Database migration and seeding completed successfully.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Database migration failed: {ex.Message}");
    return 1;
}
