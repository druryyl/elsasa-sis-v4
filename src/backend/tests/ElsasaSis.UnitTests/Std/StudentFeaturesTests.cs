using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student.Events;
using ElsasaSis.Contracts.Teaching;
using ElsasaSis.Modules.Std;
using ElsasaSis.Modules.Std.Domain;
using ElsasaSis.Modules.Std.Features.Placements.PlaceStudent;
using ElsasaSis.Modules.Std.Features.Students.AddStudentGuardian;
using ElsasaSis.Modules.Std.Features.Students.RemoveStudentGuardian;
using ElsasaSis.Modules.Std.Features.Students.UpdateStudent;
using ElsasaSis.Modules.Std.Features.Students.UpdateStudentGuardian;
using ElsasaSis.Modules.Std.Features.StudyGroups.CreateStudyGroup;
using ElsasaSis.Modules.Std.Features.StudyGroups.UpdateStudyGroup;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;
using ElsasaSis.SharedKernel.Events;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.UnitTests.Std;

public sealed class StudentFeaturesTests
{
    [Fact]
    public async Task UpdateStudentHandler_WhenStudentNotFound_Returns404()
    {
        var repo = new MockStudentRepository();
        var dispatcher = new MockDomainEventDispatcher();
        var handler = new UpdateStudentHandler(repo, dispatcher);

        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            new UpdateStudentRequest("0098765432", "Updated Name", null, new DateOnly(2010, 1, 1), "MALE", null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task UpdateStudentHandler_WhenDuplicateNisn_Returns409()
    {
        var student = CreateSampleStudent();
        var repo = new MockStudentRepository { Student = student, NisnExists = true };
        var dispatcher = new MockDomainEventDispatcher();
        var handler = new UpdateStudentHandler(repo, dispatcher);

        var result = await handler.HandleAsync(
            student.Id,
            new UpdateStudentRequest("0098765432", "Updated Name", null, new DateOnly(2010, 1, 1), "MALE", null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task UpdateStudentHandler_WhenValid_UpdatesProfileAndReturnsSuccess()
    {
        var student = CreateSampleStudent();
        var repo = new MockStudentRepository { Student = student, NisnExists = false };
        var dispatcher = new MockDomainEventDispatcher();
        var handler = new UpdateStudentHandler(repo, dispatcher);

        var result = await handler.HandleAsync(
            student.Id,
            new UpdateStudentRequest("0098765432", "Ahmad Fauzi Updated", "Ahmad", new DateOnly(2010, 5, 20), "MALE", "08123456789", "ahmad@test.local", "Jl. Merdeka No. 1"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ahmad Fauzi Updated", result.Value!.FullName);
        Assert.Equal("0098765432", result.Value.Nisn);
        Assert.Equal(student.Id, repo.UpdatedStudent?.Id);
    }

    [Fact]
    public async Task AddStudentGuardianHandler_WhenStudentNotFound_Returns404()
    {
        var repo = new MockStudentRepository();
        var dispatcher = new MockDomainEventDispatcher();
        var handler = new AddStudentGuardianHandler(repo, dispatcher);

        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            new AddStudentGuardianRequest("Bambang", "FATHER", "081111111", null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task AddStudentGuardianHandler_WhenValid_AddsGuardianAndReturnsCreated()
    {
        var student = CreateSampleStudent();
        var repo = new MockStudentRepository { Student = student };
        var dispatcher = new MockDomainEventDispatcher();
        var handler = new AddStudentGuardianHandler(repo, dispatcher);

        var result = await handler.HandleAsync(
            student.Id,
            new AddStudentGuardianRequest("Siti Aminah", "MOTHER", "082222222", "siti@test.local", "Jakarta"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Siti Aminah", result.Value!.FullName);
        Assert.Equal("MOTHER", result.Value.RelationshipType);
        Assert.Equal(2, student.Guardians.Count);
    }

    [Fact]
    public async Task UpdateStudentGuardianHandler_WhenGuardianNotFound_Returns404()
    {
        var student = CreateSampleStudent();
        var repo = new MockStudentRepository { Student = student };
        var dispatcher = new MockDomainEventDispatcher();
        var handler = new UpdateStudentGuardianHandler(repo, dispatcher);

        var result = await handler.HandleAsync(
            student.Id,
            Guid.NewGuid(),
            new UpdateStudentGuardianRequest("Updated Name", "FATHER", null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task RemoveStudentGuardianHandler_WhenLastGuardian_EnforcesBR_STU_003_Returns409Conflict()
    {
        var student = CreateSampleStudent(); // Exactly 1 guardian
        var guardianId = student.Guardians[0].Id;
        var repo = new MockStudentRepository { Student = student };
        var dispatcher = new MockDomainEventDispatcher();
        var handler = new RemoveStudentGuardianHandler(repo, dispatcher);

        var result = await handler.HandleAsync(student.Id, guardianId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Std.Student.CannotRemoveLastGuardian", result.Error.Code);
    }

    [Fact]
    public async Task RemoveStudentGuardianHandler_WhenMoreThanOneGuardian_RemovesSuccessfully()
    {
        var student = CreateSampleStudent();
        student.AddGuardian("Second Guardian", GuardianRelationshipType.Mother);
        Assert.Equal(2, student.Guardians.Count);

        var guardianToRemove = student.Guardians[1].Id;
        var repo = new MockStudentRepository { Student = student };
        var dispatcher = new MockDomainEventDispatcher();
        var handler = new RemoveStudentGuardianHandler(repo, dispatcher);

        var result = await handler.HandleAsync(student.Id, guardianToRemove);

        Assert.True(result.IsSuccess);
        Assert.Single(student.Guardians);
    }

    [Fact]
    public async Task CreateStudyGroupHandler_WhenAcademicYearNotFound_Returns404()
    {
        var groupRepo = new MockStudyGroupRepository();
        var opr = new MockOperationalContract();
        var tch = new MockTeachingContract();
        var dispatcher = new MockDomainEventDispatcher();
        var handler = new CreateStudyGroupHandler(groupRepo, opr, tch, dispatcher);

        var result = await handler.HandleAsync(new CreateStudyGroupRequest(
            Guid.NewGuid(),
            "SMP",
            "7",
            "7A",
            null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task CreateStudyGroupHandler_WhenTeacherInactive_Returns409Conflict()
    {
        var academicYearId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var groupRepo = new MockStudyGroupRepository();
        var opr = new MockOperationalContract
        {
            AcademicYear = new AcademicYearSummaryDto(academicYearId, "2026/2027", new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "ACTIVE")
        };
        var tch = new MockTeachingContract
        {
            Teacher = new TeacherSummaryDto(teacherId, "Pak Joko", "INACTIVE")
        };
        var dispatcher = new MockDomainEventDispatcher();
        var handler = new CreateStudyGroupHandler(groupRepo, opr, tch, dispatcher);

        var result = await handler.HandleAsync(new CreateStudyGroupRequest(
            academicYearId,
            "SMP",
            "7",
            "7A",
            teacherId));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Std.StudyGroup.HomeroomTeacherInactive", result.Error.Code);
    }

    [Fact]
    public async Task CreateStudyGroupHandler_WhenDuplicateName_Returns409Conflict()
    {
        var academicYearId = Guid.NewGuid();
        var groupRepo = new MockStudyGroupRepository { NameExists = true };
        var opr = new MockOperationalContract
        {
            AcademicYear = new AcademicYearSummaryDto(academicYearId, "2026/2027", new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "ACTIVE")
        };
        var tch = new MockTeachingContract();
        var dispatcher = new MockDomainEventDispatcher();
        var handler = new CreateStudyGroupHandler(groupRepo, opr, tch, dispatcher);

        var result = await handler.HandleAsync(new CreateStudyGroupRequest(
            academicYearId,
            "SMP",
            "7",
            "7A",
            null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Std.StudyGroup.DuplicateName", result.Error.Code);
    }

    [Fact]
    public async Task CreateStudyGroupHandler_WhenValidWithActiveTeacher_CreatesSuccessfully()
    {
        var academicYearId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var groupRepo = new MockStudyGroupRepository { NameExists = false };
        var opr = new MockOperationalContract
        {
            AcademicYear = new AcademicYearSummaryDto(academicYearId, "2026/2027", new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "ACTIVE")
        };
        var tch = new MockTeachingContract
        {
            Teacher = new TeacherSummaryDto(teacherId, "Budi Santoso", "ACTIVE")
        };
        var dispatcher = new MockDomainEventDispatcher();
        var handler = new CreateStudyGroupHandler(groupRepo, opr, tch, dispatcher);

        var result = await handler.HandleAsync(new CreateStudyGroupRequest(
            academicYearId,
            "SMP",
            "7",
            "7A",
            teacherId));

        Assert.True(result.IsSuccess);
        Assert.Equal("7A", result.Value!.Name);
        Assert.Equal("Budi Santoso", result.Value.HomeroomTeacherName);
        Assert.Equal(teacherId, result.Value.HomeroomTeacherId);
    }

    [Fact]
    public async Task UpdateStudyGroupHandler_WhenStudyGroupNotFound_Returns404()
    {
        var groupRepo = new MockStudyGroupRepository();
        var opr = new MockOperationalContract();
        var tch = new MockTeachingContract();
        var dispatcher = new MockDomainEventDispatcher();
        var handler = new UpdateStudyGroupHandler(groupRepo, opr, tch, dispatcher);

        var result = await handler.HandleAsync(
            Guid.NewGuid(),
            new UpdateStudyGroupRequest(Guid.NewGuid(), "SMP", "7", "7A", null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task PlaceStudentHandler_WhenStudyGroupMismatchGrade_Returns409Conflict()
    {
        var student = CreateSampleStudent();
        var academicYearId = Guid.NewGuid();
        var studentRepo = new MockStudentRepository { Student = student };
        var studyGroup = StudyGroup.Create(academicYearId, "2026/2027", SchoolLevel.SMP, "8", "8A").Value!;
        var groupRepo = new MockStudyGroupRepository { StudyGroup = studyGroup };
        var placementRepo = new MockPlacementRepository();
        var opr = new MockOperationalContract
        {
            AcademicYear = new AcademicYearSummaryDto(academicYearId, "2026/2027", new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "ACTIVE")
        };
        var dispatcher = new MockDomainEventDispatcher();

        var handler = new PlaceStudentHandler(studentRepo, groupRepo, placementRepo, opr, dispatcher);

        // Student placement requested for Grade 7, but study group is Grade 8
        var result = await handler.HandleAsync(
            student.Id,
            new PlaceStudentRequest(academicYearId, "SMP", "7", studyGroup.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Std.Placement.StudyGroupMismatch", result.Error.Code);
    }

    [Fact]
    public async Task PlaceStudentHandler_WhenValid_UpsertsAndDispatchesStudentPlacedDomainEvent()
    {
        var student = CreateSampleStudent();
        var academicYearId = Guid.NewGuid();
        var studentRepo = new MockStudentRepository { Student = student };
        var studyGroup = StudyGroup.Create(academicYearId, "2026/2027", SchoolLevel.SMP, "7", "7A").Value!;
        var groupRepo = new MockStudyGroupRepository { StudyGroup = studyGroup };
        var placementRepo = new MockPlacementRepository();
        var opr = new MockOperationalContract
        {
            AcademicYear = new AcademicYearSummaryDto(academicYearId, "2026/2027", new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "ACTIVE")
        };
        var dispatcher = new MockDomainEventDispatcher();

        var handler = new PlaceStudentHandler(studentRepo, groupRepo, placementRepo, opr, dispatcher);

        var result = await handler.HandleAsync(
            student.Id,
            new PlaceStudentRequest(academicYearId, "SMP", "7", studyGroup.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(student.Id, result.Value!.StudentId);
        Assert.Equal(studyGroup.Id, result.Value.StudyGroupId);
        Assert.Equal("7A", result.Value.StudyGroupName);
        Assert.Single(dispatcher.DispatchedEvents);
        Assert.IsType<StudentPlacedDomainEvent>(dispatcher.DispatchedEvents[0]);
    }

    [Fact]
    public void WriteHandlers_InStdModule_HaveZeroDapperOrSqlConnectionDependencies()
    {
        var stdAssembly = typeof(StdAssemblyMarker).Assembly;
        var writeHandlers = stdAssembly.GetTypes()
            .Where(t => t.IsClass &&
                        !t.IsAbstract &&
                        t.Name.EndsWith("Handler", StringComparison.Ordinal) &&
                        !t.Name.StartsWith("Get", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(writeHandlers);

        string[] forbiddenTypeNames =
        [
            "Dapper",
            typeof(ISqlConnectionFactory).FullName!
        ];

        foreach (var handler in writeHandlers)
        {
            var ctorParams = handler.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType.FullName ?? p.ParameterType.Name);

            var fields = handler.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                .Select(f => f.FieldType.FullName ?? f.FieldType.Name);

            foreach (var typeName in ctorParams.Concat(fields))
            {
                Assert.False(
                    forbiddenTypeNames.Any(f => typeName.Contains(f, StringComparison.Ordinal)),
                    $"Write handler '{handler.FullName}' must not depend on Dapper or ISqlConnectionFactory. Found: '{typeName}'.");
            }
        }
    }

    private static Student CreateSampleStudent()
    {
        var candidate = Candidate.Register(
            Guid.NewGuid(),
            "2026/2027",
            "Ahmad Fauzi",
            "Ahmad",
            new DateOnly(2010, 5, 20),
            Gender.Male,
            "081234567890",
            "ahmad@example.com",
            "Jl. Mawar 1",
            SchoolLevel.SMP,
            "7",
            [new GuardianInput("Budi Santoso", GuardianRelationshipType.Father, "081234567890")]).Value!;

        candidate.Accept();
        return Student.EnrollFromCandidate(candidate, "20260001", "0098765432").Value!;
    }

    private sealed class MockStudentRepository : IStudentRepository
    {
        public Student? Student { get; set; }
        public bool NisExists { get; set; }
        public bool NisnExists { get; set; }
        public Student? UpdatedStudent { get; set; }

        public Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Student is not null && Student.Id == id ? Student : null);

        public Task<bool> ExistsByNisAsync(string nis, Guid? excludeStudentId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(NisExists);

        public Task<bool> ExistsByNisnAsync(string nisn, Guid? excludeStudentId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(NisnExists);

        public Task<Result> AddAsync(Student student, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());

        public Task<Result> UpdateAsync(Student student, CancellationToken cancellationToken = default)
        {
            UpdatedStudent = student;
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class MockStudyGroupRepository : IStudyGroupRepository
    {
        public StudyGroup? StudyGroup { get; set; }
        public bool NameExists { get; set; }

        public Task<StudyGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(StudyGroup is not null && StudyGroup.Id == id ? StudyGroup : null);

        public Task<bool> ExistsByYearAndNameAsync(Guid academicYearId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(NameExists);

        public Task<Result> AddAsync(StudyGroup studyGroup, CancellationToken cancellationToken = default)
        {
            StudyGroup = studyGroup;
            return Task.FromResult(Result.Success());
        }

        public Task<Result> UpdateAsync(StudyGroup studyGroup, CancellationToken cancellationToken = default)
        {
            StudyGroup = studyGroup;
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class MockPlacementRepository : IStudentPlacementRepository
    {
        public StudentPlacement? Placement { get; set; }

        public Task<StudentPlacement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Placement is not null && Placement.Id == id ? Placement : null);

        public Task<StudentPlacement?> GetByStudentAndAcademicYearAsync(Guid studentId, Guid academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Placement is not null && Placement.StudentId == studentId && Placement.AcademicYearId == academicYearId ? Placement : null);

        public Task<Result> UpsertAsync(StudentPlacement placement, CancellationToken cancellationToken = default)
        {
            Placement = placement;
            return Task.FromResult(Result.Success());
        }

        public Task MarkAcademicallyCompletedAsync(Guid studentId, Guid operationalPeriodId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MockOperationalContract : IOperationalContract
    {
        public AcademicYearSummaryDto? AcademicYear { get; set; }

        public Task<ActiveAcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ActiveAcademicYearDto?>(null);

        public Task<ActiveOperationalPeriodDto?> GetActiveOperationalPeriodAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ActiveOperationalPeriodDto?>(null);

        public Task<AcademicYearSummaryDto?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(AcademicYear is not null && AcademicYear.Id == id ? AcademicYear : null);

        public Task<OperationalPeriodSummaryDto?> GetOperationalPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<OperationalPeriodSummaryDto?>(null);
    }

    private sealed class MockTeachingContract : ITeachingContract
    {
        public TeacherSummaryDto? Teacher { get; set; }

        public Task<TeacherSummaryDto?> GetTeacherByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Teacher is not null && Teacher.Id == id ? Teacher : null);
    }

    private sealed class MockDomainEventDispatcher : IDomainEventDispatcher
    {
        public List<DomainEventBase> DispatchedEvents { get; } = [];

        public Task DispatchAsync(IEnumerable<DomainEventBase> domainEvents, CancellationToken cancellationToken = default)
        {
            DispatchedEvents.AddRange(domainEvents);
            return Task.CompletedTask;
        }
    }
}
