using ElsasaSis.Contracts.Student.Events;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Std;

public sealed class StudentPlacementTests
{
    [Fact]
    public void CreateOrUpdate_WhenNewWithStudyGroup_SucceedsAndRaisesStudentPlacedDomainEvent()
    {
        var studentId = Guid.NewGuid();
        var yearId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var utcNow = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);

        var result = StudentPlacement.CreateOrUpdate(
            existing: null,
            studentId,
            yearId,
            " 2026/2027 ",
            SchoolLevel.SMP,
            " 7 ",
            groupId,
            " 7A ",
            utcNow);

        Assert.True(result.IsSuccess);
        var placement = result.Value!;
        Assert.NotEqual(Guid.Empty, placement.Id);
        Assert.Equal(studentId, placement.StudentId);
        Assert.Equal(yearId, placement.AcademicYearId);
        Assert.Equal("2026/2027", placement.AcademicYearName);
        Assert.Equal(SchoolLevel.SMP, placement.SchoolLevel);
        Assert.Equal("7", placement.Grade);
        Assert.Equal(groupId, placement.StudyGroupId);
        Assert.Equal("7A", placement.StudyGroupName);
        Assert.Equal(utcNow, placement.CreatedAt);
        Assert.Equal(utcNow, placement.UpdatedAt);

        var domainEvent = Assert.Single(placement.DomainEvents);
        var placedEvent = Assert.IsType<StudentPlacedDomainEvent>(domainEvent);
        Assert.Equal(placement.Id, placedEvent.PlacementId);
        Assert.Equal(studentId, placedEvent.StudentId);
        Assert.Equal(yearId, placedEvent.AcademicYearId);
        Assert.Equal(groupId, placedEvent.StudyGroupId);
    }

    [Fact]
    public void CreateOrUpdate_WithoutStudyGroup_SucceedsPerBrStu008()
    {
        var result = StudentPlacement.CreateOrUpdate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "2026/2027",
            SchoolLevel.SMP,
            "7");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.StudyGroupId);
        Assert.Null(result.Value.StudyGroupName);
    }

    [Fact]
    public void CreateOrUpdate_WhenStudyGroupReferencePartiallyPopulated_FailsValidationPerExtStu001()
    {
        var idWithoutName = StudentPlacement.CreateOrUpdate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "2026/2027",
            SchoolLevel.SMP,
            "7",
            Guid.NewGuid(),
            "  ");

        var nameWithoutId = StudentPlacement.CreateOrUpdate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "2026/2027",
            SchoolLevel.SMP,
            "7",
            null,
            "7A");

        Assert.True(idWithoutName.IsFailure);
        Assert.Equal(ErrorType.Validation, idWithoutName.Error.Type);
        Assert.Equal("Std.Placement.StudyGroupReferenceIncomplete", idWithoutName.Error.Code);

        Assert.True(nameWithoutId.IsFailure);
        Assert.Equal(ErrorType.Validation, nameWithoutId.Error.Type);
        Assert.Equal("Std.Placement.StudyGroupReferenceIncomplete", nameWithoutId.Error.Code);
    }

    [Fact]
    public void CreateOrUpdate_WhenExistingPlacementForSameStudentAndYear_UpdatesAndRaisesEventPerBrStu004()
    {
        var studentId = Guid.NewGuid();
        var yearId = Guid.NewGuid();
        var initialTime = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);
        var updateTime = new DateTime(2026, 7, 5, 11, 0, 0, DateTimeKind.Utc);

        var existing = StudentPlacement.CreateOrUpdate(
            studentId,
            yearId,
            "2026/2027",
            SchoolLevel.SMP,
            "7",
            Guid.NewGuid(),
            "7A",
            initialTime).Value!;
        var originalId = existing.Id;
        existing.ClearDomainEvents();

        var newGroupId = Guid.NewGuid();
        var result = StudentPlacement.CreateOrUpdate(
            existing,
            studentId,
            yearId,
            "2026/2027",
            SchoolLevel.SMP,
            "7",
            newGroupId,
            "7B",
            updateTime);

        Assert.True(result.IsSuccess);
        Assert.Same(existing, result.Value);
        Assert.Equal(originalId, existing.Id);
        Assert.Equal(newGroupId, existing.StudyGroupId);
        Assert.Equal("7B", existing.StudyGroupName);
        Assert.Equal(initialTime, existing.CreatedAt);
        Assert.Equal(updateTime, existing.UpdatedAt);

        var domainEvent = Assert.Single(existing.DomainEvents);
        var placedEvent = Assert.IsType<StudentPlacedDomainEvent>(domainEvent);
        Assert.Equal(originalId, placedEvent.PlacementId);
        Assert.Equal(newGroupId, placedEvent.StudyGroupId);
    }

    [Fact]
    public void CreateOrUpdate_WhenExistingPlacementMismatchesStudentOrYear_FailsWithConflict()
    {
        var existing = StudentPlacement.CreateOrUpdate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "2026/2027",
            SchoolLevel.SMP,
            "7").Value!;

        var result = StudentPlacement.CreateOrUpdate(
            existing,
            Guid.NewGuid(),
            existing.AcademicYearId,
            "2026/2027",
            SchoolLevel.SMP,
            "7");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Std.Placement.AggregateMismatch", result.Error.Code);
    }

    [Fact]
    public void CreateOrUpdateForStudyGroup_EnforcesMatchingYearLevelAndGradePerBrStu008()
    {
        var yearId = Guid.NewGuid();
        var studyGroup = StudyGroup.Create(
            yearId,
            "2026/2027",
            SchoolLevel.SMP,
            "7",
            "7A").Value!;

        var mismatchYear = StudentPlacement.CreateOrUpdateForStudyGroup(
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "2026/2027",
            SchoolLevel.SMP,
            "7",
            studyGroup);

        var mismatchLevel = StudentPlacement.CreateOrUpdateForStudyGroup(
            null,
            Guid.NewGuid(),
            yearId,
            "2026/2027",
            SchoolLevel.SMA,
            "7",
            studyGroup);

        var mismatchGrade = StudentPlacement.CreateOrUpdateForStudyGroup(
            null,
            Guid.NewGuid(),
            yearId,
            "2026/2027",
            SchoolLevel.SMP,
            "8",
            studyGroup);

        Assert.All([mismatchYear, mismatchLevel, mismatchGrade], r =>
        {
            Assert.True(r.IsFailure);
            Assert.Equal(ErrorType.Conflict, r.Error.Type);
            Assert.Equal("Std.Placement.StudyGroupMismatch", r.Error.Code);
        });

        var valid = StudentPlacement.CreateOrUpdateForStudyGroup(
            null,
            Guid.NewGuid(),
            yearId,
            "2026/2027",
            SchoolLevel.SMP,
            "7",
            studyGroup);

        Assert.True(valid.IsSuccess);
        Assert.Equal(studyGroup.Id, valid.Value!.StudyGroupId);
        Assert.Equal("7A", valid.Value.StudyGroupName);
    }
}
