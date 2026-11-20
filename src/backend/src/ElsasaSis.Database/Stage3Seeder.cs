using System.Reflection;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.Database;

/// <summary>
/// Idempotently seeds Stage 3 operational fixtures (fee structures, discounts, charges, receipts,
/// allocations, behaviour notes, discipline records, clubs, participations, achievements, report cards).
/// Intended for on-demand re-seeding in Stage 3 integration tests after database resets.
/// </summary>
public static class Stage3Seeder
{
    public static readonly Guid DefaultFinanceUserId = Guid.Parse("01A1F11A-8001-8001-8000-000000000001");
    public const string DefaultFinanceUsername = "finance.officer";
    public const string DefaultFinancePassword = "Teacher123!";
    public const string DefaultFinanceDisplayName = "Siti Rahma";

    public static readonly Guid DefaultChargeTypeSppSmpId = Guid.Parse("01A1F11A-8010-8010-8000-000000000010");
    public const string DefaultChargeTypeSppSmpName = "SPP Bulanan SMP";
    public static readonly Guid DefaultChargeTypeSppSmaId = Guid.Parse("01A1F11A-8011-8011-8000-000000000011");
    public const string DefaultChargeTypeSppSmaName = "SPP Bulanan SMA";
    public static readonly Guid DefaultChargeTypeUniformId = Guid.Parse("01A1F11A-8012-8012-8000-000000000012");
    public const string DefaultChargeTypeUniformName = "Seragam Sekolah";

    public static readonly Guid DefaultDiscountSitiId = Guid.Parse("01A1F11A-8020-8020-8000-000000000020");
    public static readonly Guid DefaultDiscountAhmadId = Guid.Parse("01A1F11A-8021-8021-8000-000000000021");

    public static readonly Guid DefaultChargeAhmadSppId = Guid.Parse("01A1F11A-8030-8030-8000-000000000030");
    public static readonly Guid DefaultChargeSitiSppId = Guid.Parse("01A1F11A-8031-8031-8000-000000000031");
    public static readonly Guid DefaultChargeAhmadUniformId = Guid.Parse("01A1F11A-8032-8032-8000-000000000032");

    public static readonly Guid DefaultReceipt1Id = Guid.Parse("01A1F11A-8040-8040-8000-000000000040");
    public const string DefaultReceipt1Number = "REC/2026/07/0001";
    public static readonly Guid DefaultReceipt2Id = Guid.Parse("01A1F11A-8041-8041-8000-000000000041");
    public const string DefaultReceipt2Number = "REC/2026/07/0002";

    public static readonly Guid DefaultClubPramukaId = Guid.Parse("01A1F11A-8060-8060-8000-000000000060");
    public const string DefaultClubPramukaName = "Pramuka";
    public static readonly Guid DefaultClubFutsalId = Guid.Parse("01A1F11A-8061-8061-8000-000000000061");
    public const string DefaultClubFutsalName = "Futsal";
    public static readonly Guid DefaultClubEnglishId = Guid.Parse("01A1F11A-8062-8062-8000-000000000062");
    public const string DefaultClubEnglishName = "English Club";

    public static readonly Guid DefaultPartPramukaAhmadId = Guid.Parse("01A1F11A-8070-8070-8000-000000000070");
    public static readonly Guid DefaultPartEnglishSitiId = Guid.Parse("01A1F11A-8071-8071-8000-000000000071");
    public static readonly Guid DefaultPartFutsalAhmadId = Guid.Parse("01A1F11A-8072-8072-8000-000000000072");

    public static readonly Guid DefaultBehaviourNote1Id = Guid.Parse("01A1F11A-8080-8080-8000-000000000080");
    public static readonly Guid DefaultBehaviourNote2Id = Guid.Parse("01A1F11A-8081-8081-8000-000000000081");

    public static readonly Guid DefaultDisciplineRecord1Id = Guid.Parse("01A1F11A-8090-8090-8000-000000000090");
    public static readonly Guid DefaultDisciplineRecord2Id = Guid.Parse("01A1F11A-8091-8091-8000-000000000091");

    public static readonly Guid DefaultAchievement1Id = Guid.Parse("01A1F11A-80A0-80A0-8000-0000000000A0");
    public static readonly Guid DefaultAchievement2Id = Guid.Parse("01A1F11A-80A1-80A1-8000-0000000000A1");

    public static readonly Guid DefaultReportCardAhmadId = Guid.Parse("01A1F11A-80B0-80B0-8000-0000000000B0");
    public static readonly Guid DefaultReportCardSitiId = Guid.Parse("01A1F11A-80B1-80B1-8000-0000000000B1");

    public static async Task SeedStage3FixturesAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Ensure default constraint on std.student_placements.is_academically_completed exists
        await using (var ensureConn = new SqlConnection(connectionString))
        {
            await ensureConn.OpenAsync(cancellationToken);
            await using var fixCmd = ensureConn.CreateCommand();
            fixCmd.CommandText = """
                IF EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[std].[student_placements]') 
                      AND name = N'is_academically_completed'
                ) AND NOT EXISTS (
                    SELECT 1 FROM sys.default_constraints
                    WHERE parent_object_id = OBJECT_ID(N'[std].[student_placements]')
                      AND parent_column_id = COLUMNPROPERTY(OBJECT_ID(N'[std].[student_placements]'), 'is_academically_completed', 'ColumnId')
                )
                BEGIN
                    ALTER TABLE [std].[student_placements]
                        ADD CONSTRAINT [DF_student_placements_is_academically_completed] DEFAULT 0 FOR [is_academically_completed];
                END;
                """;
            await fixCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Ensure prerequisite Stage 2 fixtures are in place
        await Stage2Seeder.SeedStage2FixturesAsync(connectionString, cancellationToken);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("101_SeedStage3Data.sql", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Embedded resource '101_SeedStage3Data.sql' was not found in ElsasaSis.Database assembly.");

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
