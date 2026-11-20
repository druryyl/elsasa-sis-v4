using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Contracts.Teaching;
using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.Modules.Dev.Features.Extracurricular;
using ElsasaSis.Modules.Dev.Features.Extracurricular.CompleteExtracurricularParticipant;
using ElsasaSis.Modules.Dev.Features.Extracurricular.CreateExtracurricularActivity;
using ElsasaSis.Modules.Dev.Features.Extracurricular.EnrollExtracurricularParticipant;
using ElsasaSis.Modules.Dev.Features.Extracurricular.GetExtracurricularActivities;
using ElsasaSis.Modules.Dev.Features.Extracurricular.GetExtracurricularParticipants;
using ElsasaSis.Modules.Dev.Features.Extracurricular.RecordExtracurricularSessionLog;
using ElsasaSis.Modules.Dev.Features.Extracurricular.UpdateExtracurricularActivity;
using ElsasaSis.Modules.Dev.Features.Extracurricular.WithdrawExtracurricularParticipant;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.UnitTests.Development;

public sealed class ExtracurricularFeatureTests
{
    private static readonly Guid SupervisorTeacherId = Guid.NewGuid();
    private const string SupervisorTeacherName = "Budi Santoso";
    private static readonly Guid StudentId = Guid.NewGuid();
    private const string StudentName = "Ahmad Dahlan";
    private const string StudentNis = "2026001";
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private const string AcademicYearName = "2026/2027";

    // -------------------------------------------------------------
    // 1. Validator Tests
    // -------------------------------------------------------------

    [Fact]
    public async Task CreateActivityValidator_WhenValid_Passes()
    {
        var validator = new CreateExtracurricularActivityValidator();
        var request = new CreateExtracurricularActivityRequest("Pramuka", "Gerakan Pramuka Gudep 01", SupervisorTeacherId);

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateActivityValidator_WhenNameEmptyOrSupervisorEmpty_Fails()
    {
        var validator = new CreateExtracurricularActivityValidator();
        var request = new CreateExtracurricularActivityRequest(string.Empty, null, Guid.Empty);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task CreateActivityValidator_WhenFieldsExceedMax_Fails()
    {
        var validator = new CreateExtracurricularActivityValidator();
        var request = new CreateExtracurricularActivityRequest(new string('a', 101), new string('b', 501), SupervisorTeacherId);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task UpdateActivityValidator_WhenValid_Passes()
    {
        var validator = new UpdateExtracurricularActivityValidator();
        var request = new UpdateExtracurricularActivityRequest("Pramuka Updated", "Desc", SupervisorTeacherId, "INACTIVE");

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdateActivityValidator_WhenInvalidStatus_Fails()
    {
        var validator = new UpdateExtracurricularActivityValidator();
        var request = new UpdateExtracurricularActivityRequest("Pramuka", "Desc", SupervisorTeacherId, "UNKNOWN_STATUS");

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Status");
    }

    [Fact]
    public async Task EnrollParticipantValidator_WhenValid_Passes()
    {
        var validator = new EnrollExtracurricularParticipantValidator();
        var request = new EnrollExtracurricularParticipantRequest(StudentId, StudentName, StudentNis, AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 20));

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task EnrollParticipantValidator_WhenStudentEmpty_Fails()
    {
        var validator = new EnrollExtracurricularParticipantValidator();
        var request = new EnrollExtracurricularParticipantRequest(Guid.Empty, null, null, null, null, default);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "StudentId");
        Assert.Contains(result.Errors, e => e.PropertyName == "JoinedDate");
    }

    [Fact]
    public async Task RecordSessionLogValidator_WhenValid_Passes()
    {
        var validator = new RecordExtracurricularSessionLogValidator();
        var request = new RecordExtracurricularSessionLogRequest(new DateOnly(2026, 8, 10), "Latihan baris berbaris dan simpul.");

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task RecordSessionLogValidator_WhenLogEmptyOrExceedsMax_Fails()
    {
        var validator = new RecordExtracurricularSessionLogValidator();
        var emptyRequest = new RecordExtracurricularSessionLogRequest(new DateOnly(2026, 8, 10), string.Empty);
        var longRequest = new RecordExtracurricularSessionLogRequest(new DateOnly(2026, 8, 10), new string('x', 1001));

        var emptyResult = await validator.ValidateAsync(emptyRequest);
        var longResult = await validator.ValidateAsync(longRequest);

        Assert.False(emptyResult.IsValid);
        Assert.False(longResult.IsValid);
    }

    // -------------------------------------------------------------
    // 2. CreateExtracurricularActivityHandler Tests
    // -------------------------------------------------------------

    [Fact]
    public async Task CreateActivity_WhenValid_SavesAndReturnsDto()
    {
        var mockRepo = new MockExtracurricularRepository();
        var mockTeaching = new MockTeachingContract
        {
            Teachers = [new TeacherSummaryDto(SupervisorTeacherId, SupervisorTeacherName, "ACTIVE")]
        };

        var handler = new CreateExtracurricularActivityHandler(mockRepo, mockTeaching);
        var request = new CreateExtracurricularActivityRequest("Pramuka", "Kegiatan kepanduan", SupervisorTeacherId);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Pramuka", result.Value.Name);
        Assert.Equal("ACTIVE", result.Value.Status);
        Assert.Equal(SupervisorTeacherId, result.Value.SupervisorId);
        Assert.Equal(SupervisorTeacherName, result.Value.SupervisorName);
        Assert.NotNull(mockRepo.InsertedActivity);
    }

    [Fact]
    public async Task CreateActivity_WhenDuplicateName_ReturnsConflict()
    {
        var mockRepo = new MockExtracurricularRepository
        {
            ExistingNames = ["Pramuka"]
        };
        var mockTeaching = new MockTeachingContract
        {
            Teachers = [new TeacherSummaryDto(SupervisorTeacherId, SupervisorTeacherName, "ACTIVE")]
        };

        var handler = new CreateExtracurricularActivityHandler(mockRepo, mockTeaching);
        var request = new CreateExtracurricularActivityRequest("Pramuka", "Kegiatan kepanduan", SupervisorTeacherId);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.Extracurricular.DuplicateName", result.Error.Code);
    }

    [Fact]
    public async Task CreateActivity_WhenSupervisorNotFound_ReturnsNotFound()
    {
        var mockRepo = new MockExtracurricularRepository();
        var mockTeaching = new MockTeachingContract(); // No teachers

        var handler = new CreateExtracurricularActivityHandler(mockRepo, mockTeaching);
        var request = new CreateExtracurricularActivityRequest("Pramuka", "Desc", SupervisorTeacherId);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.Extracurricular.SupervisorNotFound", result.Error.Code);
    }

    // -------------------------------------------------------------
    // 3. GetExtracurricularActivitiesHandler Tests
    // -------------------------------------------------------------

    [Fact]
    public async Task GetActivities_ReturnsCatalogWithCounts()
    {
        var act1 = ExtracurricularActivity.Create("Pramuka", "Desc", SupervisorTeacherId, SupervisorTeacherName).Value!;
        act1.EnrollStudent(StudentId, StudentName, StudentNis, AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 20));

        var act2 = ExtracurricularActivity.Create("Futsal", "Desc", SupervisorTeacherId, SupervisorTeacherName).Value!;

        var mockRepo = new MockExtracurricularRepository
        {
            AllActivities = [act1, act2]
        };

        var handler = new GetExtracurricularActivitiesHandler(mockRepo);
        var result = await handler.HandleAsync(new GetExtracurricularActivitiesQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal(1, result.Value[0].ActiveParticipantCount);
        Assert.Equal(0, result.Value[1].ActiveParticipantCount);
    }

    // -------------------------------------------------------------
    // 4. UpdateExtracurricularActivityHandler Tests
    // -------------------------------------------------------------

    [Fact]
    public async Task UpdateActivity_WhenValid_UpdatesFields()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Old Desc", SupervisorTeacherId, SupervisorTeacherName).Value!;
        var newSupervisorId = Guid.NewGuid();
        var mockRepo = new MockExtracurricularRepository
        {
            ActivityById = activity
        };
        var mockTeaching = new MockTeachingContract
        {
            Teachers = [new TeacherSummaryDto(newSupervisorId, "Guru Baru", "ACTIVE")]
        };

        var handler = new UpdateExtracurricularActivityHandler(mockRepo, mockTeaching);
        var request = new UpdateExtracurricularActivityRequest("Pramuka Hebat", "New Desc", newSupervisorId, "INACTIVE");

        var result = await handler.HandleAsync(activity.Id, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("Pramuka Hebat", activity.Name);
        Assert.Equal("New Desc", activity.Description);
        Assert.Equal("INACTIVE", activity.Status);
        Assert.Equal(newSupervisorId, activity.SupervisorId);
        Assert.Equal("Guru Baru", activity.SupervisorName);
        Assert.NotNull(mockRepo.UpdatedActivity);
    }

    [Fact]
    public async Task UpdateActivity_WhenNotFound_ReturnsNotFound()
    {
        var mockRepo = new MockExtracurricularRepository();
        var mockTeaching = new MockTeachingContract();

        var handler = new UpdateExtracurricularActivityHandler(mockRepo, mockTeaching);
        var result = await handler.HandleAsync(Guid.NewGuid(), new UpdateExtracurricularActivityRequest("Pramuka", null, SupervisorTeacherId, "ACTIVE"));

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.Extracurricular.NotFound", result.Error.Code);
    }

    // -------------------------------------------------------------
    // 5. EnrollExtracurricularParticipantHandler Tests
    // -------------------------------------------------------------

    [Fact]
    public async Task EnrollParticipant_WhenValidWithExplicitDetails_Succeeds()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Desc", SupervisorTeacherId, SupervisorTeacherName).Value!;
        var mockRepo = new MockExtracurricularRepository { ActivityById = activity };
        var mockStudents = new MockStudentContract();
        var mockOperational = new MockOperationalContract();

        var handler = new EnrollExtracurricularParticipantHandler(mockRepo, mockStudents, mockOperational);
        var request = new EnrollExtracurricularParticipantRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 7, 20));

        var result = await handler.HandleAsync(activity.Id, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(StudentId, result.Value.StudentId);
        Assert.Equal(StudentName, result.Value.StudentName);
        Assert.Equal("ACTIVE", result.Value.Status);
        Assert.NotNull(mockRepo.InsertedParticipation);
    }

    [Fact]
    public async Task EnrollParticipant_WhenDetailsOmitted_ResolvesFromContracts()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Desc", SupervisorTeacherId, SupervisorTeacherName).Value!;
        var mockRepo = new MockExtracurricularRepository { ActivityById = activity };
        var mockStudents = new MockStudentContract
        {
            ActiveStudents =
            [
                new StudentPlacementScopeDto(StudentId, StudentName, StudentNis, Guid.NewGuid(), "7A", "SMP", "7")
            ]
        };
        var mockOperational = new MockOperationalContract
        {
            ActiveYear = new ActiveAcademicYearDto(AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30))
        };

        var handler = new EnrollExtracurricularParticipantHandler(mockRepo, mockStudents, mockOperational);
        var request = new EnrollExtracurricularParticipantRequest(
            StudentId,
            null,
            null,
            null,
            null,
            new DateOnly(2026, 7, 20));

        var result = await handler.HandleAsync(activity.Id, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(StudentName, result.Value!.StudentName);
        Assert.Equal(StudentNis, result.Value.StudentNis);
        Assert.Equal(AcademicYearName, result.Value.AcademicYearName);
    }

    [Fact]
    public async Task EnrollParticipant_WhenActivityNotActive_ReturnsConflict()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Desc", SupervisorTeacherId, SupervisorTeacherName).Value!;
        activity.UpdateDetails("Pramuka", "Desc", SupervisorTeacherId, SupervisorTeacherName, ExtracurricularActivityStatus.Inactive);

        var mockRepo = new MockExtracurricularRepository { ActivityById = activity };
        var mockStudents = new MockStudentContract();
        var mockOperational = new MockOperationalContract();

        var handler = new EnrollExtracurricularParticipantHandler(mockRepo, mockStudents, mockOperational);
        var request = new EnrollExtracurricularParticipantRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 7, 20));

        var result = await handler.HandleAsync(activity.Id, request);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.Extracurricular.NotActive", result.Error.Code);
    }

    [Fact]
    public async Task EnrollParticipant_WhenAlreadyEnrolled_ReturnsConflict()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Desc", SupervisorTeacherId, SupervisorTeacherName).Value!;
        activity.EnrollStudent(StudentId, StudentName, StudentNis, AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 20));

        var mockRepo = new MockExtracurricularRepository { ActivityById = activity };
        var mockStudents = new MockStudentContract();
        var mockOperational = new MockOperationalContract();

        var handler = new EnrollExtracurricularParticipantHandler(mockRepo, mockStudents, mockOperational);
        var request = new EnrollExtracurricularParticipantRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 7, 25));

        var result = await handler.HandleAsync(activity.Id, request);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.Extracurricular.AlreadyEnrolled", result.Error.Code);
    }

    // -------------------------------------------------------------
    // 6. GetExtracurricularParticipantsHandler Tests
    // -------------------------------------------------------------

    [Fact]
    public async Task GetParticipants_WhenActivityExists_ReturnsList()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Desc", SupervisorTeacherId, SupervisorTeacherName).Value!;
        var part = activity.EnrollStudent(StudentId, StudentName, StudentNis, AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 20)).Value!;

        var mockRepo = new MockExtracurricularRepository
        {
            ActivityById = activity,
            ParticipationsByActivity = [part]
        };

        var handler = new GetExtracurricularParticipantsHandler(mockRepo);
        var result = await handler.HandleAsync(activity.Id, new GetExtracurricularParticipantsQuery());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(StudentId, result.Value![0].StudentId);
    }

    // -------------------------------------------------------------
    // 7. WithdrawExtracurricularParticipantHandler Tests
    // -------------------------------------------------------------

    [Fact]
    public async Task WithdrawParticipant_WhenActive_TransitionsToWithdrawn()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Desc", SupervisorTeacherId, SupervisorTeacherName).Value!;
        var part = activity.EnrollStudent(StudentId, StudentName, StudentNis, AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 20)).Value!;

        var mockRepo = new MockExtracurricularRepository { ActivityById = activity };
        var handler = new WithdrawExtracurricularParticipantHandler(mockRepo);

        var result = await handler.HandleAsync(activity.Id, part.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("WITHDRAWN", result.Value!.Status);
        Assert.NotNull(result.Value.WithdrawnAt);
        Assert.NotNull(mockRepo.UpdatedParticipation);
    }

    [Fact]
    public async Task WithdrawParticipant_WhenAlreadyWithdrawn_ReturnsConflict()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Desc", SupervisorTeacherId, SupervisorTeacherName).Value!;
        var part = activity.EnrollStudent(StudentId, StudentName, StudentNis, AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 20)).Value!;
        activity.WithdrawStudent(part.Id);

        var mockRepo = new MockExtracurricularRepository { ActivityById = activity };
        var handler = new WithdrawExtracurricularParticipantHandler(mockRepo);

        var result = await handler.HandleAsync(activity.Id, part.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.Extracurricular.AlreadyWithdrawn", result.Error.Code);
    }

    // -------------------------------------------------------------
    // 8. CompleteExtracurricularParticipantHandler Tests
    // -------------------------------------------------------------

    [Fact]
    public async Task CompleteParticipant_WhenActive_TransitionsToCompleted()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Desc", SupervisorTeacherId, SupervisorTeacherName).Value!;
        var part = activity.EnrollStudent(StudentId, StudentName, StudentNis, AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 20)).Value!;

        var mockRepo = new MockExtracurricularRepository { ActivityById = activity };
        var handler = new CompleteExtracurricularParticipantHandler(mockRepo);

        var result = await handler.HandleAsync(activity.Id, part.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("COMPLETED", result.Value!.Status);
        Assert.NotNull(result.Value.CompletedAt);
    }

    // -------------------------------------------------------------
    // 9. RecordExtracurricularSessionLogHandler Tests
    // -------------------------------------------------------------

    [Fact]
    public async Task RecordSessionLog_WhenAssignedSupervisor_UpdatesLogAndDate()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Desc", SupervisorTeacherId, SupervisorTeacherName).Value!;
        var part = activity.EnrollStudent(StudentId, StudentName, StudentNis, AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 20)).Value!;

        var mockRepo = new MockExtracurricularRepository { ActivityById = activity };
        var mockUser = new MockCurrentUser
        {
            TeacherId = SupervisorTeacherId,
            Roles = ["TEACHER"]
        };

        var handler = new RecordExtracurricularSessionLogHandler(mockRepo, mockUser);
        var request = new RecordExtracurricularSessionLogRequest(new DateOnly(2026, 8, 15), "Latihan navigasi dan mendirikan tenda.");

        var result = await handler.HandleAsync(activity.Id, part.Id, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("Latihan navigasi dan mendirikan tenda.", result.Value!.LastSessionLog);
        Assert.Equal(new DateOnly(2026, 8, 15), result.Value.LastSessionDate);
        Assert.NotNull(mockRepo.UpdatedParticipation);
    }

    [Fact]
    public async Task RecordSessionLog_WhenCallerNotAssignedSupervisor_ReturnsForbidden()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Desc", SupervisorTeacherId, SupervisorTeacherName).Value!;
        var part = activity.EnrollStudent(StudentId, StudentName, StudentNis, AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 20)).Value!;

        var mockRepo = new MockExtracurricularRepository { ActivityById = activity };
        var otherTeacherId = Guid.NewGuid();
        var mockUser = new MockCurrentUser
        {
            TeacherId = otherTeacherId,
            Roles = ["TEACHER"]
        };

        var handler = new RecordExtracurricularSessionLogHandler(mockRepo, mockUser);
        var request = new RecordExtracurricularSessionLogRequest(new DateOnly(2026, 8, 15), "Latihan futsal.");

        var result = await handler.HandleAsync(activity.Id, part.Id, request);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.Extracurricular.NotAssignedSupervisor", result.Error.Code);
    }

    [Fact]
    public async Task RecordSessionLog_WhenParticipationNotActive_ReturnsConflict()
    {
        var activity = ExtracurricularActivity.Create("Pramuka", "Desc", SupervisorTeacherId, SupervisorTeacherName).Value!;
        var part = activity.EnrollStudent(StudentId, StudentName, StudentNis, AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 20)).Value!;
        activity.WithdrawStudent(part.Id);

        var mockRepo = new MockExtracurricularRepository { ActivityById = activity };
        var mockUser = new MockCurrentUser
        {
            TeacherId = SupervisorTeacherId,
            Roles = ["TEACHER"]
        };

        var handler = new RecordExtracurricularSessionLogHandler(mockRepo, mockUser);
        var request = new RecordExtracurricularSessionLogRequest(new DateOnly(2026, 8, 15), "Latihan.");

        var result = await handler.HandleAsync(activity.Id, part.Id, request);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.Extracurricular.ParticipationNotActive", result.Error.Code);
    }

    // -------------------------------------------------------------
    // Test Mocks
    // -------------------------------------------------------------

    private sealed class MockExtracurricularRepository : IExtracurricularActivityRepository
    {
        public ExtracurricularActivity? ActivityById { get; set; }
        public List<ExtracurricularActivity> AllActivities { get; set; } = [];
        public List<string> ExistingNames { get; set; } = [];
        public List<ExtracurricularParticipation> ParticipationsByActivity { get; set; } = [];

        public ExtracurricularActivity? InsertedActivity { get; private set; }
        public ExtracurricularActivity? UpdatedActivity { get; private set; }
        public ExtracurricularParticipation? InsertedParticipation { get; private set; }
        public ExtracurricularParticipation? UpdatedParticipation { get; private set; }

        public Task<ExtracurricularActivity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(ActivityById?.Id == id ? ActivityById : ActivityById);

        public Task<ExtracurricularActivity?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(AllActivities.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<ExtracurricularActivity>> GetAllAsync(string? status = null, Guid? supervisorId = null, CancellationToken cancellationToken = default)
        {
            var query = AllActivities.AsEnumerable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => string.Equals(a.Status, status, StringComparison.OrdinalIgnoreCase));
            if (supervisorId.HasValue)
                query = query.Where(a => a.SupervisorId == supervisorId.Value);

            return Task.FromResult<IReadOnlyList<ExtracurricularActivity>>(query.ToList().AsReadOnly());
        }

        public Task<IReadOnlyList<ExtracurricularParticipation>> GetParticipationsByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExtracurricularParticipation>>([]);

        public Task<IReadOnlyList<ExtracurricularParticipation>> GetParticipationsByActivityIdAsync(Guid activityId, Guid? academicYearId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExtracurricularParticipation>>(ParticipationsByActivity.AsReadOnly());

        public Task<ExtracurricularParticipation?> GetParticipationByIdAsync(Guid participationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ParticipationsByActivity.FirstOrDefault(p => p.Id == participationId));

        public Task InsertAsync(ExtracurricularActivity activity, CancellationToken cancellationToken = default)
        {
            InsertedActivity = activity;
            AllActivities.Add(activity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ExtracurricularActivity activity, CancellationToken cancellationToken = default)
        {
            UpdatedActivity = activity;
            return Task.CompletedTask;
        }

        public Task InsertParticipationAsync(ExtracurricularParticipation participation, CancellationToken cancellationToken = default)
        {
            InsertedParticipation = participation;
            ParticipationsByActivity.Add(participation);
            return Task.CompletedTask;
        }

        public Task UpdateParticipationAsync(ExtracurricularParticipation participation, CancellationToken cancellationToken = default)
        {
            UpdatedParticipation = participation;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExistingNames.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)));
    }

    private sealed class MockTeachingContract : ITeachingContract
    {
        public List<TeacherSummaryDto> Teachers { get; set; } = [];

        public Task<TeacherSummaryDto?> GetTeacherByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Teachers.FirstOrDefault(t => t.Id == id));
    }

    private sealed class MockStudentContract : IStudentContract
    {
        public List<StudentPlacementScopeDto> ActiveStudents { get; set; } = [];

        public Task<StudyGroupSummaryDto?> GetStudyGroupByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<StudyGroupSummaryDto?>(null);

        public Task<IReadOnlyList<StudentPlacementScopeDto>> GetActiveStudentsByScopeAsync(Guid? studyGroupId, string? schoolLevel, string? grade, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentPlacementScopeDto>>(ActiveStudents.AsReadOnly());
    }

    private sealed class MockOperationalContract : IOperationalContract
    {
        public ActiveAcademicYearDto? ActiveYear { get; set; }
        public List<AcademicYearSummaryDto> Years { get; set; } = [];

        public Task<ActiveAcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ActiveYear);

        public Task<ActiveOperationalPeriodDto?> GetActiveOperationalPeriodAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ActiveOperationalPeriodDto?>(null);

        public Task<AcademicYearSummaryDto?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Years.FirstOrDefault(y => y.Id == id));

        public Task<OperationalPeriodSummaryDto?> GetOperationalPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<OperationalPeriodSummaryDto?>(null);
    }

    private sealed class MockCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = "test.user";
        public string FullName { get; set; } = "Test User";
        public IReadOnlyList<string> Roles { get; set; } = [];
        public IReadOnlyList<string> Permissions { get; set; } = [];
        public Guid? TeacherId { get; set; }
        public Guid? StudentId => null;
        public Guid? GuardianId => null;
    }
}
