using ElsasaSis.Contracts.Student.Events;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Std;

public sealed class StudentTests
{
    [Fact]
    public void EnrollFromCandidate_WhenAccepted_CreatesActiveStudentClonesGuardiansAndRaisesEvent()
    {
        var enrollTime = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);
        var enrollmentDate = new DateOnly(2026, 7, 1);
        var candidate = CreateAcceptedCandidateWithTwoGuardians(new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc));
        var originalGuardianIds = candidate.Guardians.Select(g => g.Id).ToHashSet();

        var result = Student.EnrollFromCandidate(
            candidate,
            "  20260001  ",
            "  0098765432  ",
            enrollmentDate,
            enrollTime);

        Assert.True(result.IsSuccess);
        var student = result.Value!;
        Assert.NotEqual(Guid.Empty, student.Id);
        Assert.Equal(candidate.Id, student.CandidateId);
        Assert.Equal("20260001", student.Nis);
        Assert.Equal("0098765432", student.Nisn);
        Assert.Equal(candidate.FullName, student.FullName);
        Assert.Equal(candidate.DateOfBirth, student.DateOfBirth);
        Assert.Equal(candidate.Gender, student.Gender);
        Assert.Equal(enrollmentDate, student.EnrollmentDate);
        Assert.Equal(StudentStatus.Active, student.Status);
        Assert.Equal(enrollTime, student.CreatedAt);
        Assert.Equal(enrollTime, student.UpdatedAt);

        Assert.Equal(2, student.Guardians.Count);
        Assert.All(student.Guardians, g =>
        {
            Assert.NotEqual(Guid.Empty, g.Id);
            Assert.DoesNotContain(g.Id, originalGuardianIds);
            Assert.Equal(student.Id, g.StudentId);
            Assert.Equal(enrollTime, g.CreatedAt);
        });

        Assert.Equal(AdmissionStatus.Enrolled, candidate.AdmissionStatus);
        Assert.Equal(student.Id, candidate.EnrolledStudentId);

        var domainEvent = Assert.Single(student.DomainEvents);
        var enrolledEvent = Assert.IsType<StudentEnrolledDomainEvent>(domainEvent);
        Assert.Equal(student.Id, enrolledEvent.StudentId);
        Assert.Equal(candidate.Id, enrolledEvent.CandidateId);
        Assert.Equal("20260001", enrolledEvent.Nis);
        Assert.Equal(student.FullName, enrolledEvent.FullName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnrollFromCandidate_WithBlankNisn_NormalizesNisnToNullPerBrStu010(string? rawNisn)
    {
        var candidate = CreateAcceptedCandidateWithTwoGuardians();

        var result = Student.EnrollFromCandidate(
            candidate,
            "20260001",
            rawNisn,
            new DateOnly(2026, 7, 1),
            new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Nisn);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EnrollFromCandidate_WithBlankNis_FailsValidationPerBrStu001(string blankNis)
    {
        var candidate = CreateAcceptedCandidateWithTwoGuardians();

        var result = Student.EnrollFromCandidate(
            candidate,
            blankNis,
            null,
            new DateOnly(2026, 7, 1),
            new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Std.Student.NisRequired", result.Error.Code);
        Assert.Equal(AdmissionStatus.Accepted, candidate.AdmissionStatus);
    }

    [Fact]
    public void EnrollFromCandidate_WhenCandidateNotAccepted_FailsWithConflictPerBrStu005And006()
    {
        var registeredCandidate = CandidateTests.CreateRegisteredCandidate();
        var rejectedCandidate = CandidateTests.CreateRegisteredCandidate();
        Assert.True(rejectedCandidate.Reject().IsSuccess);

        var regResult = Student.EnrollFromCandidate(registeredCandidate, "20260001");
        var rejResult = Student.EnrollFromCandidate(rejectedCandidate, "20260002");

        Assert.All([regResult, rejResult], r =>
        {
            Assert.True(r.IsFailure);
            Assert.Equal(ErrorType.Conflict, r.Error.Type);
            Assert.Equal("Std.Candidate.NotAcceptedForEnrollment", r.Error.Code);
        });

        var enrolledCandidate = CreateAcceptedCandidateWithTwoGuardians();
        Assert.True(Student.EnrollFromCandidate(enrolledCandidate, "20260003").IsSuccess);

        var secondEnrollResult = Student.EnrollFromCandidate(enrolledCandidate, "20260004");
        Assert.True(secondEnrollResult.IsFailure);
        Assert.Equal(ErrorType.Conflict, secondEnrollResult.Error.Type);
        Assert.Equal("Std.Candidate.AlreadyEnrolled", secondEnrollResult.Error.Code);
    }

    [Fact]
    public void EnrollFromCandidate_WhenCandidateRehydratedWithZeroGuardians_FailsPerBrStu003()
    {
        var now = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);
        var candidateWithoutGuardians = Candidate.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "2026/2027",
            "Siti Aminah",
            null,
            new DateOnly(2013, 5, 1),
            Gender.Female,
            null,
            null,
            null,
            SchoolLevel.SMP,
            "7",
            AdmissionStatus.Accepted,
            now.AddDays(-10),
            now.AddDays(-5),
            "Approved",
            null,
            now.AddDays(-10),
            now.AddDays(-5),
            []);

        var result = Student.EnrollFromCandidate(candidateWithoutGuardians, "20260001", null, new DateOnly(2026, 7, 1), now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Std.Guardian.AtLeastOneRequired", result.Error.Code);
    }

    [Fact]
    public void UpdateProfile_WithValidInputs_UpdatesProfileAndNormalizesBlankNisnToNull()
    {
        var enrollTime = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);
        var updateTime = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        var student = CreateActiveStudent(nisn: "1234567890", utcNow: enrollTime);

        var result = student.UpdateProfile(
            "   ",
            "  Andi Wijaya Kusuma  ",
            " Kusuma ",
            new DateOnly(2013, 4, 16),
            Gender.Male,
            " 0819999999 ",
            " kusuma@example.com ",
            " Jl. Melati 9 ",
            updateTime);

        Assert.True(result.IsSuccess);
        Assert.Null(student.Nisn);
        Assert.Equal("Andi Wijaya Kusuma", student.FullName);
        Assert.Equal("Kusuma", student.Nickname);
        Assert.Equal(new DateOnly(2013, 4, 16), student.DateOfBirth);
        Assert.Equal("0819999999", student.Phone);
        Assert.Equal("kusuma@example.com", student.Email);
        Assert.Equal("Jl. Melati 9", student.Address);
        Assert.Equal(updateTime, student.UpdatedAt);
    }

    [Fact]
    public void UpdateProfile_WithBlankFullNameOrFutureDob_FailsValidationPerBrStu002()
    {
        var now = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        var student = CreateActiveStudent(utcNow: now);

        var blankNameResult = student.UpdateProfile(null, " ", null, new DateOnly(2013, 4, 15), Gender.Male, null, null, null, now);
        var futureDobResult = student.UpdateProfile(null, "Andi", null, new DateOnly(2026, 8, 2), Gender.Male, null, null, null, now);

        Assert.True(blankNameResult.IsFailure);
        Assert.Equal("Std.Person.FullNameRequired", blankNameResult.Error.Code);

        Assert.True(futureDobResult.IsFailure);
        Assert.Equal("Std.Person.DateOfBirthInvalid", futureDobResult.Error.Code);
    }

    [Fact]
    public void AddAndUpdateGuardian_WithValidAndInvalidInputs_EnforcesBrStu007()
    {
        var now = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);
        var student = CreateActiveStudent(utcNow: now);

        var invalidAdd = student.AddGuardian("Paman", (GuardianRelationshipType)42, "0812", null, null, now);
        Assert.True(invalidAdd.IsFailure);
        Assert.Equal(ErrorType.Validation, invalidAdd.Error.Type);
        Assert.Equal("Std.Guardian.RelationshipTypeInvalid", invalidAdd.Error.Code);

        var addResult = student.AddGuardian("Wali Utama", GuardianRelationshipType.Guardian, "0815555555", "wali@example.com", "Jl. Kenanga 3", now.AddHours(1));
        Assert.True(addResult.IsSuccess);
        Assert.Equal(3, student.Guardians.Count);

        var guardian = addResult.Value!;
        var updateResult = student.UpdateGuardian(guardian.Id, "Wali Baru", GuardianRelationshipType.Mother, "0816666666", null, null, now.AddHours(2));
        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Wali Baru", guardian.FullName);
        Assert.Equal(GuardianRelationshipType.Mother, guardian.RelationshipType);
        Assert.Equal("0816666666", guardian.Phone);

        var missingUpdate = student.UpdateGuardian(Guid.NewGuid(), "Ghost", GuardianRelationshipType.Father, null, null, null, now);
        Assert.True(missingUpdate.IsFailure);
        Assert.Equal(ErrorType.NotFound, missingUpdate.Error.Type);
    }

    [Fact]
    public void RemoveGuardian_RejectsRemovingLastGuardianWith409ConflictPerBrStu003()
    {
        var student = CreateActiveStudent();
        var firstGuardianId = student.Guardians[0].Id;
        var secondGuardianId = student.Guardians[1].Id;

        var firstRemove = student.RemoveGuardian(firstGuardianId);
        Assert.True(firstRemove.IsSuccess);
        Assert.Single(student.Guardians);

        var lastRemove = student.RemoveGuardian(secondGuardianId);
        Assert.True(lastRemove.IsFailure);
        Assert.Equal(ErrorType.Conflict, lastRemove.Error.Type);
        Assert.Equal("Std.Student.CannotRemoveLastGuardian", lastRemove.Error.Code);
        Assert.Single(student.Guardians);
        Assert.Equal(secondGuardianId, student.Guardians[0].Id);
    }

    private static Candidate CreateAcceptedCandidateWithTwoGuardians(DateTime? utcNow = null)
    {
        var now = utcNow ?? new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc);
        var candidate = Candidate.Register(
            Guid.NewGuid(),
            "2026/2027",
            "Andi Wijaya",
            "Andi",
            new DateOnly(2013, 4, 15),
            Gender.Male,
            "08123456789",
            "andi@example.com",
            "Jl. Mawar 1",
            SchoolLevel.SMP,
            "7",
            [
                new("Budi Santoso", GuardianRelationshipType.Father, "081234567890"),
                new("Siti Rahma", GuardianRelationshipType.Mother, "081298765432")
            ],
            now).Value!;

        Assert.True(candidate.Accept("Accepted", now).IsSuccess);
        return candidate;
    }

    private static Student CreateActiveStudent(string? nisn = "0098765432", DateTime? utcNow = null)
    {
        var now = utcNow ?? new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);
        var candidate = CreateAcceptedCandidateWithTwoGuardians(now.AddMonths(-1));
        return Student.EnrollFromCandidate(candidate, "20260001", nisn, DateOnly.FromDateTime(now), now).Value!;
    }
}
