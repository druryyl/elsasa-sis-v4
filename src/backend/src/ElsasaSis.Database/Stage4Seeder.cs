using System.Reflection;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.Database;

/// <summary>
/// Idempotently seeds Stage 4 consumer portal fixtures (parent and student accounts, role assignments,
/// and child/actor linkages). Intended for on-demand re-seeding in integration tests and local development.
/// </summary>
public static class Stage4Seeder
{
    public static readonly Guid DefaultParentUserId = Guid.Parse("01A1F11A-9001-9001-8000-000000000001");
    public const string DefaultParentUsername = "parent.hendra";
    public const string DefaultParentPassword = "Parent123!";
    public const string DefaultParentDisplayName = "Hendra Dahlan";
    public const string DefaultParentEmail = "hendra.dahlan@elsasa.sch.id";

    public static readonly Guid DefaultStudentAhmadUserId = Guid.Parse("01A1F11A-9002-9002-8000-000000000002");
    public const string DefaultStudentAhmadUsername = "student.ahmad";
    public const string DefaultStudentAhmadPassword = "Student123!";
    public const string DefaultStudentAhmadDisplayName = "Ahmad Dahlan";
    public const string DefaultStudentAhmadEmail = "ahmad.dahlan@elsasa.sch.id";

    public static readonly Guid DefaultStudentSitiUserId = Guid.Parse("01A1F11A-9003-9003-8000-000000000003");
    public const string DefaultStudentSitiUsername = "student.siti";
    public const string DefaultStudentSitiPassword = "Student123!";
    public const string DefaultStudentSitiDisplayName = "Siti Nurhaliza";
    public const string DefaultStudentSitiEmail = "siti.nurhaliza@elsasa.sch.id";

    public static readonly Guid DefaultStudentAhmadId = Guid.Parse("01A1F11A-7010-7010-8000-000000000010");
    public static readonly Guid DefaultStudentSitiId = Guid.Parse("01A1F11A-7011-7011-8000-000000000011");

    public static async Task SeedStage4FixturesAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Ensure prerequisite Stage 3 fixtures are in place
        await Stage3Seeder.SeedStage3FixturesAsync(connectionString, cancellationToken);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("102_SeedStage4Data.sql", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Embedded resource '102_SeedStage4Data.sql' was not found in ElsasaSis.Database assembly.");

        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Unable to open resource stream for '{resourceName}'.");

        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(cancellationToken);

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
