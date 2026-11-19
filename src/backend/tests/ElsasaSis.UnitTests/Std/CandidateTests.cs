using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Std;

public sealed class CandidateTests
{
    private static readonly Guid AdmissionYearId = Guid.NewGuid();
    private const string AdmissionYearName = "2026/2027";
    private static readonly DateOnly ValidDob = new(2013, 4, 15);

    private static IReadOnlyList<GuardianInput> ValidGuardians() =>
    [
        new("Budi Santoso", GuardianRelationshipType.Father, "081234567890", "budi@example.com", "Jl. Merdeka 10")
    ];

    [Fact]
    public void Register_WithValidInputs_SucceedsAsRegisteredWithUuidV7Ids()
    {
        var utcNow = new DateTime(2026, 2, 1, 8, 30, 0, DateTimeKind.Utc);

        var result = Candidate.Register(
            AdmissionYearId,
            "  2026/2027  ",
            "  Andi Wijaya  ",
            "  Andi ",
            ValidDob,
            Gender.Male,
            " 0811111111 ",
            " andi@example.com ",
            " Jl. Mawar 1 ",
            SchoolLevel.SMP,
            " 7 ",
            ValidGuardians(),
            utcNow);

        Assert.True(result.IsSuccess);
        var candidate = result.Value!;
        Assert.NotEqual(Guid.Empty, candidate.Id);
        Assert.Equal(AdmissionYearId, candidate.AdmissionYearId);
        Assert.Equal(AdmissionYearName, candidate.AdmissionYearName);
        Assert.Equal("Andi Wijaya", candidate.FullName);
        Assert.Equal("Andi", candidate.Nickname);
        Assert.Equal(ValidDob, candidate.DateOfBirth);
        Assert.Equal(Gender.Male, candidate.Gender);
        Assert.Equal("0811111111", candidate.Phone);
        Assert.Equal("andi@example.com", candidate.Email);
        Assert.Equal("Jl. Mawar 1", candidate.Address);
        Assert.Equal(SchoolLevel.SMP, candidate.TargetSchoolLevel);
        Assert.Equal("7", candidate.TargetGrade);
        Assert.Equal(AdmissionStatus.Registered, candidate.AdmissionStatus);
        Assert.Equal(utcNow, candidate.RegisteredAt);
        Assert.Null(candidate.DecidedAt);
        Assert.Null(candidate.DecisionNotes);
        Assert.Null(candidate.EnrolledStudentId);
        Assert.Equal(utcNow, candidate.CreatedAt);
        Assert.Equal(utcNow, candidate.UpdatedAt);

        var guardian = Assert.Single(candidate.Guardians);
        Assert.NotEqual(Guid.Empty, guardian.Id);
        Assert.Equal(candidate.Id, guardian.CandidateId);
        Assert.Equal("Budi Santoso", guardian.FullName);
        Assert.Equal(GuardianRelationshipType.Father, guardian.RelationshipType);
    }

    [Fact]
    public void Register_WithZeroGuardians_FailsValidation()
    {
        var result = Candidate.Register(
            AdmissionYearId,
            AdmissionYearName,
            "Andi Wijaya",
            null,
            ValidDob,
            Gender.Male,
            null,
            null,
            null,
            SchoolLevel.SMP,
            "7",
            []);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Std.Guardian.AtLeastOneRequired", result.Error.Code);
    }

    [Fact]
    public void Register_WithInvalidGuardianRelationshipType_FailsValidation()
    {
        IReadOnlyList<GuardianInput> invalidGuardians =
        [
            new("Paman Adi", (GuardianRelationshipType)999, "081234567890")
        ];

        var result = Candidate.Register(
            AdmissionYearId,
            AdmissionYearName,
            "Andi Wijaya",
            null,
            ValidDob,
            Gender.Male,
            null,
            null,
            null,
            SchoolLevel.SMP,
            "7",
            invalidGuardians);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Std.Guardian.RelationshipTypeInvalid", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithBlankFullName_FailsValidation(string fullName)
    {
        var result = Candidate.Register(
            AdmissionYearId,
            AdmissionYearName,
            fullName,
            null,
            ValidDob,
            Gender.Male,
            null,
            null,
            null,
            SchoolLevel.SMP,
            "7",
            ValidGuardians());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Std.Person.FullNameRequired", result.Error.Code);
    }

    [Fact]
    public void Register_WithFutureDateOfBirth_FailsValidation()
    {
        var utcNow = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var futureDob = new DateOnly(2026, 2, 2);

        var result = Candidate.Register(
            AdmissionYearId,
            AdmissionYearName,
            "Andi Wijaya",
            null,
            futureDob,
            Gender.Male,
            null,
            null,
            null,
            SchoolLevel.SMP,
            "7",
            ValidGuardians(),
            utcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Std.Person.DateOfBirthInvalid", result.Error.Code);
    }

    [Fact]
    public void Accept_WhenRegistered_TransitionsToAcceptedWithNotesAndTimestamp()
    {
        var registerTime = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc);
        var decideTime = new DateTime(2026, 2, 2, 10, 0, 0, DateTimeKind.Utc);
        var candidate = CreateRegisteredCandidate(registerTime);

        var result = candidate.Accept("  Passed interview  ", decideTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(AdmissionStatus.Accepted, candidate.AdmissionStatus);
        Assert.Equal("Passed interview", candidate.DecisionNotes);
        Assert.Equal(decideTime, candidate.DecidedAt);
        Assert.Equal(decideTime, candidate.UpdatedAt);
    }

    [Fact]
    public void Reject_WhenRegistered_TransitionsToRejectedWithNotesAndTimestamp()
    {
        var registerTime = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc);
        var decideTime = new DateTime(2026, 2, 2, 10, 0, 0, DateTimeKind.Utc);
        var candidate = CreateRegisteredCandidate(registerTime);

        var result = candidate.Reject("  Quota full  ", decideTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(AdmissionStatus.Rejected, candidate.AdmissionStatus);
        Assert.Equal("Quota full", candidate.DecisionNotes);
        Assert.Equal(decideTime, candidate.DecidedAt);
        Assert.Equal(decideTime, candidate.UpdatedAt);
    }

    [Fact]
    public void AcceptAndReject_WhenNotRegistered_FailWithConflictPerBrStu009()
    {
        var acceptedCandidate = CreateRegisteredCandidate();
        Assert.True(acceptedCandidate.Accept().IsSuccess);

        var rejectAfterAccept = acceptedCandidate.Reject("Change mind");
        Assert.True(rejectAfterAccept.IsFailure);
        Assert.Equal(ErrorType.Conflict, rejectAfterAccept.Error.Type);
        Assert.Equal("Std.Candidate.InvalidStatusTransition", rejectAfterAccept.Error.Code);
        Assert.Equal(AdmissionStatus.Accepted, acceptedCandidate.AdmissionStatus);

        var rejectedCandidate = CreateRegisteredCandidate();
        Assert.True(rejectedCandidate.Reject().IsSuccess);

        var acceptAfterReject = rejectedCandidate.Accept("Reconsider");
        Assert.True(acceptAfterReject.IsFailure);
        Assert.Equal(ErrorType.Conflict, acceptAfterReject.Error.Type);
        Assert.Equal("Std.Candidate.InvalidStatusTransition", acceptAfterReject.Error.Code);
        Assert.Equal(AdmissionStatus.Rejected, rejectedCandidate.AdmissionStatus);
    }

    [Fact]
    public void AcceptRejectAndMarkEnrolled_WhenEnrolled_FailWithConflictPerBrStu006()
    {
        var candidate = CreateRegisteredCandidate();
        Assert.True(candidate.Accept().IsSuccess);
        var studentId = Guid.NewGuid();
        Assert.True(candidate.MarkEnrolled(studentId).IsSuccess);

        var acceptResult = candidate.Accept("Cannot change");
        var rejectResult = candidate.Reject("Cannot change");
        var enrollAgainResult = candidate.MarkEnrolled(Guid.NewGuid());

        Assert.All([acceptResult, rejectResult, enrollAgainResult], r =>
        {
            Assert.True(r.IsFailure);
            Assert.Equal(ErrorType.Conflict, r.Error.Type);
            Assert.Equal("Std.Candidate.AlreadyEnrolled", r.Error.Code);
        });
        Assert.Equal(AdmissionStatus.Enrolled, candidate.AdmissionStatus);
        Assert.Equal(studentId, candidate.EnrolledStudentId);
    }

    [Fact]
    public void MarkEnrolled_WhenRegisteredOrRejected_FailsWithConflictPerBrStu005()
    {
        var registeredCandidate = CreateRegisteredCandidate();
        var rejectedCandidate = CreateRegisteredCandidate();
        Assert.True(rejectedCandidate.Reject().IsSuccess);

        var regResult = registeredCandidate.MarkEnrolled(Guid.NewGuid());
        var rejResult = rejectedCandidate.MarkEnrolled(Guid.NewGuid());

        Assert.All([regResult, rejResult], r =>
        {
            Assert.True(r.IsFailure);
            Assert.Equal(ErrorType.Conflict, r.Error.Type);
            Assert.Equal("Std.Candidate.NotAcceptedForEnrollment", r.Error.Code);
        });
    }

    [Theory]
    [InlineData("FATHER", GuardianRelationshipType.Father)]
    [InlineData("mother", GuardianRelationshipType.Mother)]
    [InlineData(" Guardian ", GuardianRelationshipType.Guardian)]
    public void ParseRelationshipType_WithValidCodes_Succeeds(string raw, GuardianRelationshipType expected)
    {
        var result = StdDomainParsing.ParseRelationshipType(raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("999")]
    [InlineData("UNCLE")]
    public void ParseRelationshipType_WithInvalidOrNumericStrings_FailsValidation(string? raw)
    {
        var result = StdDomainParsing.ParseRelationshipType(raw);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Std.Guardian.RelationshipTypeInvalid", result.Error.Code);
    }

    internal static Candidate CreateRegisteredCandidate(DateTime? utcNow = null) =>
        Candidate.Register(
            AdmissionYearId,
            AdmissionYearName,
            "Andi Wijaya",
            "Andi",
            ValidDob,
            Gender.Male,
            "08123456789",
            "andi@example.com",
            "Jl. Mawar 1",
            SchoolLevel.SMP,
            "7",
            ValidGuardians(),
            utcNow ?? new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc)).Value!;
}
