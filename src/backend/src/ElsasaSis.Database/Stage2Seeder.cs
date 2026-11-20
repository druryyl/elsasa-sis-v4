using System.Reflection;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.Database;

/// <summary>
/// Idempotently seeds Stage 2 operational fixtures (curriculum hierarchy, timetable slots, student participations,
/// sample attendance records, sample assignments with scores).
/// Intended for on-demand re-seeding in Stage 2 integration tests after database resets.
/// </summary>
public static class Stage2Seeder
{
    public static readonly Guid DefaultAcademicYearId = Guid.Parse("01A1F11A-600B-700B-8000-00000000000B");
    public static readonly Guid DefaultOperationalPeriodId = Guid.Parse("01A1F11A-600C-700C-8000-00000000000C");
    public static readonly Guid DefaultBudiTeacherId = Guid.Parse("01A1F11A-6010-7010-8000-000000000010");
    public static readonly Guid DefaultStudyGroupId7A = Guid.Parse("01A1F11A-6011-7011-8000-000000000011");
    public static readonly Guid DefaultMtkSubjectId = Guid.Parse("01A1F11A-6004-7004-8000-000000000004");

    public static readonly Guid DefaultClassroomId = Guid.Parse("01A1F11A-7001-7001-8000-000000000001");
    public const string DefaultClassroomName = "Classroom 7A";

    public static readonly Guid DefaultMathStageId = Guid.Parse("01A1F11A-7002-7002-8000-000000000002");
    public const string DefaultMathStageName = "Matematika Tingkat 7";

    public static readonly Guid DefaultEnglishStageId = Guid.Parse("01A1F11A-7003-7003-8000-000000000003");
    public const string DefaultEnglishStageName = "Bahasa Inggris Tingkat 7";

    public static readonly Guid DefaultMathLu1Id = Guid.Parse("01A1F11A-7004-7004-8000-000000000004");
    public const string DefaultMathLu1Name = "Bilangan & Aljabar";

    public static readonly Guid DefaultMathLu2Id = Guid.Parse("01A1F11A-7005-7005-8000-000000000005");
    public const string DefaultMathLu2Name = "Geometri & Pengukuran";

    public static readonly Guid DefaultEnglishLu1Id = Guid.Parse("01A1F11A-7006-7006-8000-000000000006");
    public const string DefaultEnglishLu1Name = "Greetings & Introductions";

    public static readonly Guid DefaultTimetableSlot1Id = Guid.Parse("01A1F11A-7009-7009-8000-000000000009");
    public static readonly Guid DefaultTimetableSlot2Id = Guid.Parse("01A1F11A-700A-700A-8000-00000000000A");
    public static readonly Guid DefaultTeacherAssignmentId = Guid.Parse("01A1F11A-700B-700B-8000-00000000000B");

    public static readonly Guid DefaultStudent1Id = Guid.Parse("01A1F11A-7010-7010-8000-000000000010");
    public const string DefaultStudent1Name = "Ahmad Dahlan";
    public const string DefaultStudent1Nis = "202607001";

    public static readonly Guid DefaultStudent2Id = Guid.Parse("01A1F11A-7011-7011-8000-000000000011");
    public const string DefaultStudent2Name = "Siti Nurhaliza";
    public const string DefaultStudent2Nis = "202607002";

    public static readonly Guid DefaultParticipationStudent1MathId = Guid.Parse("01A1F11A-7020-7020-8000-000000000020");
    public static readonly Guid DefaultParticipationStudent1EnglishId = Guid.Parse("01A1F11A-7021-7021-8000-000000000021");
    public static readonly Guid DefaultParticipationStudent2MathId = Guid.Parse("01A1F11A-7022-7022-8000-000000000022");
    public static readonly Guid DefaultParticipationStudent2EnglishId = Guid.Parse("01A1F11A-7023-7023-8000-000000000023");

    public static readonly Guid DefaultAttendanceStudent1Id = Guid.Parse("01A1F11A-7030-7030-8000-000000000030");
    public static readonly Guid DefaultAttendanceStudent2Id = Guid.Parse("01A1F11A-7031-7031-8000-000000000031");

    public static readonly Guid DefaultFormativeAssignmentId = Guid.Parse("01A1F11A-7040-7040-8000-000000000040");
    public const string DefaultFormativeAssignmentTitle = "Kuis Aljabar 1";

    public static readonly Guid DefaultSummativeAssignmentId = Guid.Parse("01A1F11A-7050-7050-8000-000000000050");
    public const string DefaultSummativeAssignmentTitle = "Ulangan Harian 1";

    public static async Task SeedStage2FixturesAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Ensure prerequisite Stage 1 role accounts, teacher, academic year, and study groups are in place
        await Stage1Seeder.SeedRoleAccountsAndFixturesAsync(connectionString, cancellationToken);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("100_SeedStage2Data.sql", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Embedded resource '100_SeedStage2Data.sql' was not found in ElsasaSis.Database assembly.");

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
