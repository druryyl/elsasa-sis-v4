using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using Xunit;

namespace ElsasaSis.UnitTests.Acd;

public sealed class LearningStructureDomainTests
{
    [Fact]
    public void Subject_Create_WithValidData_Succeeds()
    {
        var result = Subject.Create("Matematika", "MTK");

        Assert.True(result.IsSuccess);
        var subject = result.Value!;
        Assert.NotEqual(Guid.Empty, subject.Id);
        Assert.Equal("Matematika", subject.Name);
        Assert.Equal("MTK", subject.Code);
        Assert.True(subject.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Subject_Create_WithEmptyName_Fails(string? name)
    {
        var result = Subject.Create(name!);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Subject.EmptyName", result.Error.Code);
    }

    [Fact]
    public void Subject_Create_WithNameExceeding100Chars_Fails()
    {
        var longName = new string('A', 101);
        var result = Subject.Create(longName);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Subject.NameTooLong", result.Error.Code);
    }

    [Fact]
    public void Subject_Create_WithCodeExceeding30Chars_Fails()
    {
        var longCode = new string('C', 31);
        var result = Subject.Create("Matematika", longCode);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Subject.CodeTooLong", result.Error.Code);
    }

    [Fact]
    public void Subject_Update_WithValidData_UpdatesProperties()
    {
        var subject = Subject.Create("Fisika", "FIS").Value!;
        var updateResult = subject.Update("Fisika Lanjutan", "FIS-LAN", isActive: false);

        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Fisika Lanjutan", subject.Name);
        Assert.Equal("FIS-LAN", subject.Code);
        Assert.False(subject.IsActive);
    }

    [Fact]
    public void SubjectStage_Create_WithValidData_Succeeds()
    {
        var subjectId = Guid.NewGuid();
        var result = SubjectStage.Create(subjectId, "Matematika", "Stage 7", 1, "Description");

        Assert.True(result.IsSuccess);
        var stage = result.Value!;
        Assert.NotEqual(Guid.Empty, stage.Id);
        Assert.Equal(subjectId, stage.SubjectId);
        Assert.Equal("Matematika", stage.SubjectName);
        Assert.Equal("Stage 7", stage.Name);
        Assert.Equal(1, stage.SequenceOrder);
        Assert.Equal("Description", stage.Description);
        Assert.Empty(stage.LearningUnits);
        Assert.Empty(stage.GradeMappings);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SubjectStage_Create_WithInvalidSequence_Fails(int seq)
    {
        var result = SubjectStage.Create(Guid.NewGuid(), "Matematika", "Stage 7", seq, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.SubjectStage.InvalidSequence", result.Error.Code);
    }

    [Fact]
    public void SubjectStage_AddLearningUnit_WithValidData_AddsUnit()
    {
        var stage = SubjectStage.Create(Guid.NewGuid(), "Matematika", "Stage 7", 1, null).Value!;

        var result = stage.AddLearningUnit("Bilangan & Aljabar", 1, "MODULE", "Unit 1 Description");

        Assert.True(result.IsSuccess);
        var unit = result.Value!;
        Assert.NotEqual(Guid.Empty, unit.Id);
        Assert.Equal(stage.Id, unit.SubjectStageId);
        Assert.Equal("Bilangan & Aljabar", unit.Name);
        Assert.Equal(1, unit.SequenceOrder);
        Assert.Equal("MODULE", unit.UnitType);
        Assert.Single(stage.LearningUnits);
    }

    [Fact]
    public void SubjectStage_AddLearningUnit_WithDuplicateName_Fails()
    {
        var stage = SubjectStage.Create(Guid.NewGuid(), "Matematika", "Stage 7", 1, null).Value!;
        stage.AddLearningUnit("Bilangan & Aljabar", 1, "MODULE", null);

        var result = stage.AddLearningUnit("bilangan & aljabar", 2, "LESSON", null);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.LearningUnit.DuplicateName", result.Error.Code);
    }

    [Fact]
    public void SubjectStage_AddLearningUnit_WithDuplicateSequence_Fails()
    {
        var stage = SubjectStage.Create(Guid.NewGuid(), "Matematika", "Stage 7", 1, null).Value!;
        stage.AddLearningUnit("Bilangan & Aljabar", 1, "MODULE", null);

        var result = stage.AddLearningUnit("Geometri", 1, "LESSON", null);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.LearningUnit.DuplicateSequence", result.Error.Code);
    }

    [Fact]
    public void SubjectStage_AddLearningUnit_WithInvalidUnitType_Fails()
    {
        var stage = SubjectStage.Create(Guid.NewGuid(), "Matematika", "Stage 7", 1, null).Value!;

        var result = stage.AddLearningUnit("Bilangan & Aljabar", 1, "UNKNOWN_TYPE", null);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.LearningUnit.InvalidUnitType", result.Error.Code);
    }

    [Fact]
    public void SubjectStage_UpdateLearningUnit_WithValidData_UpdatesUnit()
    {
        var stage = SubjectStage.Create(Guid.NewGuid(), "Matematika", "Stage 7", 1, null).Value!;
        var unit = stage.AddLearningUnit("Bilangan", 1, "MODULE", null).Value!;

        var result = stage.UpdateLearningUnit(unit.Id, "Bilangan Baru", 1, "PACE", "New Desc");

        Assert.True(result.IsSuccess);
        Assert.Equal("Bilangan Baru", unit.Name);
        Assert.Equal("PACE", unit.UnitType);
        Assert.Equal("New Desc", unit.Description);
    }

    [Fact]
    public void SubjectStage_RemoveLearningUnit_RemovesUnit()
    {
        var stage = SubjectStage.Create(Guid.NewGuid(), "Matematika", "Stage 7", 1, null).Value!;
        var unit = stage.AddLearningUnit("Bilangan", 1, "MODULE", null).Value!;

        var result = stage.RemoveLearningUnit(unit.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(stage.LearningUnits);
    }

    [Fact]
    public void SubjectStage_ReorderUnits_WithValidData_UpdatesSequences()
    {
        var stage = SubjectStage.Create(Guid.NewGuid(), "Matematika", "Stage 7", 1, null).Value!;
        var u1 = stage.AddLearningUnit("Unit 1", 1, "MODULE", null).Value!;
        var u2 = stage.AddLearningUnit("Unit 2", 2, "MODULE", null).Value!;

        var newOrder = new Dictionary<Guid, int>
        {
            [u1.Id] = 2,
            [u2.Id] = 1
        };

        var result = stage.ReorderUnits(newOrder);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, u1.SequenceOrder);
        Assert.Equal(1, u2.SequenceOrder);
    }

    [Theory]
    [InlineData("SD", "1", true)]
    [InlineData("SD", "6", true)]
    [InlineData("SD", "7", false)]
    [InlineData("SMP", "7", true)]
    [InlineData("SMP", "9", true)]
    [InlineData("SMP", "10", false)]
    [InlineData("SMA", "10", true)]
    [InlineData("SMA", "12", true)]
    [InlineData("SMA", "6", false)]
    [InlineData("SMK", "10", true)]
    [InlineData("SMK", "12", true)]
    [InlineData("SMK", "5", false)]
    [InlineData("UNIVERSITY", "1", false)]
    public void SchoolLevel_GradeCombinationValidation(string level, string grade, bool expectedValid)
    {
        var isValid = SchoolLevel.IsValidCombination(level, grade);
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public void SubjectStage_AddGradeMapping_WithValidData_AddsMapping()
    {
        var stage = SubjectStage.Create(Guid.NewGuid(), "Matematika", "Stage 7", 1, null).Value!;

        var result = stage.AddGradeMapping("SMP", "7");

        Assert.True(result.IsSuccess);
        var mapping = result.Value!;
        Assert.NotEqual(Guid.Empty, mapping.Id);
        Assert.Equal(stage.Id, mapping.SubjectStageId);
        Assert.Equal("SMP", mapping.SchoolLevel);
        Assert.Equal("7", mapping.Grade);
        Assert.Single(stage.GradeMappings);
    }

    [Fact]
    public void SubjectStage_AddGradeMapping_WithInvalidGradeCombination_Fails()
    {
        var stage = SubjectStage.Create(Guid.NewGuid(), "Matematika", "Stage 7", 1, null).Value!;

        var result = stage.AddGradeMapping("SMP", "11");

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.GradeMapping.InvalidGradeLevel", result.Error.Code);
    }

    [Fact]
    public void SubjectStage_AddGradeMapping_WithDuplicate_Fails()
    {
        var stage = SubjectStage.Create(Guid.NewGuid(), "Matematika", "Stage 7", 1, null).Value!;
        stage.AddGradeMapping("SMP", "7");

        var result = stage.AddGradeMapping("smp", "7");

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.GradeMapping.Duplicate", result.Error.Code);
    }

    [Fact]
    public void SubjectStage_RemoveGradeMapping_RemovesMapping()
    {
        var stage = SubjectStage.Create(Guid.NewGuid(), "Matematika", "Stage 7", 1, null).Value!;
        var mapping = stage.AddGradeMapping("SMP", "7").Value!;

        var result = stage.RemoveGradeMapping(mapping.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(stage.GradeMappings);
    }
}
