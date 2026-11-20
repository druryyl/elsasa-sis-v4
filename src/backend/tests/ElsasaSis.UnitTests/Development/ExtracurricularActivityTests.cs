using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Common;
using Xunit;

namespace ElsasaSis.UnitTests.Development;

public sealed class ExtracurricularActivityTests
{
    private static readonly Guid StudentId = Guid.NewGuid();
    private const string StudentName = "Ahmad Dahlan";
    private const string StudentNis = "2026001";
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private const string AcademicYearName = "2026/2027";
    private static readonly Guid SupervisorId = Guid.NewGuid();
    private const string SupervisorName = "Budi Santoso";

    [Fact]
    public void Create_WithValidParameters_InitializesWithActiveStatus()
    {
        var result = ExtracurricularActivity.Create(
            "Pramuka",
            "Kegiatan kepramukaan sekolah",
            SupervisorId,
            SupervisorName);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal("Pramuka", result.Value.Name);
        Assert.Equal("Kegiatan kepramukaan sekolah", result.Value.Description);
        Assert.Equal(SupervisorId, result.Value.SupervisorId);
        Assert.Equal(SupervisorName, result.Value.SupervisorName);
        Assert.Equal(ExtracurricularActivityStatus.Active, result.Value.Status);
        Assert.Empty(result.Value.Participations);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_Fails(string invalidName)
    {
        var result = ExtracurricularActivity.Create(
            invalidName,
            "Deskripsi",
            SupervisorId,
            SupervisorName);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.Extracurricular.NameRequired", result.Error.Code);
    }

    [Fact]
    public void UpdateDetails_ModifiesFieldsAndStatusSuccessfully()
    {
        var activity = ExtracurricularActivity.Create("Futsal", "Klub Futsal", SupervisorId, SupervisorName).Value!;
        var newSupervisorId = Guid.NewGuid();

        var updateResult = activity.UpdateDetails(
            "Futsal Club",
            "Klub olahraga futsal sekolah",
            newSupervisorId,
            "Dewi Sartika",
            ExtracurricularActivityStatus.Inactive);

        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Futsal Club", activity.Name);
        Assert.Equal("Klub olahraga futsal sekolah", activity.Description);
        Assert.Equal(newSupervisorId, activity.SupervisorId);
        Assert.Equal("Dewi Sartika", activity.SupervisorName);
        Assert.Equal(ExtracurricularActivityStatus.Inactive, activity.Status);
    }

    [Fact]
    public void EnrollStudent_WhenActive_AddsParticipation()
    {
        var activity = ExtracurricularActivity.Create("English Club", "Belajar bahasa inggris", SupervisorId, SupervisorName).Value!;

        var enrollResult = activity.EnrollStudent(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 8, 1));

        Assert.True(enrollResult.IsSuccess);
        Assert.Single(activity.Participations);
        var part = activity.Participations[0];
        Assert.Equal(activity.Id, part.ActivityId);
        Assert.Equal(StudentId, part.StudentId);
        Assert.Equal(ExtracurricularParticipationStatus.Active, part.Status);
        Assert.Equal(new DateOnly(2026, 8, 1), part.JoinedDate);
    }

    [Fact]
    public void EnrollStudent_DuplicateActiveEnrollmentInSameYear_Fails()
    {
        var activity = ExtracurricularActivity.Create("English Club", "Belajar bahasa inggris", SupervisorId, SupervisorName).Value!;

        activity.EnrollStudent(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 8, 1));

        var duplicateResult = activity.EnrollStudent(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 8, 2));

        Assert.True(duplicateResult.IsFailure);
        Assert.Equal("Dev.Extracurricular.AlreadyEnrolled", duplicateResult.Error.Code);
    }

    [Fact]
    public void EnrollStudent_WhenActivityInactive_Fails()
    {
        var activity = ExtracurricularActivity.Create("English Club", "Deskripsi", SupervisorId, SupervisorName).Value!;
        activity.UpdateDetails("English Club", "Deskripsi", SupervisorId, SupervisorName, ExtracurricularActivityStatus.Inactive);

        var result = activity.EnrollStudent(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 8, 1));

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.Extracurricular.NotActive", result.Error.Code);
    }

    [Fact]
    public void WithdrawStudent_TransitionsToWithdrawn()
    {
        var activity = ExtracurricularActivity.Create("Robotik", "Klub robotik", SupervisorId, SupervisorName).Value!;
        var part = activity.EnrollStudent(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 8, 1)).Value!;

        var withdrawResult = activity.WithdrawStudent(part.Id);

        Assert.True(withdrawResult.IsSuccess);
        Assert.Equal(ExtracurricularParticipationStatus.Withdrawn, part.Status);
        Assert.NotNull(part.WithdrawnAt);

        // Attempting to withdraw again fails
        var secondWithdraw = activity.WithdrawStudent(part.Id);
        Assert.True(secondWithdraw.IsFailure);
        Assert.Equal("Dev.Extracurricular.AlreadyWithdrawn", secondWithdraw.Error.Code);
    }

    [Fact]
    public void CompleteStudent_TransitionsToCompleted()
    {
        var activity = ExtracurricularActivity.Create("Robotik", "Klub robotik", SupervisorId, SupervisorName).Value!;
        var part = activity.EnrollStudent(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 8, 1)).Value!;

        var completeResult = activity.CompleteStudent(part.Id);

        Assert.True(completeResult.IsSuccess);
        Assert.Equal(ExtracurricularParticipationStatus.Completed, part.Status);
        Assert.NotNull(part.CompletedAt);
    }

    [Fact]
    public void RecordSessionLog_ByAssignedSupervisor_SucceedsAndUpdatesSessionInfo()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Kepramukaan", SupervisorId, SupervisorName).Value!;
        var part = activity.EnrollStudent(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 8, 1)).Value!;

        var sessionDate = new DateOnly(2026, 8, 15);
        var logResult = activity.RecordSessionLog(
            part.Id,
            SupervisorId,
            sessionDate,
            "Latihan baris berbaris dan sandi morse. Siswa hadir aktif.");

        Assert.True(logResult.IsSuccess);
        Assert.Equal("Latihan baris berbaris dan sandi morse. Siswa hadir aktif.", part.LastSessionLog);
        Assert.Equal(sessionDate, part.LastSessionDate);
    }

    [Fact]
    public void RecordSessionLog_ByNonSupervisor_FailsWithUnauthorized()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Kepramukaan", SupervisorId, SupervisorName).Value!;
        var part = activity.EnrollStudent(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 8, 1)).Value!;

        var otherTeacherId = Guid.NewGuid();
        var logResult = activity.RecordSessionLog(
            part.Id,
            otherTeacherId,
            new DateOnly(2026, 8, 15),
            "Latihan kepramukaan.");

        Assert.True(logResult.IsFailure);
        Assert.Equal("Dev.Extracurricular.NotAssignedSupervisor", logResult.Error.Code);
        Assert.Equal(ErrorType.Forbidden, logResult.Error.Type);
    }

    [Fact]
    public void RecordSessionLog_WhenParticipationWithdrawn_FailsWithInvalidStatus()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Kepramukaan", SupervisorId, SupervisorName).Value!;
        var part = activity.EnrollStudent(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 8, 1)).Value!;

        activity.WithdrawStudent(part.Id);

        var logResult = activity.RecordSessionLog(
            part.Id,
            SupervisorId,
            new DateOnly(2026, 8, 15),
            "Catatan pertemuan.");

        Assert.True(logResult.IsFailure);
        Assert.Equal("Dev.Extracurricular.ParticipationNotActive", logResult.Error.Code);
    }

    [Fact]
    public void RecordSessionLog_WhenParticipationCompleted_FailsWithInvalidStatus()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Kepramukaan", SupervisorId, SupervisorName).Value!;
        var part = activity.EnrollStudent(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 8, 1)).Value!;

        activity.CompleteStudent(part.Id);

        var logResult = activity.RecordSessionLog(
            part.Id,
            SupervisorId,
            new DateOnly(2026, 8, 15),
            "Catatan pertemuan.");

        Assert.True(logResult.IsFailure);
        Assert.Equal("Dev.Extracurricular.ParticipationNotActive", logResult.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordSessionLog_WhenLogEmpty_FailsWithValidation(string invalidLog)
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Kepramukaan", SupervisorId, SupervisorName).Value!;
        var part = activity.EnrollStudent(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 8, 1)).Value!;

        var logResult = activity.RecordSessionLog(
            part.Id,
            SupervisorId,
            new DateOnly(2026, 8, 15),
            invalidLog);

        Assert.True(logResult.IsFailure);
        Assert.Equal("Dev.Extracurricular.EmptySessionLog", logResult.Error.Code);
    }

    [Fact]
    public void RecordSessionLog_WhenLogExceeds1000Chars_FailsWithValidation()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Kepramukaan", SupervisorId, SupervisorName).Value!;
        var part = activity.EnrollStudent(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 8, 1)).Value!;

        var longLog = new string('L', 1001);
        var logResult = activity.RecordSessionLog(
            part.Id,
            SupervisorId,
            new DateOnly(2026, 8, 15),
            longLog);

        Assert.True(logResult.IsFailure);
        Assert.Equal("Dev.Extracurricular.SessionLogTooLong", logResult.Error.Code);
    }

    [Fact]
    public void Rehydrate_RestoresActivityAndParticipationsAccurately()
    {
        var actId = Guid.NewGuid();
        var partId = Guid.NewGuid();
        var sessionDate = new DateOnly(2026, 8, 20);
        var created = DateTime.UtcNow.AddDays(-5);
        var updated = DateTime.UtcNow;

        var part = ExtracurricularParticipation.Rehydrate(
            partId,
            actId,
            "Pramuka",
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            ExtracurricularParticipationStatus.Active,
            new DateOnly(2026, 8, 1),
            null,
            null,
            "Latihan tali temali",
            sessionDate,
            created,
            updated);

        var activity = ExtracurricularActivity.Rehydrate(
            actId,
            "Pramuka",
            "Kepramukaan",
            SupervisorId,
            SupervisorName,
            ExtracurricularActivityStatus.Active,
            created,
            updated,
            [part]);

        Assert.Equal(actId, activity.Id);
        Assert.Equal("Pramuka", activity.Name);
        Assert.Equal(SupervisorId, activity.SupervisorId);
        Assert.Single(activity.Participations);
        Assert.Equal(partId, activity.Participations[0].Id);
        Assert.Equal("Latihan tali temali", activity.Participations[0].LastSessionLog);
        Assert.Equal(sessionDate, activity.Participations[0].LastSessionDate);
    }
}
