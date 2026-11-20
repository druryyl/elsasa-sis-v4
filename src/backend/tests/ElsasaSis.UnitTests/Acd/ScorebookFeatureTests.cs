using System.Reflection;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Acd;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.Modules.Acd.Features.Scorebook;
using ElsasaSis.Modules.Acd.Features.Scorebook.CreateClassroomAssignment;
using ElsasaSis.Modules.Acd.Features.Scorebook.EnterStudentScores;
using ElsasaSis.Modules.Acd.Features.Scorebook.GetSubjectGradebookSummary;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;
using Microsoft.Extensions.DependencyInjection;

namespace ElsasaSis.UnitTests.Acd;

public sealed class ScorebookFeatureTests
{
    private static readonly DateOnly ValidAssignedDate = new(2026, 11, 15);
    private static readonly DateOnly ValidDueDate = new(2026, 11, 20);
    private static readonly Guid SubjectStageId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();
    private static readonly Guid StudyGroupId = Guid.NewGuid();
    private static readonly Guid OperationalPeriodId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid Student1Id = Guid.NewGuid();
    private static readonly Guid Student2Id = Guid.NewGuid();

    [Fact]
    public async Task CreateAssignmentValidator_WhenValidRequest_PassesValidation()
    {
        var validator = new CreateAssignmentValidator();
        var request = new CreateAssignmentRequest(
            SubjectStageId,
            SubjectId,
            StudyGroupId,
            OperationalPeriodId,
            null,
            "Kuis Aljabar 1",
            "FORMATIVE",
            100.00m,
            20.00m,
            ValidAssignedDate,
            ValidDueDate);

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateAssignmentValidator_WhenMissingRequiredFields_FailsValidation()
    {
        var validator = new CreateAssignmentValidator();
        var request = new CreateAssignmentRequest(
            Guid.Empty,
            Guid.Empty,
            Guid.Empty,
            Guid.Empty,
            null,
            "",
            "",
            0.00m,
            -5.00m,
            ValidAssignedDate,
            new DateOnly(2026, 11, 10)); // earlier than assigned date

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SubjectStageId");
        Assert.Contains(result.Errors, e => e.PropertyName == "SubjectId");
        Assert.Contains(result.Errors, e => e.PropertyName == "StudyGroupId");
        Assert.Contains(result.Errors, e => e.PropertyName == "OperationalPeriodId");
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
        Assert.Contains(result.Errors, e => e.PropertyName == "AssignmentType");
        Assert.Contains(result.Errors, e => e.PropertyName == "MaxScore");
        Assert.Contains(result.Errors, e => e.PropertyName == "Weight");
        Assert.Contains(result.Errors, e => e.PropertyName == "DueDate");
    }

    [Fact]
    public async Task CreateAssignmentValidator_WhenWeightIsNegative_FailsValidation()
    {
        var validator = new CreateAssignmentValidator();
        var request = new CreateAssignmentRequest(
            SubjectStageId,
            SubjectId,
            StudyGroupId,
            OperationalPeriodId,
            null,
            "Tugas 1",
            "FORMATIVE",
            100.00m,
            -1.00m,
            ValidAssignedDate,
            ValidDueDate);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Weight");
    }

    [Fact]
    public async Task CreateAssignmentValidator_WhenWeightExceeds100_FailsValidation()
    {
        var validator = new CreateAssignmentValidator();
        var request = new CreateAssignmentRequest(
            SubjectStageId,
            SubjectId,
            StudyGroupId,
            OperationalPeriodId,
            null,
            "Tugas 1",
            "FORMATIVE",
            100.00m,
            100.01m,
            ValidAssignedDate,
            ValidDueDate);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Weight");
    }

    [Fact]
    public async Task CreateAssignmentValidator_WhenAssignmentTypeFormatInvalid_FailsValidation()
    {
        var validator = new CreateAssignmentValidator();
        var request = new CreateAssignmentRequest(
            SubjectStageId,
            SubjectId,
            StudyGroupId,
            OperationalPeriodId,
            null,
            "Tugas 1",
            "INVALID_TYPE",
            100.00m,
            10.00m,
            ValidAssignedDate,
            ValidDueDate);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "AssignmentType");
    }

    [Fact]
    public async Task CreateAssignmentValidator_WhenMaxScoreIsZeroOrNegative_FailsValidation()
    {
        var validator = new CreateAssignmentValidator();
        var requestZero = new CreateAssignmentRequest(
            SubjectStageId,
            SubjectId,
            StudyGroupId,
            OperationalPeriodId,
            null,
            "Tugas 1",
            "FORMATIVE",
            0.00m,
            10.00m,
            ValidAssignedDate,
            ValidDueDate);

        var requestNegative = new CreateAssignmentRequest(
            SubjectStageId,
            SubjectId,
            StudyGroupId,
            OperationalPeriodId,
            null,
            "Tugas 1",
            "FORMATIVE",
            -10.00m,
            10.00m,
            ValidAssignedDate,
            ValidDueDate);

        var resultZero = await validator.ValidateAsync(requestZero);
        var resultNegative = await validator.ValidateAsync(requestNegative);

        Assert.False(resultZero.IsValid);
        Assert.Contains(resultZero.Errors, e => e.PropertyName == "MaxScore");
        Assert.False(resultNegative.IsValid);
        Assert.Contains(resultNegative.Errors, e => e.PropertyName == "MaxScore");
    }

    [Fact]
    public async Task CreateAssignmentValidator_WhenDueDateEarlierThanAssignedDate_FailsValidation()
    {
        var validator = new CreateAssignmentValidator();
        var request = new CreateAssignmentRequest(
            SubjectStageId,
            SubjectId,
            StudyGroupId,
            OperationalPeriodId,
            null,
            "Tugas 1",
            "FORMATIVE",
            100.00m,
            10.00m,
            new DateOnly(2026, 11, 20),
            new DateOnly(2026, 11, 15));

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DueDate");
    }

    [Fact]
    public async Task CreateClassroomAssignmentHandler_WhenPeriodNotFound_Returns404()
    {
        var (handler, _, _, _, mockPeriod, _, _) = CreateHandlerContext();
        mockPeriod.PeriodResult = null;

        var request = CreateValidCreateRequest();
        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.OperationalPeriodNotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateClassroomAssignmentHandler_WhenSubjectNotFound_Returns404()
    {
        var (handler, _, mockSubjectRepo, _, _, _, _) = CreateHandlerContext();
        mockSubjectRepo.Subject = null;

        var request = CreateValidCreateRequest();
        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.SubjectNotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateClassroomAssignmentHandler_WhenSubjectStageNotFound_Returns404()
    {
        var (handler, _, _, mockStageRepo, _, _, _) = CreateHandlerContext();
        mockStageRepo.Stage = null;

        var request = CreateValidCreateRequest();
        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.SubjectStageNotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateClassroomAssignmentHandler_WhenStudyGroupNotFound_Returns404()
    {
        var (handler, _, _, _, _, mockStudentContract, _) = CreateHandlerContext();
        mockStudentContract.StudyGroupResult = null;

        var request = CreateValidCreateRequest();
        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.StudyGroupNotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateClassroomAssignmentHandler_WhenTeacherNotAssigned_Returns403()
    {
        var (handler, mockAssignmentRepo, _, _, _, _, mockUser) = CreateHandlerContext();
        mockUser.TeacherId = TeacherId;
        mockAssignmentRepo.HasTeacherAssignmentResult = false; // D-04 authorization check fails

        var request = CreateValidCreateRequest();
        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.TeacherNotAuthorized", result.Error.Code);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    [Fact]
    public async Task CreateClassroomAssignmentHandler_WhenTeacherAuthorized_CreatesAssignment()
    {
        var (handler, mockAssignmentRepo, _, _, _, _, mockUser) = CreateHandlerContext();
        mockUser.TeacherId = TeacherId;
        mockAssignmentRepo.HasTeacherAssignmentResult = true;

        var request = CreateValidCreateRequest();
        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal("Kuis Aljabar 1", dto.Title);
        Assert.Equal("FORMATIVE", dto.AssignmentType);
        Assert.Equal(100.00m, dto.MaxScore);
        Assert.Equal(20.00m, dto.Weight);
        Assert.Equal(TeacherId, dto.TeacherId);
        Assert.Single(mockAssignmentRepo.AddedAssignments);
    }

    [Fact]
    public async Task CreateClassroomAssignmentHandler_WhenAdminWithNoTeacherId_UsesActiveTeacherAssignment()
    {
        var (handler, mockAssignmentRepo, _, _, _, _, mockUser) = CreateHandlerContext();
        mockUser.TeacherId = null; // Admin user without TeacherId claim
        var assignedTeacherId = Guid.NewGuid();
        mockAssignmentRepo.ActiveTeacherResult = new TeacherAssignmentSnapshotDto(
            assignedTeacherId,
            "Siti Guru",
            SubjectId,
            "Matematika",
            StudyGroupId,
            "7A",
            OperationalPeriodId,
            "Semester 1");

        var request = CreateValidCreateRequest();
        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(assignedTeacherId, result.Value!.TeacherId);
        Assert.Equal("Siti Guru", result.Value!.TeacherName);
    }

    [Fact]
    public async Task EnterStudentScoresValidator_WhenValidRequest_PassesValidation()
    {
        var validator = new EnterStudentScoresValidator();
        var request = new EnterStudentScoresRequest([
            new ScoreEntryItemDto(Student1Id, 85.00m, "Good"),
            new ScoreEntryItemDto(Student2Id, 92.50m, null)
        ]);

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task EnterStudentScoresValidator_WhenEmptyItems_FailsValidation()
    {
        var validator = new EnterStudentScoresValidator();
        var request = new EnterStudentScoresRequest([]);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task EnterStudentScoresValidator_WhenScoreIsNegative_FailsValidation()
    {
        var validator = new EnterStudentScoresValidator();
        var request = new EnterStudentScoresRequest([
            new ScoreEntryItemDto(Student1Id, -5.00m, null)
        ]);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Score") && e.ErrorMessage.Contains("cannot be negative"));
    }

    [Fact]
    public async Task EnterStudentScoresValidator_WhenStudentIdIsEmpty_FailsValidation()
    {
        var validator = new EnterStudentScoresValidator();
        var request = new EnterStudentScoresRequest([
            new ScoreEntryItemDto(Guid.Empty, 85.00m, null)
        ]);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("StudentId") && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task EnterStudentScoresValidator_WhenFeedbackExceedsLimit_FailsValidation()
    {
        var validator = new EnterStudentScoresValidator();
        var request = new EnterStudentScoresRequest([
            new ScoreEntryItemDto(Student1Id, 85.00m, new string('A', 1001))
        ]);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Feedback"));
    }

    [Fact]
    public async Task EnterStudentScoresHandler_WhenDuplicateStudentInBatch_ReturnsValidationError()
    {
        var mockRepo = new MockAssignmentRepository();
        var mockUser = new MockCurrentUser { TeacherId = TeacherId };
        var handler = new EnterStudentScoresHandler(mockRepo, mockUser);

        var request = new EnterStudentScoresRequest([
            new ScoreEntryItemDto(Student1Id, 80.00m, null),
            new ScoreEntryItemDto(Student1Id, 90.00m, null) // duplicate
        ]);

        var result = await handler.HandleAsync(Guid.NewGuid(), request);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.DuplicateStudentInBatch", result.Error.Code);
    }

    [Fact]
    public async Task EnterStudentScoresHandler_WhenAssignmentNotFound_Returns404()
    {
        var mockRepo = new MockAssignmentRepository { AssignmentToReturn = null };
        var mockUser = new MockCurrentUser { TeacherId = TeacherId };
        var handler = new EnterStudentScoresHandler(mockRepo, mockUser);

        var request = new EnterStudentScoresRequest([
            new ScoreEntryItemDto(Student1Id, 80.00m, null)
        ]);

        var result = await handler.HandleAsync(Guid.NewGuid(), request);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.AssignmentNotFound", result.Error.Code);
    }

    [Fact]
    public async Task EnterStudentScoresHandler_WhenTeacherNotAuthorized_Returns403()
    {
        var assignment = CreateValidAssignment();
        var mockRepo = new MockAssignmentRepository
        {
            AssignmentToReturn = assignment,
            HasTeacherAssignmentResult = false // D-04 authorization check fails
        };
        var mockUser = new MockCurrentUser { TeacherId = TeacherId };
        var handler = new EnterStudentScoresHandler(mockRepo, mockUser);

        var request = new EnterStudentScoresRequest([
            new ScoreEntryItemDto(Student1Id, 80.00m, null)
        ]);

        var result = await handler.HandleAsync(assignment.Id, request);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.TeacherNotAuthorized", result.Error.Code);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    [Fact]
    public async Task EnterStudentScoresHandler_WhenStudentNotActivelyEnrolled_Returns409Conflict()
    {
        var assignment = CreateValidAssignment();
        var mockRepo = new MockAssignmentRepository
        {
            AssignmentToReturn = assignment,
            HasTeacherAssignmentResult = true,
            ActiveParticipations = [
                new ActiveParticipationSnapshotDto(Student1Id, "Ahmad Dahlan", "202607001")
                // Student2Id is NOT enrolled!
            ]
        };
        var mockUser = new MockCurrentUser { TeacherId = TeacherId };
        var handler = new EnterStudentScoresHandler(mockRepo, mockUser);

        var request = new EnterStudentScoresRequest([
            new ScoreEntryItemDto(Student1Id, 80.00m, null),
            new ScoreEntryItemDto(Student2Id, 90.00m, null)
        ]);

        var result = await handler.HandleAsync(assignment.Id, request);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.StudentNotActivelyEnrolled", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task EnterStudentScoresHandler_WhenValidBatch_UpsertsScoresAndSucceeds()
    {
        var assignment = CreateValidAssignment();
        var mockRepo = new MockAssignmentRepository
        {
            AssignmentToReturn = assignment,
            HasTeacherAssignmentResult = true,
            ActiveParticipations = [
                new ActiveParticipationSnapshotDto(Student1Id, "Ahmad Dahlan", "202607001"),
                new ActiveParticipationSnapshotDto(Student2Id, "Siti Nurhaliza", "202607002")
            ]
        };
        var mockUser = new MockCurrentUser { TeacherId = TeacherId };
        var handler = new EnterStudentScoresHandler(mockRepo, mockUser);

        var request = new EnterStudentScoresRequest([
            new ScoreEntryItemDto(Student1Id, 85.00m, "Good"),
            new ScoreEntryItemDto(Student2Id, 92.50m, "Excellent")
        ]);

        var result = await handler.HandleAsync(assignment.Id, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(assignment.Id, result.Value!.AssignmentId);
        Assert.Equal(2, result.Value!.ScoresUpdatedCount);
        Assert.Equal(2, assignment.Scores.Count);
        Assert.Single(mockRepo.UpsertedScoreBatches);
    }

    [Fact]
    public async Task GetSubjectGradebookSummaryHandler_WhenRequiredParameterMissing_ReturnsValidationError()
    {
        var mockConnectionFactory = new MockSqlConnectionFactory();
        var handler = new GetSubjectGradebookSummaryHandler(mockConnectionFactory);

        var query = new GetSubjectGradebookSummaryQuery(Guid.Empty, StudyGroupId, OperationalPeriodId);
        var result = await handler.HandleAsync(query);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.SubjectStageIdRequired", result.Error.Code);
    }

    [Fact]
    public void DependencyInjection_AddAcdModule_RegistersScorebookRepositoriesAndHandlers()
    {
        var services = new ServiceCollection();
        services.AddScoped<ISqlConnectionFactory, MockSqlConnectionFactory>();
        services.AddScoped<ICurrentUser, MockCurrentUser>();
        services.AddScoped<IOperationalContract, MockOperationalContract>();
        services.AddScoped<IStudentContract, MockStudentContract>();

        services.AddAcdModule();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IAssignmentRepository>());
        Assert.NotNull(provider.GetService<CreateClassroomAssignmentHandler>());
        Assert.NotNull(provider.GetService<EnterStudentScoresHandler>());
        Assert.NotNull(provider.GetService<GetSubjectGradebookSummaryHandler>());
    }

    [Fact]
    public void WriteHandlers_DoNotDependOnISqlConnectionFactory()
    {
        // Enforce CQRS Handler Discipline (Rule 4): Write handlers must not depend on ISqlConnectionFactory
        var writeHandlerTypes = new[]
        {
            typeof(CreateClassroomAssignmentHandler),
            typeof(EnterStudentScoresHandler)
        };

        foreach (var type in writeHandlerTypes)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            foreach (var ctor in constructors)
            {
                var parameterTypes = ctor.GetParameters().Select(p => p.ParameterType);
                Assert.DoesNotContain(typeof(ISqlConnectionFactory), parameterTypes);
            }
        }
    }

    private static (
        CreateClassroomAssignmentHandler Handler,
        MockAssignmentRepository AssignmentRepo,
        MockSubjectRepository SubjectRepo,
        MockSubjectStageRepository StageRepo,
        MockOperationalContract OperationalContract,
        MockStudentContract StudentContract,
        MockCurrentUser CurrentUser) CreateHandlerContext()
    {
        var assignmentRepo = new MockAssignmentRepository();
        var subjectRepo = new MockSubjectRepository
        {
            Subject = Subject.Create("Matematika", "MTK").Value
        };
        var stageRepo = new MockSubjectStageRepository
        {
            Stage = SubjectStage.Create(SubjectId, "Matematika", "Mathematics Stage 7", 1, null).Value
        };
        var operationalContract = new MockOperationalContract
        {
            PeriodResult = new OperationalPeriodSummaryDto(
                OperationalPeriodId,
                Guid.NewGuid(),
                "2026/2027",
                "Semester 1",
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 12, 31),
                "ACTIVE")
        };
        var studentContract = new MockStudentContract
        {
            StudyGroupResult = new StudyGroupSummaryDto(
                StudyGroupId,
                Guid.NewGuid(),
                "2026/2027",
                "SMP",
                "7",
                "7A",
                Guid.NewGuid(),
                "Budi Santoso")
        };
        var currentUser = new MockCurrentUser
        {
            TeacherId = TeacherId
        };

        var handler = new CreateClassroomAssignmentHandler(
            assignmentRepo,
            subjectRepo,
            stageRepo,
            operationalContract,
            studentContract,
            currentUser);

        return (handler, assignmentRepo, subjectRepo, stageRepo, operationalContract, studentContract, currentUser);
    }

    private static CreateAssignmentRequest CreateValidCreateRequest() =>
        new(
            SubjectStageId,
            SubjectId,
            StudyGroupId,
            OperationalPeriodId,
            null,
            "Kuis Aljabar 1",
            "FORMATIVE",
            100.00m,
            20.00m,
            ValidAssignedDate,
            ValidDueDate);

    private static Assignment CreateValidAssignment() =>
        Assignment.Create(
            SubjectStageId,
            "Mathematics Stage 7",
            SubjectId,
            "Matematika",
            OperationalPeriodId,
            "Semester 1",
            StudyGroupId,
            "7A",
            "Kuis Aljabar 1",
            AssignmentType.Formative,
            100.00m,
            20.00m,
            ValidAssignedDate,
            ValidDueDate,
            TeacherId,
            "Budi Santoso").Value!;

    private sealed class MockAssignmentRepository : IAssignmentRepository
    {
        public Assignment? AssignmentToReturn { get; set; }
        public bool HasTeacherAssignmentResult { get; set; } = true;
        public TeacherAssignmentSnapshotDto? ActiveTeacherResult { get; set; }
        public IReadOnlyList<ActiveParticipationSnapshotDto> ActiveParticipations { get; set; } = [];
        public List<Assignment> AddedAssignments { get; } = [];
        public List<Assignment> UpdatedAssignments { get; } = [];
        public List<IReadOnlyList<AssignmentScore>> UpsertedScoreBatches { get; } = [];

        public Task<Assignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(AssignmentToReturn);

        public Task AddAsync(Assignment assignment, CancellationToken cancellationToken = default)
        {
            AddedAssignments.Add(assignment);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken = default)
        {
            UpdatedAssignments.Add(assignment);
            return Task.CompletedTask;
        }

        public Task UpsertScoresAsync(Guid assignmentId, IReadOnlyList<AssignmentScore> scores, CancellationToken cancellationToken = default)
        {
            UpsertedScoreBatches.Add(scores);
            return Task.CompletedTask;
        }

        public Task<bool> HasTeacherAssignmentAsync(
            Guid teacherId,
            Guid studyGroupId,
            Guid subjectId,
            Guid operationalPeriodId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(HasTeacherAssignmentResult);

        public Task<TeacherAssignmentSnapshotDto?> GetActiveTeacherAssignmentAsync(
            Guid studyGroupId,
            Guid subjectId,
            Guid operationalPeriodId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ActiveTeacherResult);

        public Task<IReadOnlyList<ActiveParticipationSnapshotDto>> GetActiveParticipationsAsync(
            Guid subjectStageId,
            Guid studyGroupId,
            Guid operationalPeriodId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ActiveParticipations);
    }

    private sealed class MockSubjectRepository : ISubjectRepository
    {
        public Subject? Subject { get; set; }
        public Task<Subject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Subject);
        public Task<Subject?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(Subject);
        public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<Result> AddAsync(Subject subject, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
        public Task<Result> UpdateAsync(Subject subject, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class MockSubjectStageRepository : ISubjectStageRepository
    {
        public SubjectStage? Stage { get; set; }
        public Task<SubjectStage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stage);
        public Task<IReadOnlyList<SubjectStage>> GetBySubjectIdAsync(Guid subjectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SubjectStage>>([]);
        public Task<bool> ExistsByNameAsync(Guid subjectId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> ExistsBySequenceOrderAsync(Guid subjectId, int sequenceOrder, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<Result> AddAsync(SubjectStage stage, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
        public Task<Result> UpdateAsync(SubjectStage stage, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
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
            Task.FromResult(PeriodResult);
    }

    private sealed class MockStudentContract : IStudentContract
    {
        public StudyGroupSummaryDto? StudyGroupResult { get; set; }
        public Task<StudyGroupSummaryDto?> GetStudyGroupByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(StudyGroupResult);
    }

    private sealed class MockCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = "teacher.budi";
        public string FullName { get; set; } = "Budi Santoso";
        public IReadOnlyList<string> Roles { get; set; } = ["TEACHER"];
        public IReadOnlyList<string> Permissions { get; set; } = [ElsasaSis.Modules.Acd.Permissions.Academic.Scorebook.Manage];
        public Guid? TeacherId { get; set; }
        public Guid? StudentId => null;
        public Guid? GuardianId => null;
    }

    private sealed class MockSqlConnectionFactory : ISqlConnectionFactory
    {
        public System.Data.IDbConnection CreateConnection() => throw new NotImplementedException();
        public Task<System.Data.IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
