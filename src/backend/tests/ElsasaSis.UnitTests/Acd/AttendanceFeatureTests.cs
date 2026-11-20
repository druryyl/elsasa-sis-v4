using System.Reflection;
using ElsasaSis.Contracts.Academic;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Acd;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.Modules.Acd.Features.Attendance.GetSubjectAttendanceSheet;
using ElsasaSis.Modules.Acd.Features.Attendance.RecordSubjectAttendance;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;
using Microsoft.Extensions.DependencyInjection;

namespace ElsasaSis.UnitTests.Acd;

public sealed class AttendanceFeatureTests
{
    private static readonly DateOnly ValidDate = new(2026, 11, 19);
    private static readonly Guid SubjectId = Guid.NewGuid();
    private static readonly Guid StudyGroupId = Guid.NewGuid();
    private static readonly Guid OperationalPeriodId = Guid.NewGuid();
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private static readonly Guid Student1Id = Guid.NewGuid();
    private static readonly Guid Student2Id = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();

    [Fact]
    public async Task RecordAttendanceValidator_WhenValid_PassesValidation()
    {
        var validator = new RecordAttendanceValidator();
        var request = new RecordAttendanceRequest(
            SubjectId,
            StudyGroupId,
            ValidDate,
            OperationalPeriodId,
            [
                new RecordAttendanceItemRequest(Student1Id, "PRESENT", null),
                new RecordAttendanceItemRequest(Student2Id, "SICK", "Demam")
            ]);

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task RecordAttendanceValidator_WhenMissingRequiredFields_FailsValidation()
    {
        var validator = new RecordAttendanceValidator();
        var request = new RecordAttendanceRequest(
            Guid.Empty,
            Guid.Empty,
            default,
            Guid.Empty,
            []);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SubjectId");
        Assert.Contains(result.Errors, e => e.PropertyName == "StudyGroupId");
        Assert.Contains(result.Errors, e => e.PropertyName == "OperationalPeriodId");
        Assert.Contains(result.Errors, e => e.PropertyName == "AttendanceDate");
        Assert.Contains(result.Errors, e => e.PropertyName == "Items");
    }

    [Fact]
    public async Task RecordAttendanceValidator_WhenItemStatusInvalid_FailsValidation()
    {
        var validator = new RecordAttendanceValidator();
        var request = new RecordAttendanceRequest(
            SubjectId,
            StudyGroupId,
            ValidDate,
            OperationalPeriodId,
            [new RecordAttendanceItemRequest(Student1Id, "NOT_A_VALID_STATUS", null)]);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Status"));
    }

    [Fact]
    public async Task RecordSubjectAttendanceHandler_WhenItemsNullOrEmpty_ReturnsValidationError()
    {
        var harness = new AttendanceTestHarness();
        var handler = harness.CreateHandler();

        var request = new RecordAttendanceRequest(
            SubjectId,
            StudyGroupId,
            ValidDate,
            OperationalPeriodId,
            []);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Acd.Attendance.ItemsRequired", result.Error.Code);
    }

    [Fact]
    public async Task RecordSubjectAttendanceHandler_WhenDuplicateStudentInBatch_ReturnsValidationError()
    {
        var harness = new AttendanceTestHarness();
        var handler = harness.CreateHandler();

        var request = new RecordAttendanceRequest(
            SubjectId,
            StudyGroupId,
            ValidDate,
            OperationalPeriodId,
            [
                new RecordAttendanceItemRequest(Student1Id, "PRESENT", null),
                new RecordAttendanceItemRequest(Student1Id, "ABSENT", null)
            ]);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Acd.Attendance.DuplicateStudentInBatch", result.Error.Code);
    }

    [Fact]
    public async Task RecordSubjectAttendanceHandler_WhenOperationalPeriodNotFound_Returns404()
    {
        var harness = new AttendanceTestHarness { Period = null };
        var handler = harness.CreateHandler();

        var request = new RecordAttendanceRequest(
            SubjectId,
            StudyGroupId,
            ValidDate,
            OperationalPeriodId,
            [new RecordAttendanceItemRequest(Student1Id, "PRESENT", null)]);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Acd.Attendance.OperationalPeriodNotFound", result.Error.Code);
    }

    [Fact]
    public async Task RecordSubjectAttendanceHandler_WhenSubjectNotFound_Returns404()
    {
        var harness = new AttendanceTestHarness { Subject = null };
        var handler = harness.CreateHandler();

        var request = new RecordAttendanceRequest(
            SubjectId,
            StudyGroupId,
            ValidDate,
            OperationalPeriodId,
            [new RecordAttendanceItemRequest(Student1Id, "PRESENT", null)]);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Acd.Attendance.SubjectNotFound", result.Error.Code);
    }

    [Fact]
    public async Task RecordSubjectAttendanceHandler_WhenStudyGroupNotFound_Returns404()
    {
        var harness = new AttendanceTestHarness { StudyGroup = null };
        var handler = harness.CreateHandler();

        var request = new RecordAttendanceRequest(
            SubjectId,
            StudyGroupId,
            ValidDate,
            OperationalPeriodId,
            [new RecordAttendanceItemRequest(Student1Id, "PRESENT", null)]);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Acd.Attendance.StudyGroupNotFound", result.Error.Code);
    }

    [Fact]
    public async Task RecordSubjectAttendanceHandler_WhenStudentNotEnrolled_Returns409Conflict()
    {
        var harness = new AttendanceTestHarness();
        // Student1 is enrolled, Student2 is not
        harness.EnrolledStudents =
        [
            new EnrolledStudentSnapshotDto(Student1Id, "Ahmad Dahlan", "202607001")
        ];

        var handler = harness.CreateHandler();

        var request = new RecordAttendanceRequest(
            SubjectId,
            StudyGroupId,
            ValidDate,
            OperationalPeriodId,
            [
                new RecordAttendanceItemRequest(Student1Id, "PRESENT", null),
                new RecordAttendanceItemRequest(Student2Id, "PRESENT", null)
            ]);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Acd.Attendance.StudentNotEnrolled", result.Error.Code);
    }

    [Fact]
    public async Task RecordSubjectAttendanceHandler_WhenAllNewRecords_CreatesAndUpsertsBatch()
    {
        var harness = new AttendanceTestHarness();
        var handler = harness.CreateHandler();

        var request = new RecordAttendanceRequest(
            SubjectId,
            StudyGroupId,
            ValidDate,
            OperationalPeriodId,
            [
                new RecordAttendanceItemRequest(Student1Id, "PRESENT", null),
                new RecordAttendanceItemRequest(Student2Id, "LATE", "Macet")
            ]);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.RecordedCount);
        Assert.Equal(2, harness.Repository.UpsertedRecords.Count);

        var record1 = harness.Repository.UpsertedRecords.First(r => r.StudentId == Student1Id);
        Assert.Equal("Ahmad Dahlan", record1.StudentName);
        Assert.Equal("202607001", record1.StudentNis);
        Assert.Equal("Matematika", record1.SubjectName);
        Assert.Equal("7A", record1.StudyGroupName);
        Assert.Equal("Semester 1", record1.OperationalPeriodName);
        Assert.Equal(AttendanceStatus.Present, record1.Status);
        Assert.Null(record1.Notes);
        Assert.Equal(TeacherId, record1.RecordedByTeacherId);
        Assert.Equal("Budi Santoso", record1.RecordedByTeacherName);

        var record2 = harness.Repository.UpsertedRecords.First(r => r.StudentId == Student2Id);
        Assert.Equal("Siti Nurhaliza", record2.StudentName);
        Assert.Equal("202607002", record2.StudentNis);
        Assert.Equal(AttendanceStatus.Late, record2.Status);
        Assert.Equal("Macet", record2.Notes);
    }

    [Fact]
    public async Task RecordSubjectAttendanceHandler_WhenRecordAlreadyExists_UpdatesExistingAggregate()
    {
        var harness = new AttendanceTestHarness();

        var existingRecord = AttendanceRecord.Create(
            Student1Id,
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
            DateTime.UtcNow.AddHours(-1)).Value!;

        harness.Repository.ExistingRecords = [existingRecord];

        var handler = harness.CreateHandler();

        var request = new RecordAttendanceRequest(
            SubjectId,
            StudyGroupId,
            ValidDate,
            OperationalPeriodId,
            [
                new RecordAttendanceItemRequest(Student1Id, "SICK", "Sakit flu")
            ]);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.RecordedCount);
        Assert.Single(harness.Repository.UpsertedRecords);

        var updated = harness.Repository.UpsertedRecords[0];
        Assert.Equal(existingRecord.Id, updated.Id);
        Assert.Equal(AttendanceStatus.Sick, updated.Status);
        Assert.Equal("Sakit flu", updated.Notes);
    }

    [Fact]
    public async Task RecordSubjectAttendanceHandler_WhenTeacherIdIsNull_FallsBackToUserId()
    {
        var harness = new AttendanceTestHarness();
        var fallbackUserId = Guid.NewGuid();
        harness.CurrentUser = new MockCurrentUser
        {
            TeacherId = null,
            UserId = fallbackUserId,
            FullName = "Admin User"
        };

        var handler = harness.CreateHandler();

        var request = new RecordAttendanceRequest(
            SubjectId,
            StudyGroupId,
            ValidDate,
            OperationalPeriodId,
            [new RecordAttendanceItemRequest(Student1Id, "PRESENT", null)]);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        var record = harness.Repository.UpsertedRecords.Single();
        Assert.Equal(fallbackUserId, record.RecordedByTeacherId);
        Assert.Equal("Admin User", record.RecordedByTeacherName);
    }

    [Fact]
    public async Task GetSubjectAttendanceSheetHandler_WhenQueryFieldsEmpty_ReturnsValidationError()
    {
        var handler = new GetSubjectAttendanceSheetHandler(new MockSqlConnectionFactory());

        var emptySubject = await handler.HandleAsync(new GetSubjectAttendanceSheetQuery(Guid.Empty, StudyGroupId, ValidDate, OperationalPeriodId));
        Assert.True(emptySubject.IsFailure);
        Assert.Equal("Acd.Attendance.SubjectIdRequired", emptySubject.Error.Code);

        var emptyGroup = await handler.HandleAsync(new GetSubjectAttendanceSheetQuery(SubjectId, Guid.Empty, ValidDate, OperationalPeriodId));
        Assert.True(emptyGroup.IsFailure);
        Assert.Equal("Acd.Attendance.StudyGroupIdRequired", emptyGroup.Error.Code);

        var emptyPeriod = await handler.HandleAsync(new GetSubjectAttendanceSheetQuery(SubjectId, StudyGroupId, ValidDate, Guid.Empty));
        Assert.True(emptyPeriod.IsFailure);
        Assert.Equal("Acd.Attendance.OperationalPeriodIdRequired", emptyPeriod.Error.Code);

        var emptyDate = await handler.HandleAsync(new GetSubjectAttendanceSheetQuery(SubjectId, StudyGroupId, default, OperationalPeriodId));
        Assert.True(emptyDate.IsFailure);
        Assert.Equal("Acd.Attendance.AttendanceDateRequired", emptyDate.Error.Code);
    }

    [Fact]
    public void DependencyInjection_AddAcdModule_RegistersAttendanceRepositoryAndHandlers()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISqlConnectionFactory>(new MockSqlConnectionFactory());
        services.AddSingleton<ICurrentUser>(new MockCurrentUser());
        services.AddSingleton<IOperationalContract>(new MockOperationalContract());
        services.AddSingleton<IAcademicContract>(new MockAcademicContract());
        services.AddSingleton<IStudentContract>(new MockStudentContract());

        services.AddAcdModule();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IAttendanceRecordRepository>());
        Assert.NotNull(scope.ServiceProvider.GetService<RecordSubjectAttendanceHandler>());
        Assert.NotNull(scope.ServiceProvider.GetService<GetSubjectAttendanceSheetHandler>());
    }

    [Fact]
    public void AcdWriteHandlers_ShouldNotDependOn_Dapper_Or_SqlConnectionFactory()
    {
        var writeHandlers = typeof(AcdAssemblyMarker).Assembly
            .GetTypes()
            .Where(t => t.Name.EndsWith("Handler", StringComparison.Ordinal) && !t.Name.StartsWith("Get", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(writeHandlers);

        string[] forbidden = ["Dapper", typeof(ISqlConnectionFactory).FullName!];

        foreach (var handler in writeHandlers)
        {
            var ctorTypes = handler.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType.FullName ?? p.ParameterType.Name);

            var fieldTypes = handler.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Select(f => f.FieldType.FullName ?? f.FieldType.Name);

            foreach (var typeName in ctorTypes.Concat(fieldTypes))
            {
                Assert.False(
                    forbidden.Any(f => typeName.Contains(f, StringComparison.Ordinal)),
                    $"Write handler '{handler.FullName}' must not depend on Dapper or ISqlConnectionFactory. Found: '{typeName}'.");
            }
        }
    }

    private sealed class AttendanceTestHarness
    {
        public MockAttendanceRecordRepository Repository { get; } = new();
        public MockOperationalContract OperationalContract { get; } = new();
        public MockAcademicContract AcademicContract { get; } = new();
        public MockStudentContract StudentContract { get; } = new();
        public MockCurrentUser CurrentUser { get; set; } = new()
        {
            TeacherId = AttendanceFeatureTests.TeacherId,
            FullName = "Budi Santoso",
            Username = "teacher.budi"
        };

        public OperationalPeriodSummaryDto? Period { get; set; } = new(
            OperationalPeriodId,
            AcademicYearId,
            "2026/2027",
            "Semester 1",
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 12, 31),
            "ACTIVE");

        public SubjectDto? Subject { get; set; } = new(SubjectId, "Matematika", "MTK");

        public StudyGroupSummaryDto? StudyGroup { get; set; } = new(
            StudyGroupId,
            AcademicYearId,
            "2026/2027",
            "SMP",
            "7",
            "7A",
            null,
            null);

        public List<EnrolledStudentSnapshotDto> EnrolledStudents { get; set; } =
        [
            new EnrolledStudentSnapshotDto(Student1Id, "Ahmad Dahlan", "202607001"),
            new EnrolledStudentSnapshotDto(Student2Id, "Siti Nurhaliza", "202607002")
        ];

        public RecordSubjectAttendanceHandler CreateHandler()
        {
            OperationalContract.PeriodResult = Period;
            AcademicContract.SubjectResult = Subject;
            StudentContract.StudyGroupResult = StudyGroup;
            Repository.EnrolledStudentsResult = EnrolledStudents;

            return new RecordSubjectAttendanceHandler(
                Repository,
                OperationalContract,
                AcademicContract,
                StudentContract,
                CurrentUser);
        }
    }

    private sealed class MockAttendanceRecordRepository : IAttendanceRecordRepository
    {
        public List<AttendanceRecord> ExistingRecords { get; set; } = [];
        public List<EnrolledStudentSnapshotDto> EnrolledStudentsResult { get; set; } = [];
        public List<AttendanceRecord> UpsertedRecords { get; } = [];
        public Result UpsertResult { get; set; } = Result.Success();

        public Task<AttendanceRecord?> GetByStudentSubjectAndDateAsync(
            Guid studentId,
            Guid subjectId,
            DateOnly attendanceDate,
            CancellationToken cancellationToken = default)
        {
            var found = ExistingRecords.FirstOrDefault(r =>
                r.StudentId == studentId && r.SubjectId == subjectId && r.AttendanceDate == attendanceDate);
            return Task.FromResult(found);
        }

        public Task<IReadOnlyList<AttendanceRecord>> GetBySubjectGroupAndDateAsync(
            Guid subjectId,
            Guid studyGroupId,
            DateOnly attendanceDate,
            CancellationToken cancellationToken = default)
        {
            var found = ExistingRecords.Where(r =>
                r.SubjectId == subjectId && r.StudyGroupId == studyGroupId && r.AttendanceDate == attendanceDate).ToList();
            return Task.FromResult<IReadOnlyList<AttendanceRecord>>(found);
        }

        public Task<IReadOnlyList<EnrolledStudentSnapshotDto>> GetEnrolledStudentsAsync(
            Guid operationalPeriodId,
            Guid subjectId,
            Guid studyGroupId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EnrolledStudentSnapshotDto>>(EnrolledStudentsResult);

        public Task<Result> UpsertBatchAsync(
            IReadOnlyList<AttendanceRecord> records,
            CancellationToken cancellationToken = default)
        {
            if (UpsertResult.IsSuccess)
            {
                UpsertedRecords.AddRange(records);
            }
            return Task.FromResult(UpsertResult);
        }
    }

    private sealed class MockOperationalContract : IOperationalContract
    {
        public OperationalPeriodSummaryDto? PeriodResult { get; set; }

        public Task<ActiveAcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ActiveAcademicYearDto?>(null);

        public Task<ActiveOperationalPeriodDto?> GetActiveOperationalPeriodAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ActiveOperationalPeriodDto?>(null);

        public Task<AcademicYearSummaryDto?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<AcademicYearSummaryDto?>(null);

        public Task<OperationalPeriodSummaryDto?> GetOperationalPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(PeriodResult is not null && PeriodResult.Id == id ? PeriodResult : null);
    }

    private sealed class MockAcademicContract : IAcademicContract
    {
        public SubjectDto? SubjectResult { get; set; }

        public Task<IReadOnlyList<SubjectDto>> GetAllSubjectsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SubjectDto>>([]);

        public Task<SubjectDto?> GetSubjectByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(SubjectResult is not null && SubjectResult.Id == id ? SubjectResult : null);
    }

    private sealed class MockStudentContract : IStudentContract
    {
        public StudyGroupSummaryDto? StudyGroupResult { get; set; }

        public Task<StudyGroupSummaryDto?> GetStudyGroupByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(StudyGroupResult is not null && StudyGroupResult.Id == id ? StudyGroupResult : null);

        public Task<IReadOnlyList<StudentPlacementScopeDto>> GetActiveStudentsByScopeAsync(
            Guid? studyGroupId,
            string? schoolLevel,
            string? grade,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentPlacementScopeDto>>([]);
    }

    private sealed class MockCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = "teacher.budi";
        public string FullName { get; set; } = "Budi Santoso";
        public IReadOnlyList<string> Roles { get; set; } = ["TEACHER"];
        public IReadOnlyList<string> Permissions { get; set; } = [ElsasaSis.Modules.Acd.Permissions.Academic.Attendance.Record];
        public Guid? TeacherId { get; set; }
        public Guid? StudentId => null;
        public Guid? GuardianId => null;
    }

    private sealed class MockSqlConnectionFactory : ISqlConnectionFactory
    {
        public System.Data.IDbConnection CreateConnection() =>
            throw new NotImplementedException();

        public Task<System.Data.IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
