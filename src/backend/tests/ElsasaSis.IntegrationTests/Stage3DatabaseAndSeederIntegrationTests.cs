using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests;

public sealed class Stage3DatabaseAndSeederIntegrationTests : IntegrationTestBase
{
    public Stage3DatabaseAndSeederIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task SeedStage3FixturesAsync_IsIdempotent_AndPopulatesAllStage3Data()
    {
        // First execution
        await SeedStage3FixturesAsync();

        // Second execution (assert idempotency)
        await SeedStage3FixturesAsync();

        await using var connection = new SqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();

        // Verify 11 new tables exist in sys.tables
        var tableNames = (await connection.QueryAsync<string>("""
            SELECT s.name + '.' + t.name
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name IN ('fin', 'dev')
            """)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 6 fin tables
        Assert.Contains("fin.charge_types", tableNames);
        Assert.Contains("fin.student_discounts", tableNames);
        Assert.Contains("fin.student_charges", tableNames);
        Assert.Contains("fin.payment_receipts", tableNames);
        Assert.Contains("fin.payment_allocations", tableNames);
        Assert.Contains("fin.receipt_sequences", tableNames);

        // 5 dev tables
        Assert.Contains("dev.behaviour_notes", tableNames);
        Assert.Contains("dev.discipline_records", tableNames);
        Assert.Contains("dev.extracurricular_activities", tableNames);
        Assert.Contains("dev.extracurricular_participations", tableNames);
        Assert.Contains("dev.achievements", tableNames);

        // Verify modified columns exist
        var reportCardsColumns = (await connection.QueryAsync<string>("""
            SELECT c.name
            FROM sys.columns c
            WHERE c.object_id = OBJECT_ID(N'[acd].[report_cards]')
            """)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("override_reason", reportCardsColumns);
        Assert.Contains("general_remarks", reportCardsColumns);

        var chargesColumns = (await connection.QueryAsync<string>("""
            SELECT c.name
            FROM sys.columns c
            WHERE c.object_id = OBJECT_ID(N'[fin].[student_charges]')
            """)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("cancellation_reason", chargesColumns);
        Assert.Contains("cancelled_at", chargesColumns);

        var participationsColumns = (await connection.QueryAsync<string>("""
            SELECT c.name
            FROM sys.columns c
            WHERE c.object_id = OBJECT_ID(N'[dev].[extracurricular_participations]')
            """)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("last_session_log", participationsColumns);
        Assert.Contains("last_session_date", participationsColumns);

        var placementsColumns = (await connection.QueryAsync<string>("""
            SELECT c.name
            FROM sys.columns c
            WHERE c.object_id = OBJECT_ID(N'[std].[student_placements]')
            """)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("is_academically_completed", placementsColumns);

        // Verify Finance user seeded
        var financeUser = await connection.QuerySingleOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM [auth].[users] WHERE [id] = @Id AND [username] = @Username",
            new { Id = Stage3Seeder.DefaultFinanceUserId, Username = Stage3Seeder.DefaultFinanceUsername });
        Assert.Equal(1, financeUser);

        // Verify Charge Types
        var chargeTypes = await connection.QueryAsync<dynamic>(
            "SELECT [id], [name], [frequency], [amount], [status] FROM [fin].[charge_types]");
        Assert.True(chargeTypes.Count() >= 3);

        var sppSmp = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT [name], [amount], [frequency] FROM [fin].[charge_types] WHERE [id] = @Id",
            new { Id = Stage3Seeder.DefaultChargeTypeSppSmpId });
        Assert.NotNull(sppSmp);
        Assert.Equal(Stage3Seeder.DefaultChargeTypeSppSmpName, (string)sppSmp!.name);
        Assert.Equal(350000.00m, (decimal)sppSmp.amount);
        Assert.Equal("MONTHLY", (string)sppSmp.frequency);

        // Verify Student Discounts
        var discounts = await connection.QueryAsync<dynamic>(
            "SELECT [id], [student_name], [discount_type], [discount_value] FROM [fin].[student_discounts]");
        Assert.True(discounts.Count() >= 2);

        // Verify Student Charges
        var ahmadSppCharge = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT [id], [net_amount], [paid_amount], [outstanding_amount], [status] FROM [fin].[student_charges] WHERE [id] = @Id",
            new { Id = Stage3Seeder.DefaultChargeAhmadSppId });
        Assert.NotNull(ahmadSppCharge);
        Assert.Equal(250000.00m, (decimal)ahmadSppCharge!.net_amount);
        Assert.Equal(250000.00m, (decimal)ahmadSppCharge.paid_amount);
        Assert.Equal(0.00m, (decimal)ahmadSppCharge.outstanding_amount);
        Assert.Equal("PAID", (string)ahmadSppCharge.status);

        var sitiSppCharge = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT [id], [net_amount], [paid_amount], [outstanding_amount], [status] FROM [fin].[student_charges] WHERE [id] = @Id",
            new { Id = Stage3Seeder.DefaultChargeSitiSppId });
        Assert.NotNull(sitiSppCharge);
        Assert.Equal(280000.00m, (decimal)sitiSppCharge!.net_amount);
        Assert.Equal(150000.00m, (decimal)sitiSppCharge.paid_amount);
        Assert.Equal(130000.00m, (decimal)sitiSppCharge.outstanding_amount);
        Assert.Equal("PARTIALLY_PAID", (string)sitiSppCharge.status);

        // Verify Payment Receipts & Allocations
        var receipt1 = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT [receipt_number], [total_amount], [allocated_amount], [unallocated_amount], [payment_method] FROM [fin].[payment_receipts] WHERE [id] = @Id",
            new { Id = Stage3Seeder.DefaultReceipt1Id });
        Assert.NotNull(receipt1);
        Assert.Equal(Stage3Seeder.DefaultReceipt1Number, (string)receipt1!.receipt_number);
        Assert.Equal(250000.00m, (decimal)receipt1.total_amount);
        Assert.Equal(250000.00m, (decimal)receipt1.allocated_amount);
        Assert.Equal(0.00m, (decimal)receipt1.unallocated_amount);

        var receipt2 = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT [receipt_number], [total_amount], [allocated_amount], [unallocated_amount], [payment_method] FROM [fin].[payment_receipts] WHERE [id] = @Id",
            new { Id = Stage3Seeder.DefaultReceipt2Id });
        Assert.NotNull(receipt2);
        Assert.Equal(Stage3Seeder.DefaultReceipt2Number, (string)receipt2!.receipt_number);
        Assert.Equal(200000.00m, (decimal)receipt2.total_amount);
        Assert.Equal(150000.00m, (decimal)receipt2.allocated_amount);
        Assert.Equal(50000.00m, (decimal)receipt2.unallocated_amount);

        // Verify Receipt Sequences
        var seq = await connection.QuerySingleOrDefaultAsync<int>(
            "SELECT [next_seq] FROM [fin].[receipt_sequences] WHERE [month] = '2026-07'");
        Assert.True(seq >= 3);

        // Verify Behaviour Notes
        var behaviourNotes = await connection.QueryAsync<dynamic>(
            "SELECT [id], [student_name], [behaviour_type] FROM [dev].[behaviour_notes]");
        Assert.True(behaviourNotes.Count() >= 2);

        // Verify Discipline Records
        var disciplineRecords = await connection.QueryAsync<dynamic>(
            "SELECT [id], [student_name], [sanction] FROM [dev].[discipline_records]");
        Assert.True(disciplineRecords.Count() >= 2);

        // Verify Extracurricular Activities
        var clubCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM [dev].[extracurricular_activities]");
        Assert.True(clubCount >= 3);

        // Verify Extracurricular Participations
        var participations = await connection.QueryAsync<dynamic>(
            "SELECT [id], [activity_name], [student_name], [last_session_log] FROM [dev].[extracurricular_participations]");
        Assert.True(participations.Count() >= 2);
        Assert.All(participations, p => Assert.NotNull((string?)p.last_session_log));

        // Verify Achievements
        var achievements = await connection.QueryAsync<dynamic>(
            "SELECT [id], [student_name], [recognition_level] FROM [dev].[achievements]");
        Assert.True(achievements.Count() >= 2);

        // Verify Report Cards
        var ahmadCard = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT [status], [finalized_at], [general_remarks] FROM [acd].[report_cards] WHERE [id] = @Id",
            new { Id = Stage3Seeder.DefaultReportCardAhmadId });
        Assert.NotNull(ahmadCard);
        Assert.Equal("FINALIZED", (string)ahmadCard!.status);
        Assert.NotNull(ahmadCard.finalized_at);
        Assert.NotNull((string?)ahmadCard.general_remarks);

        var sitiCard = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT [status], [finalized_at] FROM [acd].[report_cards] WHERE [id] = @Id",
            new { Id = Stage3Seeder.DefaultReportCardSitiId });
        Assert.NotNull(sitiCard);
        Assert.Equal("DRAFT", (string)sitiCard!.status);
        Assert.Null(sitiCard.finalized_at);

        // Verify Report Card Entries
        var cardEntries = await connection.QueryAsync<dynamic>(
            "SELECT [numeric_score], [letter_grade], [teacher_notes] FROM [acd].[report_card_entries] WHERE [report_card_id] = @Id",
            new { Id = Stage3Seeder.DefaultReportCardAhmadId });
        Assert.Equal(2, cardEntries.Count());
        Assert.All(cardEntries, e => Assert.NotNull((string?)e.teacher_notes));

        // Verify Placement Academic Completion flag
        var isCompleted = await connection.ExecuteScalarAsync<bool>(
            "SELECT [is_academically_completed] FROM [std].[student_placements] WHERE [student_id] = @StudentId",
            new { StudentId = Stage2Seeder.DefaultStudent1Id });
        Assert.True(isCompleted);
    }
}
