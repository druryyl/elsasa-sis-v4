using System.Text.RegularExpressions;
using ElsasaSis.Database;
using Microsoft.AspNetCore.Identity;

namespace ElsasaSis.UnitTests.Database;

public sealed partial class Stage1SeedPasswordHashTests
{
    private const string SeedScriptResourceName = "ElsasaSis.Database.Scripts.099_SeedStage1Data.sql";

    [Theory]
    [InlineData("principal", "Principal123!")]
    [InlineData("teacher.budi", "Teacher123!")]
    public void SeedScript_EmbeddedPasswordHashes_PassIdentityV3Verification(string username, string expectedPassword)
    {
        var sql = ReadEmbeddedScript(SeedScriptResourceName);
        var hash = ExtractEmbeddedPasswordHash(sql, username);

        var hasher = new PasswordHasher<object>();
        var validResult = hasher.VerifyHashedPassword(new object(), hash, expectedPassword);
        var invalidResult = hasher.VerifyHashedPassword(new object(), hash, "WrongPassword123!");

        Assert.NotEqual(PasswordVerificationResult.Failed, validResult);
        Assert.Equal(PasswordVerificationResult.Success, validResult);
        Assert.Equal(PasswordVerificationResult.Failed, invalidResult);
    }

    [Theory]
    [InlineData("ElsasaSis.Database.Scripts.011_CreateOprCalendarAndRoomTables.sql")]
    [InlineData("ElsasaSis.Database.Scripts.012_CreateOprAnnouncementTables.sql")]
    [InlineData("ElsasaSis.Database.Scripts.020_CreateStdCandidateTables.sql")]
    [InlineData("ElsasaSis.Database.Scripts.021_CreateStdStudentTables.sql")]
    [InlineData("ElsasaSis.Database.Scripts.022_CreateStdStudyGroupTables.sql")]
    [InlineData("ElsasaSis.Database.Scripts.030_CreateAcdSubjectTables.sql")]
    [InlineData("ElsasaSis.Database.Scripts.040_CreateTchTeacherTables.sql")]
    [InlineData("ElsasaSis.Database.Scripts.041_CreateTchAssignmentTables.sql")]
    [InlineData("ElsasaSis.Database.Scripts.099_SeedStage1Data.sql")]
    public void Stage1Scripts_AreEmbeddedAndFollowPurePersistenceRules(string resourceName)
    {
        var sql = ReadEmbeddedScript(resourceName);

        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.DoesNotMatch(ForbiddenPersistenceConstructRegex(), sql);
    }

    [Theory]
    [InlineData("PA-BP")]
    [InlineData("PPKN")]
    [InlineData("BIND")]
    [InlineData("MTK")]
    [InlineData("IPA")]
    [InlineData("IPS")]
    [InlineData("BING")]
    [InlineData("SNB")]
    [InlineData("PJOK")]
    [InlineData("INF")]
    public void SubjectMigration_ContainsStandardK12SubjectCode(string subjectCode)
    {
        var sql = ReadEmbeddedScript("ElsasaSis.Database.Scripts.030_CreateAcdSubjectTables.sql");

        Assert.Contains($"N'{subjectCode}'", sql, StringComparison.Ordinal);
    }

    private static string ReadEmbeddedScript(string resourceName)
    {
        var assembly = typeof(DatabaseMigrator).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    private static string ExtractEmbeddedPasswordHash(string sql, string username)
    {
        var escapedUsername = Regex.Escape(username);
        var match = Regex.Match(
            sql,
            $"'{escapedUsername}',\\s*'(AQAAAA[^']+)'",
            RegexOptions.CultureInvariant);

        Assert.True(match.Success, $"Could not find embedded PBKDF2 password hash for '{username}' in {SeedScriptResourceName}.");
        return match.Groups[1].Value;
    }

    [GeneratedRegex(@"\b(FOREIGN\s+KEY|REFERENCES\s+\[|DEFAULT\s+(?:\(|'|0|1|GETDATE|SYSUTCDATETIME|NEWID)|CREATE\s+TRIGGER|CREATE\s+PROC(?:EDURE)?|CONSTRAINT\s+\[?CK_|CHECK\s*\()\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ForbiddenPersistenceConstructRegex();
}
