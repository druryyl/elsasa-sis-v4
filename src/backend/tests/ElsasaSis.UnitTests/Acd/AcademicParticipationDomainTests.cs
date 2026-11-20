using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using Xunit;

namespace ElsasaSis.UnitTests.Acd;

public sealed class AcademicParticipationDomainTests
{
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _subjectStageId = Guid.NewGuid();
    private readonly Guid _subjectId = Guid.NewGuid();
    private readonly Guid _operationalPeriodId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _studyGroupId = Guid.NewGuid();

    [Fact]
    public void AcademicParticipation_Create_WithValidData_Succeeds()
    {
        var result = AcademicParticipation.Create(
            studentId: _studentId,
            studentName: "Ahmad Dahlan",
            studentNis: "NIS-001",
            subjectStageId: _subjectStageId,
            subjectStageName: "Mathematics Stage 7",
            subjectId: _subjectId,
            subjectName: "Matematika",
            operationalPeriodId: _operationalPeriodId,
            operationalPeriodName: "Semester Ganjil 2026/2027",
            academicYearId: _academicYearId,
            academicYearName: "2026/2027",
            studyGroupId: _studyGroupId,
            studyGroupName: "7A");

        Assert.True(result.IsSuccess);
        var p = result.Value!;
        Assert.NotEqual(Guid.Empty, p.Id);
        Assert.Equal(_studentId, p.StudentId);
        Assert.Equal("Ahmad Dahlan", p.StudentName);
        Assert.Equal("NIS-001", p.StudentNis);
        Assert.Equal(_subjectStageId, p.SubjectStageId);
        Assert.Equal("Mathematics Stage 7", p.SubjectStageName);
        Assert.Equal(_subjectId, p.SubjectId);
        Assert.Equal("Matematika", p.SubjectName);
        Assert.Equal(_operationalPeriodId, p.OperationalPeriodId);
        Assert.Equal("Semester Ganjil 2026/2027", p.OperationalPeriodName);
        Assert.Equal(_academicYearId, p.AcademicYearId);
        Assert.Equal("2026/2027", p.AcademicYearName);
        Assert.Equal(_studyGroupId, p.StudyGroupId);
        Assert.Equal("7A", p.StudyGroupName);
        Assert.Equal(ParticipationStatus.Active, p.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AcademicParticipation_Create_WithEmptyStudentName_Fails(string? name)
    {
        var result = AcademicParticipation.Create(
            studentId: _studentId,
            studentName: name!,
            studentNis: "NIS-001",
            subjectStageId: _subjectStageId,
            subjectStageName: "Stage 7",
            subjectId: _subjectId,
            subjectName: "Matematika",
            operationalPeriodId: _operationalPeriodId,
            operationalPeriodName: "Semester 1",
            academicYearId: _academicYearId,
            academicYearName: "2026/2027",
            studyGroupId: null,
            studyGroupName: null);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Participation.studentNameRequired", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AcademicParticipation_Create_WithEmptyStudentNis_Fails(string? nis)
    {
        var result = AcademicParticipation.Create(
            studentId: _studentId,
            studentName: "Ahmad",
            studentNis: nis!,
            subjectStageId: _subjectStageId,
            subjectStageName: "Stage 7",
            subjectId: _subjectId,
            subjectName: "Matematika",
            operationalPeriodId: _operationalPeriodId,
            operationalPeriodName: "Semester 1",
            academicYearId: _academicYearId,
            academicYearName: "2026/2027",
            studyGroupId: null,
            studyGroupName: null);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Participation.studentNisRequired", result.Error.Code);
    }

    [Fact]
    public void AcademicParticipation_Create_WithEmptyStudentId_Fails()
    {
        var result = AcademicParticipation.Create(
            studentId: Guid.Empty,
            studentName: "Ahmad",
            studentNis: "NIS-001",
            subjectStageId: _subjectStageId,
            subjectStageName: "Stage 7",
            subjectId: _subjectId,
            subjectName: "Matematika",
            operationalPeriodId: _operationalPeriodId,
            operationalPeriodName: "Semester 1",
            academicYearId: _academicYearId,
            academicYearName: "2026/2027",
            studyGroupId: null,
            studyGroupName: null);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Participation.StudentIdRequired", result.Error.Code);
    }

    [Fact]
    public void AcademicParticipation_Create_WithEmptySubjectStageId_Fails()
    {
        var result = AcademicParticipation.Create(
            studentId: _studentId,
            studentName: "Ahmad",
            studentNis: "NIS-001",
            subjectStageId: Guid.Empty,
            subjectStageName: "Stage 7",
            subjectId: _subjectId,
            subjectName: "Matematika",
            operationalPeriodId: _operationalPeriodId,
            operationalPeriodName: "Semester 1",
            academicYearId: _academicYearId,
            academicYearName: "2026/2027",
            studyGroupId: null,
            studyGroupName: null);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Participation.SubjectStageIdRequired", result.Error.Code);
    }

    [Fact]
    public void AcademicParticipation_Create_WithEmptySubjectId_Fails()
    {
        var result = AcademicParticipation.Create(
            studentId: _studentId,
            studentName: "Ahmad",
            studentNis: "NIS-001",
            subjectStageId: _subjectStageId,
            subjectStageName: "Stage 7",
            subjectId: Guid.Empty,
            subjectName: "Matematika",
            operationalPeriodId: _operationalPeriodId,
            operationalPeriodName: "Semester 1",
            academicYearId: _academicYearId,
            academicYearName: "2026/2027",
            studyGroupId: null,
            studyGroupName: null);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Participation.SubjectIdRequired", result.Error.Code);
    }

    [Fact]
    public void AcademicParticipation_Create_WithEmptyOperationalPeriodId_Fails()
    {
        var result = AcademicParticipation.Create(
            studentId: _studentId,
            studentName: "Ahmad",
            studentNis: "NIS-001",
            subjectStageId: _subjectStageId,
            subjectStageName: "Stage 7",
            subjectId: _subjectId,
            subjectName: "Matematika",
            operationalPeriodId: Guid.Empty,
            operationalPeriodName: "Semester 1",
            academicYearId: _academicYearId,
            academicYearName: "2026/2027",
            studyGroupId: null,
            studyGroupName: null);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Participation.OperationalPeriodIdRequired", result.Error.Code);
    }

    [Fact]
    public void AcademicParticipation_Create_WithEmptyAcademicYearId_Fails()
    {
        var result = AcademicParticipation.Create(
            studentId: _studentId,
            studentName: "Ahmad",
            studentNis: "NIS-001",
            subjectStageId: _subjectStageId,
            subjectStageName: "Stage 7",
            subjectId: _subjectId,
            subjectName: "Matematika",
            operationalPeriodId: _operationalPeriodId,
            operationalPeriodName: "Semester 1",
            academicYearId: Guid.Empty,
            academicYearName: "2026/2027",
            studyGroupId: null,
            studyGroupName: null);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Participation.AcademicYearIdRequired", result.Error.Code);
    }

    [Fact]
    public void AcademicParticipation_StateTransitions_CompleteAndReactivate()
    {
        var p = AcademicParticipation.Create(
            studentId: _studentId,
            studentName: "Ahmad",
            studentNis: "NIS-001",
            subjectStageId: _subjectStageId,
            subjectStageName: "Stage 7",
            subjectId: _subjectId,
            subjectName: "Matematika",
            operationalPeriodId: _operationalPeriodId,
            operationalPeriodName: "Semester 1",
            academicYearId: _academicYearId,
            academicYearName: "2026/2027",
            studyGroupId: null,
            studyGroupName: null).Value!;

        Assert.Equal(ParticipationStatus.Active, p.Status);

        var completeResult = p.Complete();
        Assert.True(completeResult.IsSuccess);
        Assert.Equal(ParticipationStatus.Completed, p.Status);

        // Idempotent complete
        Assert.True(p.Complete().IsSuccess);
        Assert.Equal(ParticipationStatus.Completed, p.Status);

        var reactivateResult = p.Reactivate();
        Assert.True(reactivateResult.IsSuccess);
        Assert.Equal(ParticipationStatus.Active, p.Status);

        // Idempotent reactivate
        Assert.True(p.Reactivate().IsSuccess);
        Assert.Equal(ParticipationStatus.Active, p.Status);
    }

    [Fact]
    public void AcademicParticipation_UpdateStudyGroup_UpdatesValues()
    {
        var p = AcademicParticipation.Create(
            studentId: _studentId,
            studentName: "Ahmad",
            studentNis: "NIS-001",
            subjectStageId: _subjectStageId,
            subjectStageName: "Stage 7",
            subjectId: _subjectId,
            subjectName: "Matematika",
            operationalPeriodId: _operationalPeriodId,
            operationalPeriodName: "Semester 1",
            academicYearId: _academicYearId,
            academicYearName: "2026/2027",
            studyGroupId: null,
            studyGroupName: null).Value!;

        var newGroupId = Guid.NewGuid();
        var updateResult = p.UpdateStudyGroup(newGroupId, " 7B ");

        Assert.True(updateResult.IsSuccess);
        Assert.Equal(newGroupId, p.StudyGroupId);
        Assert.Equal("7B", p.StudyGroupName);
    }

    [Fact]
    public void ParticipationStatus_Parse_And_ToCode()
    {
        Assert.Equal("ACTIVE", ParticipationStatus.Active.ToCode());
        Assert.Equal("COMPLETED", ParticipationStatus.Completed.ToCode());

        Assert.Equal(ParticipationStatus.Active, ParticipationStatusExtensions.ParseParticipationStatus("ACTIVE").Value);
        Assert.Equal(ParticipationStatus.Active, ParticipationStatusExtensions.ParseParticipationStatus("active").Value);
        Assert.Equal(ParticipationStatus.Completed, ParticipationStatusExtensions.ParseParticipationStatus("COMPLETED").Value);
        Assert.Equal(ParticipationStatus.Completed, ParticipationStatusExtensions.ParseParticipationStatus("completed").Value);

        var invalidResult = ParticipationStatusExtensions.ParseParticipationStatus("UNKNOWN");
        Assert.True(invalidResult.IsFailure);
        Assert.Equal("Acd.Participation.InvalidStatus", invalidResult.Error.Code);
    }

    [Fact]
    public void AcademicParticipation_Rehydrate_RestoresExactState()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-2);
        var updatedAt = DateTime.UtcNow.AddDays(-1);

        var p = AcademicParticipation.Rehydrate(
            id,
            _studentId,
            "Siti Nurhaliza",
            "NIS-002",
            _subjectStageId,
            "English Stage 7",
            _subjectId,
            "Bahasa Inggris",
            _operationalPeriodId,
            "Semester 1",
            _academicYearId,
            "2026/2027",
            _studyGroupId,
            "7A",
            ParticipationStatus.Completed,
            createdAt,
            updatedAt);

        Assert.Equal(id, p.Id);
        Assert.Equal(_studentId, p.StudentId);
        Assert.Equal("Siti Nurhaliza", p.StudentName);
        Assert.Equal("NIS-002", p.StudentNis);
        Assert.Equal(_subjectStageId, p.SubjectStageId);
        Assert.Equal("English Stage 7", p.SubjectStageName);
        Assert.Equal(_subjectId, p.SubjectId);
        Assert.Equal("Bahasa Inggris", p.SubjectName);
        Assert.Equal(_operationalPeriodId, p.OperationalPeriodId);
        Assert.Equal("Semester 1", p.OperationalPeriodName);
        Assert.Equal(_academicYearId, p.AcademicYearId);
        Assert.Equal("2026/2027", p.AcademicYearName);
        Assert.Equal(_studyGroupId, p.StudyGroupId);
        Assert.Equal("7A", p.StudyGroupName);
        Assert.Equal(ParticipationStatus.Completed, p.Status);
        Assert.Equal(createdAt, p.CreatedAt);
        Assert.Equal(updatedAt, p.UpdatedAt);
    }
}
