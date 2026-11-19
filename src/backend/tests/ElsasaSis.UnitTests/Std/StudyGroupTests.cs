using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Std;

public sealed class StudyGroupTests
{
    [Fact]
    public void Create_WithValidInputsAndHomeroomTeacher_Succeeds()
    {
        var yearId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var utcNow = new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc);

        var result = StudyGroup.Create(
            yearId,
            " 2026/2027 ",
            SchoolLevel.SMP,
            " 7 ",
            " 7A ",
            teacherId,
            " Budi Santoso ",
            utcNow);

        Assert.True(result.IsSuccess);
        var group = result.Value!;
        Assert.NotEqual(Guid.Empty, group.Id);
        Assert.Equal(yearId, group.AcademicYearId);
        Assert.Equal("2026/2027", group.AcademicYearName);
        Assert.Equal(SchoolLevel.SMP, group.SchoolLevel);
        Assert.Equal("7", group.Grade);
        Assert.Equal("7A", group.Name);
        Assert.Equal(teacherId, group.HomeroomTeacherId);
        Assert.Equal("Budi Santoso", group.HomeroomTeacherName);
        Assert.Equal(utcNow, group.CreatedAt);
        Assert.Equal(utcNow, group.UpdatedAt);
    }

    [Fact]
    public void Create_WithoutHomeroomTeacher_SucceedsWithNullHomeroomFields()
    {
        var result = StudyGroup.Create(
            Guid.NewGuid(),
            "2026/2027",
            SchoolLevel.SMP,
            "7",
            "7B");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.HomeroomTeacherId);
        Assert.Null(result.Value.HomeroomTeacherName);
    }

    [Fact]
    public void Create_WhenHomeroomTeacherReferenceIncomplete_FailsValidation()
    {
        var missingName = StudyGroup.Create(
            Guid.NewGuid(),
            "2026/2027",
            SchoolLevel.SMP,
            "7",
            "7A",
            Guid.NewGuid(),
            "   ");

        var missingId = StudyGroup.Create(
            Guid.NewGuid(),
            "2026/2027",
            SchoolLevel.SMP,
            "7",
            "7A",
            null,
            "Budi Santoso");

        Assert.True(missingName.IsFailure);
        Assert.Equal(ErrorType.Validation, missingName.Error.Type);
        Assert.Equal("Std.StudyGroup.HomeroomTeacherReferenceIncomplete", missingName.Error.Code);

        Assert.True(missingId.IsFailure);
        Assert.Equal(ErrorType.Validation, missingId.Error.Type);
        Assert.Equal("Std.StudyGroup.HomeroomTeacherReferenceIncomplete", missingId.Error.Code);
    }

    [Fact]
    public void Update_UpdatesDetailsAndAllowsClearingHomeroomTeacher()
    {
        var createdAt = new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc);
        var group = StudyGroup.Create(
            Guid.NewGuid(),
            "2026/2027",
            SchoolLevel.SMP,
            "7",
            "7A",
            Guid.NewGuid(),
            "Budi Santoso",
            createdAt).Value!;

        var result = group.Update(
            SchoolLevel.SMP,
            "8",
            "8A",
            homeroomTeacherId: null,
            homeroomTeacherName: null,
            updatedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal("8", group.Grade);
        Assert.Equal("8A", group.Name);
        Assert.Null(group.HomeroomTeacherId);
        Assert.Null(group.HomeroomTeacherName);
        Assert.Equal(createdAt, group.CreatedAt);
        Assert.Equal(updatedAt, group.UpdatedAt);
    }
}
