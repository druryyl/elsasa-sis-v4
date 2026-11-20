using ElsasaSis.Contracts.Development.Events;
using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Common;
using Xunit;

namespace ElsasaSis.UnitTests.Development;

public sealed class DisciplineRecordTests
{
    private static readonly Guid StudentId = Guid.NewGuid();
    private const string StudentName = "Ahmad Dahlan";
    private const string StudentNis = "2026001";
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private const string AcademicYearName = "2026/2027";
    private static readonly Guid RecorderId = Guid.NewGuid();
    private const string RecorderName = "Budi Santoso";

    [Theory]
    [InlineData(DisciplineSanction.VerbalWarning)]
    [InlineData(DisciplineSanction.WrittenWarning)]
    public void CreateIncident_TeacherWithClassroomSanctions_ShouldSucceed(string sanction)
    {
        var incidentDate = new DateOnly(2026, 9, 5);
        var result = DisciplineRecord.CreateIncident(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            incidentDate,
            "KETERLAMBATAN",
            "Siswa terlambat lebih dari 3 kali berturut-turut.",
            sanction,
            RecorderId,
            RecorderName,
            role: "TEACHER");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(StudentId, result.Value.StudentId);
        Assert.Equal(sanction, result.Value.Sanction);
        Assert.Empty(result.Value.DomainEvents);
    }

    [Theory]
    [InlineData(DisciplineSanction.ParentMeeting)]
    [InlineData(DisciplineSanction.Suspension)]
    public void CreateIncident_TeacherWithEscalatedSanctions_ShouldFailWithSanctionNotAllowedForRole(string sanction)
    {
        var result = DisciplineRecord.CreateIncident(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 5),
            "PERKELAHIAN",
            "Terlibat perkelahian di lorong sekolah.",
            sanction,
            RecorderId,
            RecorderName,
            role: "TEACHER");

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.DisciplineRecord.SanctionNotAllowedForRole", result.Error.Code);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    [Theory]
    [InlineData(DisciplineSanction.ParentMeeting)]
    [InlineData(DisciplineSanction.Suspension)]
    public void CreateIncident_PrincipalWithEscalatedSanctions_ShouldSucceedAndRaiseDomainEvent(string sanction)
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
            "Terbukti memimpin tindakan kekerasan berulang.",
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
    [InlineData(DisciplineSanction.VerbalWarning)]
    [InlineData(DisciplineSanction.WrittenWarning)]
    public void CreateIncident_PrincipalWithClassroomSanctions_ShouldFailWithSanctionNotAllowedForRole(string sanction)
    {
        var result = DisciplineRecord.CreateIncident(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 10),
            "KETERLAMBATAN",
            "Teguran keterlambatan.",
            sanction,
            RecorderId,
            RecorderName,
            role: "PRINCIPAL");

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.DisciplineRecord.SanctionNotAllowedForRole", result.Error.Code);
    }

    [Theory]
    [InlineData(DisciplineSanction.VerbalWarning)]
    [InlineData(DisciplineSanction.WrittenWarning)]
    public void CreateIncident_SchoolAdminWithClassroomSanctions_ShouldSucceed(string sanction)
    {
        var result = DisciplineRecord.CreateIncident(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 10),
            "PELANGGARAN_SERAGAM",
            "Tidak memakai atribut lengkap.",
            sanction,
            RecorderId,
            RecorderName,
            role: "SCHOOL_ADMIN");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(sanction, result.Value.Sanction);
    }

    [Theory]
    [InlineData(DisciplineSanction.ParentMeeting)]
    [InlineData(DisciplineSanction.Suspension)]
    public void CreateIncident_SchoolAdminWithEscalatedSanctions_ShouldFailWithSanctionNotAllowedForRole(string sanction)
    {
        var result = DisciplineRecord.CreateIncident(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 10),
            "PELANGGARAN_BERAT",
            "Pelanggaran tata tertib berat.",
            sanction,
            RecorderId,
            RecorderName,
            role: "SCHOOL_ADMIN");

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.DisciplineRecord.SanctionNotAllowedForRole", result.Error.Code);
    }

    [Fact]
    public void CreateIncident_WithEmptyViolationType_ShouldFail()
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
    public void CreateIncident_WithViolationTypeExceeding50Chars_ShouldFail()
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
    public void CreateIncident_WithDescriptionExceeding1000Chars_ShouldFail()
    {
        var longDesc = new string('D', 1001);
        var result = DisciplineRecord.CreateIncident(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 10),
            "PELANGGARAN",
            longDesc,
            DisciplineSanction.VerbalWarning,
            RecorderId,
            RecorderName,
            role: "TEACHER");

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.DescriptionTooLong", result.Error.Code);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreAllFieldsAccurately()
    {
        var id = Guid.NewGuid();
        var incident = new DateOnly(2026, 8, 20);
        var created = DateTime.UtcNow.AddDays(-2);
        var updated = DateTime.UtcNow;

        var record = DisciplineRecord.Rehydrate(
            id,
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            incident,
            "MEROKOK",
            "Merokok di toilet sekolah",
            DisciplineSanction.ParentMeeting,
            RecorderId,
            RecorderName,
            created,
            updated);

        Assert.Equal(id, record.Id);
        Assert.Equal(StudentId, record.StudentId);
        Assert.Equal(StudentName, record.StudentName);
        Assert.Equal(StudentNis, record.StudentNis);
        Assert.Equal(AcademicYearId, record.AcademicYearId);
        Assert.Equal(AcademicYearName, record.AcademicYearName);
        Assert.Equal(incident, record.IncidentDate);
        Assert.Equal("MEROKOK", record.ViolationType);
        Assert.Equal("Merokok di toilet sekolah", record.Description);
        Assert.Equal(DisciplineSanction.ParentMeeting, record.Sanction);
        Assert.Equal(RecorderId, record.RecorderId);
        Assert.Equal(RecorderName, record.RecorderName);
        Assert.Equal(created, record.CreatedAt);
        Assert.Equal(updated, record.UpdatedAt);
    }
}
