using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Acd;

public sealed class AttendanceDomainTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 19, 8, 30, 0, DateTimeKind.Utc);
    private static readonly DateOnly ValidDate = new(2026, 11, 18);
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();
    private static readonly Guid StudyGroupId = Guid.NewGuid();
    private static readonly Guid OperationalPeriodId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();

    [Fact]
    public void Create_WhenValidParameters_SucceedsWithDeterministicValues()
    {
        var result = AttendanceRecord.Create(
            StudentId,
            " Ahmad Dahlan ",
            " 202607001 ",
            SubjectId,
            " Matematika ",
            StudyGroupId,
            " 7A ",
            OperationalPeriodId,
            " Semester 1 ",
            ValidDate,
            AttendanceStatus.Present,
            " Hadir tepat waktu ",
            TeacherId,
            " Budi Santoso ",
            FixedNow);

        Assert.True(result.IsSuccess);
        var record = result.Value!;
        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal(StudentId, record.StudentId);
        Assert.Equal("Ahmad Dahlan", record.StudentName);
        Assert.Equal("202607001", record.StudentNis);
        Assert.Equal(SubjectId, record.SubjectId);
        Assert.Equal("Matematika", record.SubjectName);
        Assert.Equal(StudyGroupId, record.StudyGroupId);
        Assert.Equal("7A", record.StudyGroupName);
        Assert.Equal(OperationalPeriodId, record.OperationalPeriodId);
        Assert.Equal("Semester 1", record.OperationalPeriodName);
        Assert.Equal(ValidDate, record.AttendanceDate);
        Assert.Equal(AttendanceStatus.Present, record.Status);
        Assert.Equal("Hadir tepat waktu", record.Notes);
        Assert.Equal(TeacherId, record.RecordedByTeacherId);
        Assert.Equal("Budi Santoso", record.RecordedByTeacherName);
        Assert.Equal(FixedNow, record.CreatedAt);
        Assert.Equal(FixedNow, record.UpdatedAt);
    }

    [Fact]
    public void Create_WhenStudentIdIsEmpty_FailsWithValidationError()
    {
        var result = AttendanceRecord.Create(
            Guid.Empty,
            "Ahmad Dahlan",
            "202607001",
            SubjectId,
            "Matematika",
            StudyGroupId,
            "7A",
            OperationalPeriodId,
            "Semester 1",
            ValidDate,
            AttendanceStatus.Present,
            null,
            TeacherId,
            "Budi Santoso",
            FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Acd.Attendance.StudentIdRequired", result.Error.Code);
    }

    [Fact]
    public void Create_WhenSubjectIdIsEmpty_FailsWithValidationError()
    {
        var result = AttendanceRecord.Create(
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            Guid.Empty,
            "Matematika",
            StudyGroupId,
            "7A",
            OperationalPeriodId,
            "Semester 1",
            ValidDate,
            AttendanceStatus.Present,
            null,
            TeacherId,
            "Budi Santoso",
            FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Acd.Attendance.SubjectIdRequired", result.Error.Code);
    }

    [Fact]
    public void Create_WhenOperationalPeriodIdIsEmpty_FailsWithValidationError()
    {
        var result = AttendanceRecord.Create(
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            SubjectId,
            "Matematika",
            StudyGroupId,
            "7A",
            Guid.Empty,
            "Semester 1",
            ValidDate,
            AttendanceStatus.Present,
            null,
            TeacherId,
            "Budi Santoso",
            FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Acd.Attendance.OperationalPeriodIdRequired", result.Error.Code);
    }

    [Fact]
    public void Create_WhenAttendanceDateIsDefault_FailsWithValidationError()
    {
        var result = AttendanceRecord.Create(
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            SubjectId,
            "Matematika",
            StudyGroupId,
            "7A",
            OperationalPeriodId,
            "Semester 1",
            default,
            AttendanceStatus.Present,
            null,
            TeacherId,
            "Budi Santoso",
            FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Acd.Attendance.AttendanceDateRequired", result.Error.Code);
    }

    [Theory]
    [InlineData("", "202607001", "Matematika", "Semester 1", "Budi Santoso", "studentName")]
    [InlineData("Ahmad Dahlan", "", "Matematika", "Semester 1", "Budi Santoso", "studentNis")]
    [InlineData("Ahmad Dahlan", "202607001", "", "Semester 1", "Budi Santoso", "subjectName")]
    [InlineData("Ahmad Dahlan", "202607001", "Matematika", "", "Budi Santoso", "operationalPeriodName")]
    [InlineData("Ahmad Dahlan", "202607001", "Matematika", "Semester 1", "", "recordedByTeacherName")]
    public void Create_WhenRequiredSnapshotIsBlank_Fails(
        string studentName,
        string studentNis,
        string subjectName,
        string operationalPeriodName,
        string recordedByTeacherName,
        string expectedField)
    {
        var result = AttendanceRecord.Create(
            StudentId,
            studentName,
            studentNis,
            SubjectId,
            subjectName,
            StudyGroupId,
            "7A",
            OperationalPeriodId,
            operationalPeriodName,
            ValidDate,
            AttendanceStatus.Present,
            null,
            TeacherId,
            recordedByTeacherName,
            FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains(expectedField, result.Error.Code, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WhenNotesExceed500Chars_Fails()
    {
        var longNotes = new string('A', 501);

        var result = AttendanceRecord.Create(
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            SubjectId,
            "Matematika",
            StudyGroupId,
            "7A",
            OperationalPeriodId,
            "Semester 1",
            ValidDate,
            AttendanceStatus.Present,
            longNotes,
            TeacherId,
            "Budi Santoso",
            FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Attendance.NotesTooLong", result.Error.Code);
    }

    [Fact]
    public void UpdateStatus_WhenValid_UpdatesFieldsAndTimestamp()
    {
        var record = AttendanceRecord.Create(
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            SubjectId,
            "Matematika",
            StudyGroupId,
            "7A",
            OperationalPeriodId,
            "Semester 1",
            ValidDate,
            AttendanceStatus.Present,
            null,
            TeacherId,
            "Budi Santoso",
            FixedNow).Value!;

        var newTeacherId = Guid.NewGuid();
        var updateTime = FixedNow.AddHours(2);

        var updateResult = record.UpdateStatus(
            AttendanceStatus.Sick,
            " Sakit flu ",
            newTeacherId,
            " Budi Santoso, M.Pd. ",
            updateTime);

        Assert.True(updateResult.IsSuccess);
        Assert.Equal(AttendanceStatus.Sick, record.Status);
        Assert.Equal("Sakit flu", record.Notes);
        Assert.Equal(newTeacherId, record.RecordedByTeacherId);
        Assert.Equal("Budi Santoso, M.Pd.", record.RecordedByTeacherName);
        Assert.Equal(FixedNow, record.CreatedAt);
        Assert.Equal(updateTime, record.UpdatedAt);
    }

    [Fact]
    public void Rehydrate_PreservesAllProperties()
    {
        var id = Guid.NewGuid();
        var createdAt = FixedNow.AddDays(-1);
        var updatedAt = FixedNow;

        var record = AttendanceRecord.Rehydrate(
            id,
            StudentId,
            "Siti Nurhaliza",
            "202607002",
            SubjectId,
            "Bahasa Inggris",
            StudyGroupId,
            "7A",
            OperationalPeriodId,
            "Semester 1",
            ValidDate,
            AttendanceStatus.Late,
            "Terlambat 10 menit",
            TeacherId,
            "Budi Santoso",
            createdAt,
            updatedAt);

        Assert.Equal(id, record.Id);
        Assert.Equal(StudentId, record.StudentId);
        Assert.Equal("Siti Nurhaliza", record.StudentName);
        Assert.Equal("202607002", record.StudentNis);
        Assert.Equal(SubjectId, record.SubjectId);
        Assert.Equal("Bahasa Inggris", record.SubjectName);
        Assert.Equal(StudyGroupId, record.StudyGroupId);
        Assert.Equal("7A", record.StudyGroupName);
        Assert.Equal(OperationalPeriodId, record.OperationalPeriodId);
        Assert.Equal("Semester 1", record.OperationalPeriodName);
        Assert.Equal(ValidDate, record.AttendanceDate);
        Assert.Equal(AttendanceStatus.Late, record.Status);
        Assert.Equal("Terlambat 10 menit", record.Notes);
        Assert.Equal(TeacherId, record.RecordedByTeacherId);
        Assert.Equal("Budi Santoso", record.RecordedByTeacherName);
        Assert.Equal(createdAt, record.CreatedAt);
        Assert.Equal(updatedAt, record.UpdatedAt);
    }

    [Theory]
    [InlineData("PRESENT", AttendanceStatus.Present)]
    [InlineData("ABSENT", AttendanceStatus.Absent)]
    [InlineData("EXCUSED", AttendanceStatus.Excused)]
    [InlineData("SICK", AttendanceStatus.Sick)]
    [InlineData("LATE", AttendanceStatus.Late)]
    [InlineData("present", AttendanceStatus.Present)]
    [InlineData(" sick ", AttendanceStatus.Sick)]
    public void AttendanceStatus_ParseAndToCode_RoundTrips(string raw, AttendanceStatus expectedStatus)
    {
        var parseResult = AttendanceStatusExtensions.ParseAttendanceStatus(raw);
        Assert.True(parseResult.IsSuccess);
        Assert.Equal(expectedStatus, parseResult.Value);
        Assert.Equal(expectedStatus.ToString().ToUpperInvariant(), parseResult.Value.ToCode());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INVALID")]
    [InlineData("UNKNOWN")]
    public void AttendanceStatus_Parse_WhenInvalid_Fails(string? raw)
    {
        var parseResult = AttendanceStatusExtensions.ParseAttendanceStatus(raw);
        Assert.True(parseResult.IsFailure);
        Assert.Equal("Acd.Attendance.InvalidStatus", parseResult.Error.Code);
    }
}
