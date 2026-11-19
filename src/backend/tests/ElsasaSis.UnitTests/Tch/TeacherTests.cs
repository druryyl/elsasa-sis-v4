using ElsasaSis.Modules.Tch.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Tch;

public sealed class TeacherTests
{
    private static readonly DateOnly ValidDob = new(1985, 1, 15);
    private static readonly DateTime FixedNow = new(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Register_WithPhoneOnlyAndBlankNipNik_SucceedsAsActiveWithNormalizedNulls()
    {
        var result = Teacher.Register(
            fullName: "  Budi Santoso  ",
            dateOfBirth: ValidDob,
            gender: Gender.Male,
            nickname: "   ",
            nip: "   ",
            nik: "",
            phone: "  081234567890  ",
            email: "  ",
            address: "  Jl. Merdeka No. 1  ",
            utcNow: FixedNow);

        Assert.True(result.IsSuccess);
        var teacher = result.Value!;
        Assert.NotEqual(Guid.Empty, teacher.Id);
        Assert.Equal("Budi Santoso", teacher.FullName);
        Assert.Null(teacher.Nickname);
        Assert.Null(teacher.Nip);
        Assert.Null(teacher.Nik);
        Assert.Equal(ValidDob, teacher.DateOfBirth);
        Assert.Equal(Gender.Male, teacher.Gender);
        Assert.Equal("081234567890", teacher.Phone);
        Assert.Null(teacher.Email);
        Assert.Equal("Jl. Merdeka No. 1", teacher.Address);
        Assert.Equal(TeacherStatus.Active, teacher.Status);
        Assert.Equal(FixedNow, teacher.CreatedAt);
        Assert.Equal(FixedNow, teacher.UpdatedAt);
        Assert.Empty(teacher.Capabilities);
    }

    [Fact]
    public void Register_WithEmailOnlyAndTrimmedNipNik_Succeeds()
    {
        var result = Teacher.Register(
            fullName: "Siti Rahma",
            dateOfBirth: ValidDob,
            gender: Gender.Female,
            nip: " 198501012010012001 ",
            nik: " 3271010101850001 ",
            email: " siti.rahma@school.id ",
            utcNow: FixedNow);

        Assert.True(result.IsSuccess);
        var teacher = result.Value!;
        Assert.Equal("198501012010012001", teacher.Nip);
        Assert.Equal("3271010101850001", teacher.Nik);
        Assert.Null(teacher.Phone);
        Assert.Equal("siti.rahma@school.id", teacher.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WhenFullNameBlank_FailsWithValidationError(string invalidName)
    {
        var result = Teacher.Register(
            fullName: invalidName,
            dateOfBirth: ValidDob,
            gender: Gender.Male,
            phone: "081234567890",
            utcNow: FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Tch.Teacher.FullNameRequired", result.Error.Code);
    }

    [Fact]
    public void Register_WhenDateOfBirthDefaultOrInFuture_FailsWithValidationError()
    {
        var defaultDobResult = Teacher.Register(
            fullName: "Budi Santoso",
            dateOfBirth: default,
            gender: Gender.Male,
            phone: "081234567890",
            utcNow: FixedNow);

        Assert.True(defaultDobResult.IsFailure);
        Assert.Equal(ErrorType.Validation, defaultDobResult.Error.Type);
        Assert.Equal("Tch.Teacher.DateOfBirthRequired", defaultDobResult.Error.Code);

        var futureDobResult = Teacher.Register(
            fullName: "Budi Santoso",
            dateOfBirth: DateOnly.FromDateTime(FixedNow).AddDays(1),
            gender: Gender.Male,
            phone: "081234567890",
            utcNow: FixedNow);

        Assert.True(futureDobResult.IsFailure);
        Assert.Equal(ErrorType.Validation, futureDobResult.Error.Type);
        Assert.Equal("Tch.Teacher.DateOfBirthInFuture", futureDobResult.Error.Code);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public void Register_WhenBothPhoneAndEmailMissing_FailsWithValidationError(string? phone, string? email)
    {
        var result = Teacher.Register(
            fullName: "Budi Santoso",
            dateOfBirth: ValidDob,
            gender: Gender.Male,
            phone: phone,
            email: email,
            utcNow: FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Tch.Teacher.ContactMethodRequired", result.Error.Code);
    }

    [Fact]
    public void Register_WhenGenderInvalid_FailsWithValidationError()
    {
        var result = Teacher.Register(
            fullName: "Budi Santoso",
            dateOfBirth: ValidDob,
            gender: (Gender)99,
            phone: "081234567890",
            utcNow: FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Tch.Teacher.InvalidGender", result.Error.Code);
    }

    [Theory]
    [InlineData("MALE", Gender.Male)]
    [InlineData("female", Gender.Female)]
    [InlineData("  Male ", Gender.Male)]
    public void ParseGender_WithValidStrings_ReturnsExpectedGender(string raw, Gender expected)
    {
        var result = Teacher.ParseGender(raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("OTHER")]
    public void ParseGender_WithInvalidStrings_FailsWithValidationError(string? raw)
    {
        var result = Teacher.ParseGender(raw);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Tch.Teacher.InvalidGender", result.Error.Code);
    }

    [Fact]
    public void Update_WithValidInputs_UpdatesProfileAndNormalizesBlankNipNik()
    {
        var teacher = Teacher.Register(
            fullName: "Budi Santoso",
            dateOfBirth: ValidDob,
            gender: Gender.Male,
            nip: "198501012010011001",
            nik: "3271010101850001",
            phone: "081234567890",
            utcNow: FixedNow).Value!;

        var updatedAt = FixedNow.AddDays(5);
        var updateResult = teacher.Update(
            fullName: "  Budi Santoso, M.Pd.  ",
            dateOfBirth: new DateOnly(1985, 2, 20),
            gender: Gender.Male,
            nickname: "Budi",
            nip: "   ",
            nik: "",
            phone: null,
            email: "  budi@school.id  ",
            address: "Jl. Pendidikan 10",
            utcNow: updatedAt);

        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Budi Santoso, M.Pd.", teacher.FullName);
        Assert.Equal("Budi", teacher.Nickname);
        Assert.Null(teacher.Nip);
        Assert.Null(teacher.Nik);
        Assert.Equal(new DateOnly(1985, 2, 20), teacher.DateOfBirth);
        Assert.Null(teacher.Phone);
        Assert.Equal("budi@school.id", teacher.Email);
        Assert.Equal("Jl. Pendidikan 10", teacher.Address);
        Assert.Equal(FixedNow, teacher.CreatedAt);
        Assert.Equal(updatedAt, teacher.UpdatedAt);
    }

    [Fact]
    public void Update_WhenRemovingLastContactMethod_FailsAndPreservesOriginalState()
    {
        var teacher = Teacher.Register(
            fullName: "Budi Santoso",
            dateOfBirth: ValidDob,
            gender: Gender.Male,
            phone: "081234567890",
            utcNow: FixedNow).Value!;

        var result = teacher.Update(
            fullName: "Changed Name",
            dateOfBirth: ValidDob,
            gender: Gender.Male,
            phone: "  ",
            email: null,
            utcNow: FixedNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Tch.Teacher.ContactMethodRequired", result.Error.Code);
        Assert.Equal("Budi Santoso", teacher.FullName);
        Assert.Equal("081234567890", teacher.Phone);
        Assert.Equal(FixedNow, teacher.UpdatedAt);
    }

    [Fact]
    public void Deactivate_And_Activate_TransitionsStatusAndPreservesCapabilities()
    {
        var teacher = Teacher.Register(
            fullName: "Budi Santoso",
            dateOfBirth: ValidDob,
            gender: Gender.Male,
            phone: "081234567890",
            utcNow: FixedNow).Value!;

        var subjectId = Guid.NewGuid();
        teacher.AddCapability(subjectId, "Matematika", FixedNow);

        var deactivateTime = FixedNow.AddDays(1);
        var deactivateResult = teacher.Deactivate(deactivateTime);

        Assert.True(deactivateResult.IsSuccess);
        Assert.Equal(TeacherStatus.Inactive, teacher.Status);
        Assert.Equal(deactivateTime, teacher.UpdatedAt);
        Assert.Single(teacher.Capabilities);

        var activateTime = FixedNow.AddDays(2);
        var activateResult = teacher.Activate(activateTime);

        Assert.True(activateResult.IsSuccess);
        Assert.Equal(TeacherStatus.Active, teacher.Status);
        Assert.Equal(activateTime, teacher.UpdatedAt);
        Assert.Single(teacher.Capabilities);
    }

    [Fact]
    public void AddCapability_WhenNew_AddsCapabilityAndEnforcesUniqueness()
    {
        var teacher = Teacher.Register(
            fullName: "Budi Santoso",
            dateOfBirth: ValidDob,
            gender: Gender.Male,
            phone: "081234567890",
            utcNow: FixedNow).Value!;

        var subjectId = Guid.NewGuid();
        var capTime = FixedNow.AddHours(2);

        var firstResult = teacher.AddCapability(subjectId, "  Matematika  ", capTime);
        Assert.True(firstResult.IsSuccess);
        Assert.Single(teacher.Capabilities);
        Assert.Equal(teacher.Id, firstResult.Value!.TeacherId);
        Assert.Equal(subjectId, firstResult.Value.SubjectId);
        Assert.Equal("Matematika", firstResult.Value.SubjectName);
        Assert.True(teacher.HasCapability(subjectId));

        var duplicateResult = teacher.AddCapability(subjectId, "Matematika", capTime.AddMinutes(5));
        Assert.True(duplicateResult.IsFailure);
        Assert.Equal(ErrorType.Conflict, duplicateResult.Error.Type);
        Assert.Equal("Tch.Capability.Duplicate", duplicateResult.Error.Code);
        Assert.Single(teacher.Capabilities);
    }

    [Fact]
    public void RemoveCapability_WhenExists_RemovesAndWhenMissing_ReturnsNotFound()
    {
        var teacher = Teacher.Register(
            fullName: "Budi Santoso",
            dateOfBirth: ValidDob,
            gender: Gender.Male,
            phone: "081234567890",
            utcNow: FixedNow).Value!;

        var subjectId = Guid.NewGuid();
        teacher.AddCapability(subjectId, "Matematika", FixedNow);

        var removeResult = teacher.RemoveCapability(subjectId, FixedNow.AddHours(1));
        Assert.True(removeResult.IsSuccess);
        Assert.Empty(teacher.Capabilities);
        Assert.False(teacher.HasCapability(subjectId));

        var removeMissingResult = teacher.RemoveCapability(subjectId);
        Assert.True(removeMissingResult.IsFailure);
        Assert.Equal(ErrorType.NotFound, removeMissingResult.Error.Type);
        Assert.Equal("Tch.Capability.NotFound", removeMissingResult.Error.Code);
    }
}
