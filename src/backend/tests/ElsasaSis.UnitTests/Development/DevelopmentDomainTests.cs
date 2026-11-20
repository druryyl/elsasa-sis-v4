using ElsasaSis.Contracts.Development.Events;
using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Development;

public sealed class DevelopmentDomainTests
{
    private static readonly Guid StudentId = Guid.NewGuid();
    private const string StudentName = "Ahmad Dahlan";
    private const string StudentNis = "2026001";
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private const string AcademicYearName = "2026/2027";
    private static readonly Guid RecorderId = Guid.NewGuid();
    private const string RecorderName = "Budi Santoso";

    [Fact]
    public void BehaviourNote_Create_WithValidPositiveAndNegative_ShouldSucceed()
    {
        // Positive
        var posResult = BehaviourNote.Create(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            BehaviourType.Positive,
            new DateOnly(2026, 9, 1),
            "Sangat aktif membantu teman dalam praktikum IPA.",
            RecorderId,
            RecorderName);

        Assert.True(posResult.IsSuccess);
        Assert.NotNull(posResult.Value);
        Assert.NotEqual(Guid.Empty, posResult.Value.Id);
        Assert.Equal(StudentId, posResult.Value.StudentId);
        Assert.Equal("POSITIVE", posResult.Value.BehaviourType);
        Assert.Equal("Sangat aktif membantu teman dalam praktikum IPA.", posResult.Value.Description);

        // Negative
        var negResult = BehaviourNote.Create(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            "negative",
            new DateOnly(2026, 9, 2),
            "Terlambat masuk kelas setelah istirahat.",
            RecorderId,
            RecorderName);

        Assert.True(negResult.IsSuccess);
        Assert.NotNull(negResult.Value);
        Assert.Equal("NEGATIVE", negResult.Value.BehaviourType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NEUTRAL")]
    [InlineData("UNKNOWN")]
    public void BehaviourNote_Create_WithInvalidType_ShouldFail(string invalidType)
    {
        var result = BehaviourNote.Create(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            invalidType,
            new DateOnly(2026, 9, 1),
            "Test description",
            RecorderId,
            RecorderName);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.BehaviourNote.InvalidType", result.Error.Code);
    }

    [Fact]
    public void BehaviourNote_Create_WithEmptyDescription_ShouldFail()
    {
        var result = BehaviourNote.Create(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            BehaviourType.Positive,
            new DateOnly(2026, 9, 1),
            "   ",
            RecorderId,
            RecorderName);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.DescriptionRequired", result.Error.Code);
    }

    [Fact]
    public void BehaviourNote_Create_WithDescriptionExceeding1000Chars_ShouldFail()
    {
        var longDesc = new string('A', 1001);
        var result = BehaviourNote.Create(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            BehaviourType.Positive,
            new DateOnly(2026, 9, 1),
            longDesc,
            RecorderId,
            RecorderName);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.DescriptionTooLong", result.Error.Code);
    }

    [Fact]
    public void BehaviourNote_Rehydrate_ShouldRestoreAllFields()
    {
        var id = Guid.NewGuid();
        var observed = new DateOnly(2026, 8, 15);
        var created = DateTime.UtcNow.AddDays(-1);
        var updated = DateTime.UtcNow;

        var note = BehaviourNote.Rehydrate(
            id,
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            BehaviourType.Positive,
            observed,
            "Rehydrated note",
            RecorderId,
            RecorderName,
            created,
            updated);

        Assert.Equal(id, note.Id);
        Assert.Equal(StudentId, note.StudentId);
        Assert.Equal(StudentName, note.StudentName);
        Assert.Equal(StudentNis, note.StudentNis);
        Assert.Equal(AcademicYearId, note.AcademicYearId);
        Assert.Equal(AcademicYearName, note.AcademicYearName);
        Assert.Equal(BehaviourType.Positive, note.BehaviourType);
        Assert.Equal(observed, note.ObservedDate);
        Assert.Equal("Rehydrated note", note.Description);
        Assert.Equal(RecorderId, note.RecorderId);
        Assert.Equal(RecorderName, note.RecorderName);
        Assert.Equal(created, note.CreatedAt);
        Assert.Equal(updated, note.UpdatedAt);
    }

    [Theory]
    [InlineData("VERBAL_WARNING")]
    [InlineData("WRITTEN_WARNING")]
    public void DisciplineRecord_CreateIncident_TeacherAllowedSanctions_ShouldSucceed(string sanction)
    {
        var result = DisciplineRecord.CreateIncident(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 5),
            "KETERLAMBATAN",
            "Siswa terlambat lebih dari 3 kali berturut-turut.",
            sanction,
            RecorderId,
            RecorderName,
            role: "TEACHER");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(sanction, result.Value.Sanction);
        Assert.Empty(result.Value.DomainEvents);
    }

    [Theory]
    [InlineData("PARENT_MEETING")]
    [InlineData("SUSPENSION")]
    public void DisciplineRecord_CreateIncident_TeacherDisallowedEscalatedSanctions_ShouldFail(string sanction)
    {
        var result = DisciplineRecord.CreateIncident(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 5),
            "PERKELAHIAN",
            "Terlibat perkelahian.",
            sanction,
            RecorderId,
            RecorderName,
            role: "TEACHER");

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.DisciplineRecord.SanctionNotAllowedForRole", result.Error.Code);
    }

    [Theory]
    [InlineData("PARENT_MEETING")]
    [InlineData("SUSPENSION")]
    public void DisciplineRecord_CreateIncident_PrincipalEscalatedSanctions_ShouldSucceedAndRaiseDomainEvent(string sanction)
    {
        var incidentDate = new DateOnly(2026, 9, 10);
        var result = DisciplineRecord.CreateIncident(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            incidentDate,
            "PERKELAHIAN_BERAT",
            "Terbukti melakukan perkelahian di area sekolah.",
            sanction,
            RecorderId,
            RecorderName,
            role: "PRINCIPAL");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(sanction, result.Value.Sanction);

        var domainEvents = result.Value.DomainEvents;
        Assert.Single(domainEvents);
        var sanctionEvent = Assert.IsType<SanctionImposedDomainEvent>(domainEvents[0]);
        Assert.Equal(result.Value.Id, sanctionEvent.DisciplineRecordId);
        Assert.Equal(StudentId, sanctionEvent.StudentId);
        Assert.Equal(sanction, sanctionEvent.Sanction);
        Assert.Equal(incidentDate.ToDateTime(TimeOnly.MinValue), sanctionEvent.IncidentDate);
    }

    [Theory]
    [InlineData("VERBAL_WARNING")]
    [InlineData("WRITTEN_WARNING")]
    public void DisciplineRecord_CreateIncident_PrincipalWithClassroomSanctions_ShouldFail(string sanction)
    {
        var result = DisciplineRecord.CreateIncident(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 10),
            "KETERLAMBATAN",
            "Keterlambatan ringan.",
            sanction,
            RecorderId,
            RecorderName,
            role: "PRINCIPAL");

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.DisciplineRecord.SanctionNotAllowedForRole", result.Error.Code);
    }

    [Fact]
    public void DisciplineRecord_CreateIncident_EmptyViolationType_ShouldFail()
    {
        var result = DisciplineRecord.CreateIncident(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 10),
            "   ",
            "Deskripsi pelanggaran",
            DisciplineSanction.VerbalWarning,
            RecorderId,
            RecorderName,
            role: "TEACHER");

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.DisciplineRecord.ViolationTypeRequired", result.Error.Code);
    }

    [Fact]
    public void DisciplineRecord_CreateIncident_ViolationTypeExceeding50Chars_ShouldFail()
    {
        var longType = new string('V', 51);
        var result = DisciplineRecord.CreateIncident(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 10),
            longType,
            "Deskripsi pelanggaran",
            DisciplineSanction.VerbalWarning,
            RecorderId,
            RecorderName,
            role: "TEACHER");

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.DisciplineRecord.ViolationTypeTooLong", result.Error.Code);
    }

    [Fact]
    public void ExtracurricularActivity_Create_ShouldInitializeWithActiveStatus()
    {
        var result = ExtracurricularActivity.Create(
            "Pramuka",
            "Kegiatan kepramukaan sekolah",
            RecorderId,
            RecorderName);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Pramuka", result.Value.Name);
        Assert.Equal("Kegiatan kepramukaan sekolah", result.Value.Description);
        Assert.Equal(RecorderId, result.Value.SupervisorId);
        Assert.Equal(RecorderName, result.Value.SupervisorName);
        Assert.Equal(ExtracurricularActivityStatus.Active, result.Value.Status);
        Assert.Empty(result.Value.Participations);
    }

    [Fact]
    public void ExtracurricularActivity_UpdateDetails_ShouldModifyFields()
    {
        var activity = ExtracurricularActivity.Create("Futsal", "Klub Futsal", RecorderId, RecorderName).Value!;
        var newSupervisorId = Guid.NewGuid();

        var updateResult = activity.UpdateDetails(
            "Futsal Club",
            "Klub olahraga futsal dan pembinaan bakat",
            newSupervisorId,
            "Dewi Sartika",
            ExtracurricularActivityStatus.Inactive);

        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Futsal Club", activity.Name);
        Assert.Equal("Klub olahraga futsal dan pembinaan bakat", activity.Description);
        Assert.Equal(newSupervisorId, activity.SupervisorId);
        Assert.Equal("Dewi Sartika", activity.SupervisorName);
        Assert.Equal(ExtracurricularActivityStatus.Inactive, activity.Status);
    }

    [Fact]
    public void ExtracurricularActivity_EnrollStudent_ShouldAddActiveParticipation()
    {
        var activity = ExtracurricularActivity.Create("English Club", "Belajar bahasa inggris", RecorderId, RecorderName).Value!;

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
    }

    [Fact]
    public void ExtracurricularActivity_EnrollStudent_DuplicateActiveEnrollment_ShouldFail()
    {
        var activity = ExtracurricularActivity.Create("English Club", "Belajar bahasa inggris", RecorderId, RecorderName).Value!;

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
    public void ExtracurricularActivity_EnrollStudent_WhenActivityInactive_ShouldFail()
    {
        var activity = ExtracurricularActivity.Create("English Club", "Deskripsi", RecorderId, RecorderName).Value!;
        activity.UpdateDetails("English Club", "Deskripsi", RecorderId, RecorderName, ExtracurricularActivityStatus.Inactive);

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
    public void ExtracurricularActivity_WithdrawStudent_ShouldTransitionToWithdrawn()
    {
        var activity = ExtracurricularActivity.Create("Robotik", "Klub robotik", RecorderId, RecorderName).Value!;
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

        // Attempting to withdraw again
        var secondWithdraw = activity.WithdrawStudent(part.Id);
        Assert.True(secondWithdraw.IsFailure);
        Assert.Equal("Dev.Extracurricular.AlreadyWithdrawn", secondWithdraw.Error.Code);
    }

    [Fact]
    public void ExtracurricularActivity_RecordSessionLog_ByAssignedSupervisor_ShouldSucceed()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Kepramukaan", RecorderId, RecorderName).Value!;
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
            RecorderId,
            sessionDate,
            "Latihan baris berbaris dan sandi morse. Siswa hadir aktif.");

        Assert.True(logResult.IsSuccess);
        Assert.Equal("Latihan baris berbaris dan sandi morse. Siswa hadir aktif.", part.LastSessionLog);
        Assert.Equal(sessionDate, part.LastSessionDate);
    }

    [Fact]
    public void ExtracurricularActivity_RecordSessionLog_ByOtherTeacher_ShouldFail()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Kepramukaan", RecorderId, RecorderName).Value!;
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
            "Log content");

        Assert.True(logResult.IsFailure);
        Assert.Equal("Dev.Extracurricular.NotAssignedSupervisor", logResult.Error.Code);
    }

    [Fact]
    public void ExtracurricularActivity_RecordSessionLog_OnWithdrawnParticipation_ShouldFail()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Kepramukaan", RecorderId, RecorderName).Value!;
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
            RecorderId,
            new DateOnly(2026, 8, 15),
            "Log content");

        Assert.True(logResult.IsFailure);
        Assert.Equal("Dev.Extracurricular.ParticipationNotActive", logResult.Error.Code);
    }

    [Theory]
    [InlineData("SPORTS", "SCHOOL")]
    [InlineData("ARTS", "CITY")]
    [InlineData("SCIENCE", "PROVINCE")]
    [InlineData("COMMUNITY", "NATIONAL")]
    [InlineData("OTHER", "NATIONAL")]
    public void Achievement_Create_WithValidTypesAndLevels_ShouldSucceed(string type, string level)
    {
        var achDate = new DateOnly(2026, 8, 20);
        var result = Achievement.Create(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            type,
            "Kompetisi Sains Madrasah 2026",
            level,
            achDate,
            "Meraih medali emas bidang Fisika Terapan.",
            RecorderId,
            RecorderName);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(type, result.Value.AchievementType);
        Assert.Equal(level, result.Value.RecognitionLevel);
        Assert.Equal("Kompetisi Sains Madrasah 2026", result.Value.EventName);
        Assert.Equal(achDate, result.Value.AchievementDate);
    }

    [Fact]
    public void Achievement_Create_WithInvalidType_ShouldFail()
    {
        var result = Achievement.Create(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            "INVALID_TYPE",
            "Event Name",
            RecognitionLevel.School,
            new DateOnly(2026, 8, 20),
            "Description",
            RecorderId,
            RecorderName);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.Achievement.InvalidType", result.Error.Code);
    }

    [Fact]
    public void Achievement_Create_WithInvalidRecognitionLevel_ShouldFail()
    {
        var result = Achievement.Create(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            AchievementType.Science,
            "Event Name",
            "INTERNATIONAL_INVALID",
            new DateOnly(2026, 8, 20),
            "Description",
            RecorderId,
            RecorderName);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.Achievement.InvalidLevel", result.Error.Code);
    }

    [Fact]
    public void Achievement_Create_WithEmptyEventName_ShouldFail()
    {
        var result = Achievement.Create(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            AchievementType.Science,
            "   ",
            RecognitionLevel.School,
            new DateOnly(2026, 8, 20),
            "Description",
            RecorderId,
            RecorderName);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.Achievement.EventNameRequired", result.Error.Code);
    }

    [Fact]
    public void Achievement_Rehydrate_ShouldRestoreAllFields()
    {
        var id = Guid.NewGuid();
        var achDate = new DateOnly(2026, 8, 20);
        var created = DateTime.UtcNow.AddDays(-2);
        var updated = DateTime.UtcNow;

        var ach = Achievement.Rehydrate(
            id,
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            AchievementType.Sports,
            "Kejuaraan Futsal Antar Sekolah",
            RecognitionLevel.City,
            achDate,
            "Juara 1 Futsal",
            RecorderId,
            RecorderName,
            created,
            updated);

        Assert.Equal(id, ach.Id);
        Assert.Equal(StudentId, ach.StudentId);
        Assert.Equal(AchievementType.Sports, ach.AchievementType);
        Assert.Equal("Kejuaraan Futsal Antar Sekolah", ach.EventName);
        Assert.Equal(RecognitionLevel.City, ach.RecognitionLevel);
        Assert.Equal(achDate, ach.AchievementDate);
        Assert.Equal("Juara 1 Futsal", ach.Description);
        Assert.Equal(created, ach.CreatedAt);
        Assert.Equal(updated, ach.UpdatedAt);
    }
}
