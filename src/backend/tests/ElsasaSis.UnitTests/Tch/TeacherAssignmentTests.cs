using ElsasaSis.Contracts.Teaching.Events;
using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Tch;

public sealed class TeacherAssignmentTests
{
    private static readonly DateTime FixedNow = new(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private static readonly Guid OperationalPeriodId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();
    private static readonly Guid StudyGroupId = Guid.NewGuid();

    private static Teacher CreateCapableActiveTeacher(Guid? capabilitySubjectId = null)
    {
        var teacher = Teacher.Register(
            fullName: "Budi Santoso",
            dateOfBirth: new DateOnly(1985, 1, 1),
            gender: Gender.Male,
            phone: "081234567890",
            utcNow: FixedNow).Value!;

        if (capabilitySubjectId.HasValue)
        {
            teacher.AddCapability(capabilitySubjectId.Value, "Matematika", FixedNow);
        }

        return teacher;
    }

    [Fact]
    public void Assign_WhenTeacherActiveAndCapable_SucceedsAndRaisesTeacherAssignedDomainEvent()
    {
        var teacher = CreateCapableActiveTeacher(SubjectId);

        var result = TeacherAssignment.Assign(
            teacher,
            AcademicYearId,
            " 2026/2027 ",
            OperationalPeriodId,
            " Semester 1 ",
            SubjectId,
            " Matematika ",
            StudyGroupId,
            " 7A ",
            FixedNow);

        Assert.True(result.IsSuccess);
        var assignment = result.Value!;
        Assert.NotEqual(Guid.Empty, assignment.Id);
        Assert.Equal(teacher.Id, assignment.TeacherId);
        Assert.Equal("Budi Santoso", assignment.TeacherName);
        Assert.Equal(AcademicYearId, assignment.AcademicYearId);
        Assert.Equal("2026/2027", assignment.AcademicYearName);
        Assert.Equal(OperationalPeriodId, assignment.OperationalPeriodId);
        Assert.Equal("Semester 1", assignment.OperationalPeriodName);
        Assert.Equal(SubjectId, assignment.SubjectId);
        Assert.Equal("Matematika", assignment.SubjectName);
        Assert.Equal(StudyGroupId, assignment.StudyGroupId);
        Assert.Equal("7A", assignment.StudyGroupName);
        Assert.Equal(AssignmentStatus.Assigned, assignment.Status);
        Assert.Equal(FixedNow, assignment.CreatedAt);
        Assert.Equal(FixedNow, assignment.UpdatedAt);

        var domainEvent = Assert.Single(assignment.DomainEvents);
        var assignedEvent = Assert.IsType<TeacherAssignedDomainEvent>(domainEvent);
        Assert.Equal(assignment.Id, assignedEvent.AssignmentId);
        Assert.Equal(teacher.Id, assignedEvent.TeacherId);
        Assert.Equal(SubjectId, assignedEvent.SubjectId);
        Assert.Equal(StudyGroupId, assignedEvent.StudyGroupId);
        Assert.Equal(OperationalPeriodId, assignedEvent.OperationalPeriodId);
    }

    [Fact]
    public void Assign_WhenTeacherInactive_FailsWithConflict()
    {
        var teacher = CreateCapableActiveTeacher(SubjectId);
        teacher.Deactivate(FixedNow);

        var result = TeacherAssignment.Assign(
            teacher,
            AcademicYearId,
            "2026/2027",
            OperationalPeriodId,
            "Semester 1",
            SubjectId,
            "Matematika",
            StudyGroupId,
            "7A",
            FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Tch.Assignment.TeacherInactive", result.Error.Code);
    }

    [Fact]
    public void Assign_WhenTeacherLacksCapability_FailsWithConflict()
    {
        var teacher = CreateCapableActiveTeacher(capabilitySubjectId: null);

        var result = TeacherAssignment.Assign(
            teacher,
            AcademicYearId,
            "2026/2027",
            OperationalPeriodId,
            "Semester 1",
            SubjectId,
            "Matematika",
            StudyGroupId,
            "7A",
            FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Tch.Assignment.MissingCapability", result.Error.Code);
    }

    [Fact]
    public void Remove_WhenAssigned_TransitionsToRemoved_AndRejectsAlreadyRemovedWithConflict()
    {
        var teacher = CreateCapableActiveTeacher(SubjectId);
        var assignment = TeacherAssignment.Assign(
            teacher,
            AcademicYearId,
            "2026/2027",
            OperationalPeriodId,
            "Semester 1",
            SubjectId,
            "Matematika",
            StudyGroupId,
            "7A",
            FixedNow).Value!;

        var removeTime = FixedNow.AddDays(3);
        var removeResult = assignment.Remove(removeTime);

        Assert.True(removeResult.IsSuccess);
        Assert.Equal(AssignmentStatus.Removed, assignment.Status);
        Assert.Equal(removeTime, assignment.UpdatedAt);

        var secondRemoveResult = assignment.Remove(removeTime.AddHours(1));
        Assert.True(secondRemoveResult.IsFailure);
        Assert.Equal(ErrorType.Conflict, secondRemoveResult.Error.Type);
        Assert.Equal("Tch.Assignment.AlreadyRemoved", secondRemoveResult.Error.Code);
    }

    [Fact]
    public void Reactivate_WhenRemoved_TransitionsToAssignedAndRaisesDomainEvent()
    {
        var teacher = CreateCapableActiveTeacher(SubjectId);
        var assignment = TeacherAssignment.Assign(
            teacher,
            AcademicYearId,
            "2026/2027",
            OperationalPeriodId,
            "Semester 1",
            SubjectId,
            "Matematika",
            StudyGroupId,
            "7A",
            FixedNow).Value!;
        assignment.ClearDomainEvents();

        assignment.Remove(FixedNow.AddDays(1));
        var reactivateTime = FixedNow.AddDays(2);

        var reactivateResult = assignment.Reactivate(reactivateTime);

        Assert.True(reactivateResult.IsSuccess);
        Assert.Equal(AssignmentStatus.Assigned, assignment.Status);
        Assert.Equal(reactivateTime, assignment.UpdatedAt);

        var domainEvent = Assert.Single(assignment.DomainEvents);
        var assignedEvent = Assert.IsType<TeacherAssignedDomainEvent>(domainEvent);
        Assert.Equal(assignment.Id, assignedEvent.AssignmentId);
    }

    [Fact]
    public void Reactivate_WhenAlreadyAssigned_FailsWithConflict()
    {
        var teacher = CreateCapableActiveTeacher(SubjectId);
        var assignment = TeacherAssignment.Assign(
            teacher,
            AcademicYearId,
            "2026/2027",
            OperationalPeriodId,
            "Semester 1",
            SubjectId,
            "Matematika",
            StudyGroupId,
            "7A",
            FixedNow).Value!;

        var result = assignment.Reactivate(FixedNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Tch.Assignment.Duplicate", result.Error.Code);
    }

    [Fact]
    public void ReactivateWithTeacher_EnforcesActiveAndCapabilityAndRefreshesSnapshots()
    {
        var teacher = CreateCapableActiveTeacher(SubjectId);
        var assignment = TeacherAssignment.Assign(
            teacher,
            AcademicYearId,
            "2026/2027",
            OperationalPeriodId,
            "Semester 1",
            SubjectId,
            "Matematika",
            StudyGroupId,
            "7A",
            FixedNow).Value!;
        assignment.ClearDomainEvents();
        assignment.Remove(FixedNow.AddDays(1));

        teacher.Deactivate(FixedNow.AddDays(2));
        var inactiveResult = assignment.Reactivate(teacher, "2026/2027", "Semester 1", "Matematika", "7A", FixedNow.AddDays(3));
        Assert.True(inactiveResult.IsFailure);
        Assert.Equal(ErrorType.Conflict, inactiveResult.Error.Type);
        Assert.Equal("Tch.Assignment.TeacherInactive", inactiveResult.Error.Code);

        teacher.Activate(FixedNow.AddDays(3));
        teacher.RemoveCapability(SubjectId, FixedNow.AddDays(3));
        var missingCapResult = assignment.Reactivate(teacher, "2026/2027", "Semester 1", "Matematika", "7A", FixedNow.AddDays(4));
        Assert.True(missingCapResult.IsFailure);
        Assert.Equal(ErrorType.Conflict, missingCapResult.Error.Type);
        Assert.Equal("Tch.Assignment.MissingCapability", missingCapResult.Error.Code);

        teacher.AddCapability(SubjectId, "Matematika Wajib", FixedNow.AddDays(4));
        teacher.Update("Budi Santoso, M.Pd.", new DateOnly(1985, 1, 1), Gender.Male, phone: "081234567890", utcNow: FixedNow.AddDays(4));

        var successResult = assignment.Reactivate(teacher, "2026/2027 Rev", "Semester 1 Rev", "Matematika Wajib", "7A Unggulan", FixedNow.AddDays(5));
        Assert.True(successResult.IsSuccess);
        Assert.Equal(AssignmentStatus.Assigned, assignment.Status);
        Assert.Equal("Budi Santoso, M.Pd.", assignment.TeacherName);
        Assert.Equal("2026/2027 Rev", assignment.AcademicYearName);
        Assert.Equal("Semester 1 Rev", assignment.OperationalPeriodName);
        Assert.Equal("Matematika Wajib", assignment.SubjectName);
        Assert.Equal("7A Unggulan", assignment.StudyGroupName);
        Assert.Single(assignment.DomainEvents);
    }
}
