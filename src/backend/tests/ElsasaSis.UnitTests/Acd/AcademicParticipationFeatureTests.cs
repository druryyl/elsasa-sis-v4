using System.Data;
using System.Security.Claims;
using ElsasaSis.Contracts.Academic;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Contracts.Teaching;
using ElsasaSis.Modules.Acd;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.Modules.Acd.Features.Participations.EnrollStudentsInSubjectStages;
using ElsasaSis.Modules.Acd.Features.Participations.GetSubjectStageParticipations;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElsasaSis.UnitTests.Acd;

public sealed class AcademicParticipationFeatureTests
{
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _stageId = Guid.NewGuid();
    private readonly Guid _subjectId = Guid.NewGuid();
    private readonly Guid _studyGroupId = Guid.NewGuid();

    [Fact]
    public void Validator_WhenFieldsEmpty_FailsValidation()
    {
        var validator = new EnrollStudentsInSubjectStagesValidator();

        Assert.False(validator.Validate(new EnrollStudentsInSubjectStagesRequest(Guid.Empty, _stageId, _studyGroupId)).IsValid);
        Assert.False(validator.Validate(new EnrollStudentsInSubjectStagesRequest(_periodId, Guid.Empty, _studyGroupId)).IsValid);
        Assert.False(validator.Validate(new EnrollStudentsInSubjectStagesRequest(_periodId, _stageId, Guid.Empty)).IsValid);
        Assert.True(validator.Validate(new EnrollStudentsInSubjectStagesRequest(_periodId, _stageId, _studyGroupId)).IsValid);
    }

    [Fact]
    public async Task Handler_WhenPeriodNotFound_ReturnsNotFound()
    {
        var participationRepo = new MockAcademicParticipationRepository();
        var stageRepo = new MockSubjectStageRepository();
        var oprContract = new MockOperationalContract();
        var stdContract = new MockStudentContract();

        var handler = new EnrollStudentsInSubjectStagesHandler(
            participationRepo,
            stageRepo,
            oprContract,
            stdContract);

        var result = await handler.HandleAsync(new EnrollStudentsInSubjectStagesRequest(_periodId, _stageId, _studyGroupId));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Participation.OperationalPeriodNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handler_WhenPeriodNotActive_ReturnsConflict()
    {
        var participationRepo = new MockAcademicParticipationRepository();
        var stageRepo = new MockSubjectStageRepository();
        var oprContract = new MockOperationalContract
        {
            Period = new OperationalPeriodSummaryDto(
                _periodId,
                _academicYearId,
                "2026/2027",
                "Semester 1",
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 12, 31),
                "CLOSED")
        };
        var stdContract = new MockStudentContract();

        var handler = new EnrollStudentsInSubjectStagesHandler(
            participationRepo,
            stageRepo,
            oprContract,
            stdContract);

        var result = await handler.HandleAsync(new EnrollStudentsInSubjectStagesRequest(_periodId, _stageId, _studyGroupId));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Participation.OperationalPeriodNotActive", result.Error.Code);
    }

    [Fact]
    public async Task Handler_WhenStageNotFound_ReturnsNotFound()
    {
        var participationRepo = new MockAcademicParticipationRepository();
        var stageRepo = new MockSubjectStageRepository();
        var oprContract = new MockOperationalContract
        {
            Period = new OperationalPeriodSummaryDto(
                _periodId,
                _academicYearId,
                "2026/2027",
                "Semester 1",
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 12, 31),
                "ACTIVE")
        };
        var stdContract = new MockStudentContract();

        var handler = new EnrollStudentsInSubjectStagesHandler(
            participationRepo,
            stageRepo,
            oprContract,
            stdContract);

        var result = await handler.HandleAsync(new EnrollStudentsInSubjectStagesRequest(_periodId, _stageId, _studyGroupId));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Participation.SubjectStageNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handler_WhenStudyGroupNotFound_ReturnsNotFound()
    {
        var participationRepo = new MockAcademicParticipationRepository();
        var stageRepo = new MockSubjectStageRepository();
        stageRepo.Stages.Add(SubjectStage.Create(_subjectId, "Matematika", "Stage 7", 1, null).Value!);

        var oprContract = new MockOperationalContract
        {
            Period = new OperationalPeriodSummaryDto(
                _periodId,
                _academicYearId,
                "2026/2027",
                "Semester 1",
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 12, 31),
                "ACTIVE")
        };
        var stdContract = new MockStudentContract(); // Study group not set -> returns null

        var handler = new EnrollStudentsInSubjectStagesHandler(
            participationRepo,
            stageRepo,
            oprContract,
            stdContract);

        var result = await handler.HandleAsync(new EnrollStudentsInSubjectStagesRequest(
            _periodId,
            stageRepo.Stages[0].Id,
            _studyGroupId));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Participation.StudyGroupNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handler_WhenNoStudentsInStudyGroup_ReturnsZeroCounts()
    {
        var participationRepo = new MockAcademicParticipationRepository();
        var stageRepo = new MockSubjectStageRepository();
        var stage = SubjectStage.Create(_subjectId, "Matematika", "Stage 7", 1, null).Value!;
        stageRepo.Stages.Add(stage);

        var oprContract = new MockOperationalContract
        {
            Period = new OperationalPeriodSummaryDto(
                _periodId,
                _academicYearId,
                "2026/2027",
                "Semester 1",
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 12, 31),
                "ACTIVE")
        };

        var stdContract = new MockStudentContract
        {
            StudyGroup = new StudyGroupSummaryDto(
                _studyGroupId,
                _academicYearId,
                "2026/2027",
                "SMP",
                "7",
                "7A",
                null,
                null)
        };

        var handler = new EnrollStudentsInSubjectStagesHandler(
            participationRepo,
            stageRepo,
            oprContract,
            stdContract);

        var result = await handler.HandleAsync(new EnrollStudentsInSubjectStagesRequest(
            _periodId,
            stage.Id,
            _studyGroupId));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.EnrolledCount);
        Assert.Equal(0, result.Value.SkippedCount);
    }

    [Fact]
    public async Task Handler_WhenAllStudentsNew_EnrollsAll()
    {
        var student1 = Guid.NewGuid();
        var student2 = Guid.NewGuid();

        var participationRepo = new MockAcademicParticipationRepository();
        participationRepo.Placements.Add(new StudentPlacementRosterDto(
            student1,
            "Ahmad Dahlan",
            "NIS-001",
            _studyGroupId,
            "7A",
            _academicYearId,
            "2026/2027"));
        participationRepo.Placements.Add(new StudentPlacementRosterDto(
            student2,
            "Siti Nurhaliza",
            "NIS-002",
            _studyGroupId,
            "7A",
            _academicYearId,
            "2026/2027"));

        var stageRepo = new MockSubjectStageRepository();
        var stage = SubjectStage.Create(_subjectId, "Matematika", "Stage 7", 1, null).Value!;
        stageRepo.Stages.Add(stage);

        var oprContract = new MockOperationalContract
        {
            Period = new OperationalPeriodSummaryDto(
                _periodId,
                _academicYearId,
                "2026/2027",
                "Semester 1",
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 12, 31),
                "ACTIVE")
        };

        var stdContract = new MockStudentContract
        {
            StudyGroup = new StudyGroupSummaryDto(
                _studyGroupId,
                _academicYearId,
                "2026/2027",
                "SMP",
                "7",
                "7A",
                null,
                null)
        };

        var handler = new EnrollStudentsInSubjectStagesHandler(
            participationRepo,
            stageRepo,
            oprContract,
            stdContract);

        var result = await handler.HandleAsync(new EnrollStudentsInSubjectStagesRequest(
            _periodId,
            stage.Id,
            _studyGroupId));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.EnrolledCount);
        Assert.Equal(0, result.Value.SkippedCount);
        Assert.Equal(2, participationRepo.Participations.Count);

        var first = participationRepo.Participations.First(p => p.StudentId == student1);
        Assert.Equal("Ahmad Dahlan", first.StudentName);
        Assert.Equal("NIS-001", first.StudentNis);
        Assert.Equal(stage.Id, first.SubjectStageId);
        Assert.Equal("Stage 7", first.SubjectStageName);
        Assert.Equal(_subjectId, first.SubjectId);
        Assert.Equal("Matematika", first.SubjectName);
        Assert.Equal(_periodId, first.OperationalPeriodId);
        Assert.Equal("Semester 1", first.OperationalPeriodName);
        Assert.Equal(_studyGroupId, first.StudyGroupId);
        Assert.Equal("7A", first.StudyGroupName);
        Assert.Equal(ParticipationStatus.Active, first.Status);
    }

    [Fact]
    public async Task Handler_WhenSomeStudentsAlreadyEnrolled_SkipsGracefully()
    {
        var student1 = Guid.NewGuid();
        var student2 = Guid.NewGuid();

        var participationRepo = new MockAcademicParticipationRepository();
        participationRepo.Placements.Add(new StudentPlacementRosterDto(
            student1,
            "Ahmad Dahlan",
            "NIS-001",
            _studyGroupId,
            "7A",
            _academicYearId,
            "2026/2027"));
        participationRepo.Placements.Add(new StudentPlacementRosterDto(
            student2,
            "Siti Nurhaliza",
            "NIS-002",
            _studyGroupId,
            "7A",
            _academicYearId,
            "2026/2027"));

        var stageRepo = new MockSubjectStageRepository();
        var stage = SubjectStage.Create(_subjectId, "Matematika", "Stage 7", 1, null).Value!;
        stageRepo.Stages.Add(stage);

        // Pre-enroll student 1
        var existingP = AcademicParticipation.Create(
            student1,
            "Ahmad Dahlan",
            "NIS-001",
            stage.Id,
            stage.Name,
            stage.SubjectId,
            stage.SubjectName,
            _periodId,
            "Semester 1",
            _academicYearId,
            "2026/2027",
            _studyGroupId,
            "7A").Value!;
        participationRepo.Participations.Add(existingP);

        var oprContract = new MockOperationalContract
        {
            Period = new OperationalPeriodSummaryDto(
                _periodId,
                _academicYearId,
                "2026/2027",
                "Semester 1",
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 12, 31),
                "ACTIVE")
        };

        var stdContract = new MockStudentContract
        {
            StudyGroup = new StudyGroupSummaryDto(
                _studyGroupId,
                _academicYearId,
                "2026/2027",
                "SMP",
                "7",
                "7A",
                null,
                null)
        };

        var handler = new EnrollStudentsInSubjectStagesHandler(
            participationRepo,
            stageRepo,
            oprContract,
            stdContract);

        var result = await handler.HandleAsync(new EnrollStudentsInSubjectStagesRequest(
            _periodId,
            stage.Id,
            _studyGroupId));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.EnrolledCount);
        Assert.Equal(1, result.Value.SkippedCount);
        Assert.Equal(2, participationRepo.Participations.Count);
    }

    [Fact]
    public void DependencyInjection_AddAcdModule_RegistersParticipationServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISqlConnectionFactory>(new MockSqlConnFactory());
        services.AddSingleton<ICurrentUser>(new MockCurrentUser());
        services.AddSingleton<IOperationalContract>(new MockOperationalContract());
        services.AddSingleton<IStudentContract>(new MockStudentContract());

        services.AddAcdModule();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IAcademicParticipationRepository>());
        Assert.NotNull(scope.ServiceProvider.GetService<EnrollStudentsInSubjectStagesHandler>());
        Assert.NotNull(scope.ServiceProvider.GetService<GetSubjectStageParticipationsHandler>());
    }

    private sealed class MockAcademicParticipationRepository : IAcademicParticipationRepository
    {
        public List<AcademicParticipation> Participations { get; } = [];
        public List<StudentPlacementRosterDto> Placements { get; } = [];

        public Task<AcademicParticipation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Participations.FirstOrDefault(p => p.Id == id));

        public Task<AcademicParticipation?> GetByStudentStagePeriodAsync(
            Guid studentId,
            Guid subjectStageId,
            Guid operationalPeriodId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Participations.FirstOrDefault(p =>
                p.StudentId == studentId &&
                p.SubjectStageId == subjectStageId &&
                p.OperationalPeriodId == operationalPeriodId));

        public Task<IReadOnlyList<AcademicParticipation>> GetByCriteriaAsync(
            Guid? subjectStageId,
            Guid? studyGroupId,
            Guid? operationalPeriodId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AcademicParticipation>>(Participations.Where(p =>
                (!subjectStageId.HasValue || p.SubjectStageId == subjectStageId.Value) &&
                (!studyGroupId.HasValue || p.StudyGroupId == studyGroupId.Value) &&
                (!operationalPeriodId.HasValue || p.OperationalPeriodId == operationalPeriodId.Value)
            ).ToList());

        public Task<IReadOnlySet<Guid>> GetEnrolledStudentIdsAsync(
            Guid subjectStageId,
            Guid operationalPeriodId,
            IEnumerable<Guid> studentIds,
            CancellationToken cancellationToken = default)
        {
            var idSet = studentIds.ToHashSet();
            var enrolled = Participations
                .Where(p => p.SubjectStageId == subjectStageId &&
                            p.OperationalPeriodId == operationalPeriodId &&
                            idSet.Contains(p.StudentId))
                .Select(p => p.StudentId)
                .ToHashSet();

            return Task.FromResult<IReadOnlySet<Guid>>(enrolled);
        }

        public Task<IReadOnlyList<StudentPlacementRosterDto>> GetActivePlacementsByStudyGroupAsync(
            Guid studyGroupId,
            Guid academicYearId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentPlacementRosterDto>>(
                Placements.Where(p => p.StudyGroupId == studyGroupId && p.AcademicYearId == academicYearId).ToList());

        public Task<Result> AddAsync(AcademicParticipation participation, CancellationToken cancellationToken = default)
        {
            if (Participations.Any(p =>
                p.StudentId == participation.StudentId &&
                p.SubjectStageId == participation.SubjectStageId &&
                p.OperationalPeriodId == participation.OperationalPeriodId))
            {
                return Task.FromResult(Result.Failure(ParticipationErrors.DuplicateParticipation(
                    participation.StudentId,
                    participation.SubjectStageId,
                    participation.OperationalPeriodId)));
            }

            Participations.Add(participation);
            return Task.FromResult(Result.Success());
        }

        public Task<int> AddBatchAsync(IEnumerable<AcademicParticipation> participations, CancellationToken cancellationToken = default)
        {
            var count = 0;
            foreach (var p in participations)
            {
                if (!Participations.Any(existing =>
                    existing.StudentId == p.StudentId &&
                    existing.SubjectStageId == p.SubjectStageId &&
                    existing.OperationalPeriodId == p.OperationalPeriodId))
                {
                    Participations.Add(p);
                    count++;
                }
            }

            return Task.FromResult(count);
        }

        public Task<Result> UpdateAsync(AcademicParticipation participation, CancellationToken cancellationToken = default)
        {
            var existingIndex = Participations.FindIndex(p => p.Id == participation.Id);
            if (existingIndex < 0)
            {
                return Task.FromResult(Result.Failure(ParticipationErrors.ParticipationNotFound(participation.Id)));
            }

            Participations[existingIndex] = participation;
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class MockSubjectStageRepository : ISubjectStageRepository
    {
        public List<SubjectStage> Stages { get; } = [];

        public Task<SubjectStage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stages.FirstOrDefault(s => s.Id == id));

        public Task<bool> ExistsByNameAsync(Guid subjectId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stages.Any(s => s.SubjectId == subjectId && s.Id != excludeId && s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> ExistsBySequenceOrderAsync(Guid subjectId, int sequenceOrder, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stages.Any(s => s.SubjectId == subjectId && s.Id != excludeId && s.SequenceOrder == sequenceOrder));

        public Task<Result> AddAsync(SubjectStage stage, CancellationToken cancellationToken = default)
        {
            Stages.Add(stage);
            return Task.FromResult(Result.Success());
        }

        public Task<Result> UpdateAsync(SubjectStage stage, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class MockOperationalContract : IOperationalContract
    {
        public OperationalPeriodSummaryDto? Period { get; set; }

        public Task<ActiveAcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ActiveAcademicYearDto?>(null);

        public Task<ActiveOperationalPeriodDto?> GetActiveOperationalPeriodAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ActiveOperationalPeriodDto?>(null);

        public Task<AcademicYearSummaryDto?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<AcademicYearSummaryDto?>(null);

        public Task<OperationalPeriodSummaryDto?> GetOperationalPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Period?.Id == id ? Period : null);
    }

    private sealed class MockStudentContract : IStudentContract
    {
        public StudyGroupSummaryDto? StudyGroup { get; set; }

        public Task<StudyGroupSummaryDto?> GetStudyGroupByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(StudyGroup?.Id == id ? StudyGroup : null);

        public Task<IReadOnlyList<StudentPlacementScopeDto>> GetActiveStudentsByScopeAsync(
            Guid? studyGroupId,
            string? schoolLevel,
            string? grade,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentPlacementScopeDto>>([]);
    }

    private sealed class MockSqlConnFactory : ISqlConnectionFactory
    {
        public IDbConnection CreateConnection() => throw new NotImplementedException();
        public Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class MockCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = "admin";
        public string FullName { get; set; } = "Administrator";
        public IReadOnlyList<string> Roles { get; set; } = ["SCHOOL_ADMIN"];
        public IReadOnlyList<string> Permissions { get; set; } = [ElsasaSis.Modules.Acd.Permissions.Academic.Curriculum.Manage];
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? GuardianId => null;
    }
}
