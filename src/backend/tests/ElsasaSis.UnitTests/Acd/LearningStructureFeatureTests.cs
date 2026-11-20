using ElsasaSis.Modules.Acd;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.Modules.Acd.Features.LearningStructure.CreateLearningUnit;
using ElsasaSis.Modules.Acd.Features.LearningStructure.CreateSubject;
using ElsasaSis.Modules.Acd.Features.LearningStructure.CreateSubjectStage;
using ElsasaSis.Modules.Acd.Features.LearningStructure.MapSubjectStageGrade;
using ElsasaSis.Modules.Acd.Features.LearningStructure.RemoveLearningUnit;
using ElsasaSis.Modules.Acd.Features.LearningStructure.RemoveSubjectStageGrade;
using ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateLearningUnit;
using ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateSubject;
using ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateSubjectStage;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElsasaSis.UnitTests.Acd;

public sealed class LearningStructureFeatureTests
{
    [Fact]
    public async Task CreateSubjectHandler_WhenNameAlreadyExists_ReturnsDuplicateNameError()
    {
        var subjectRepo = new MockSubjectRepository { NameExists = true };
        var handler = new CreateSubjectHandler(subjectRepo);

        var result = await handler.HandleAsync(new CreateSubjectRequest("Matematika", "MTK"));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Subject.DuplicateName", result.Error.Code);
    }

    [Fact]
    public async Task CreateSubjectHandler_WhenCodeAlreadyExists_ReturnsDuplicateCodeError()
    {
        var subjectRepo = new MockSubjectRepository { NameExists = false, CodeExists = true };
        var handler = new CreateSubjectHandler(subjectRepo);

        var result = await handler.HandleAsync(new CreateSubjectRequest("Matematika Baru", "MTK"));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Subject.DuplicateCode", result.Error.Code);
    }

    [Fact]
    public async Task CreateSubjectHandler_WhenValid_AddsSubjectAndReturnsDto()
    {
        var subjectRepo = new MockSubjectRepository();
        var handler = new CreateSubjectHandler(subjectRepo);

        var result = await handler.HandleAsync(new CreateSubjectRequest("Matematika", "MTK"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Matematika", result.Value!.Name);
        Assert.Equal("MTK", result.Value.Code);
        Assert.Single(subjectRepo.Subjects);
    }

    [Fact]
    public void CreateSubjectValidator_ValidationRules()
    {
        var validator = new CreateSubjectValidator();

        Assert.False(validator.Validate(new CreateSubjectRequest("", null)).IsValid);
        Assert.False(validator.Validate(new CreateSubjectRequest("Name", new string('C', 31))).IsValid);
        Assert.True(validator.Validate(new CreateSubjectRequest("Matematika", "MTK")).IsValid);
    }

    [Fact]
    public void CreateSubjectStageValidator_ValidationRules()
    {
        var validator = new CreateSubjectStageValidator();

        Assert.False(validator.Validate(new CreateSubjectStageRequest("", 1, null)).IsValid);
        Assert.False(validator.Validate(new CreateSubjectStageRequest("Stage", 0, null)).IsValid);
        Assert.False(validator.Validate(new CreateSubjectStageRequest("Stage", -1, null)).IsValid);
        Assert.False(validator.Validate(new CreateSubjectStageRequest(new string('S', 101), 1, null)).IsValid);
        Assert.True(validator.Validate(new CreateSubjectStageRequest("Stage 7", 1, "Description")).IsValid);
    }

    [Fact]
    public void CreateLearningUnitValidator_ValidationRules()
    {
        var validator = new CreateLearningUnitValidator();

        Assert.False(validator.Validate(new CreateLearningUnitRequest("", 1, "MODULE", null)).IsValid);
        Assert.False(validator.Validate(new CreateLearningUnitRequest("Unit", 0, "MODULE", null)).IsValid);
        Assert.False(validator.Validate(new CreateLearningUnitRequest("Unit", 1, "", null)).IsValid);
        Assert.False(validator.Validate(new CreateLearningUnitRequest("Unit", 1, "INVALID_TYPE", null)).IsValid);
        Assert.True(validator.Validate(new CreateLearningUnitRequest("Unit 1", 1, "MODULE", "Desc")).IsValid);
    }

    [Fact]
    public void MapSubjectStageGradeValidator_ValidationRules()
    {
        var validator = new MapSubjectStageGradeValidator();

        Assert.False(validator.Validate(new MapSubjectStageGradeRequest("", "7")).IsValid);
        Assert.False(validator.Validate(new MapSubjectStageGradeRequest("INVALID_LEVEL", "7")).IsValid);
        Assert.False(validator.Validate(new MapSubjectStageGradeRequest("SMP", "")).IsValid);
        Assert.False(validator.Validate(new MapSubjectStageGradeRequest("SMP", "10")).IsValid);
        Assert.False(validator.Validate(new MapSubjectStageGradeRequest("SD", "7")).IsValid);
        Assert.True(validator.Validate(new MapSubjectStageGradeRequest("SMP", "7")).IsValid);
        Assert.True(validator.Validate(new MapSubjectStageGradeRequest("SMA", "10")).IsValid);
        Assert.True(validator.Validate(new MapSubjectStageGradeRequest("SMK", "12")).IsValid);
    }

    [Fact]
    public async Task UpdateSubjectHandler_WhenSubjectNotFound_ReturnsNotFoundError()
    {
        var subjectRepo = new MockSubjectRepository();
        var handler = new UpdateSubjectHandler(subjectRepo);

        var result = await handler.HandleAsync(Guid.NewGuid(), new UpdateSubjectRequest("New Name", null));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Subject.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpdateSubjectHandler_WhenValid_UpdatesSubject()
    {
        var subject = Subject.Create("Old Name", "OLD").Value!;
        var subjectRepo = new MockSubjectRepository();
        subjectRepo.Subjects.Add(subject);

        var handler = new UpdateSubjectHandler(subjectRepo);
        var result = await handler.HandleAsync(subject.Id, new UpdateSubjectRequest("New Name", "NEW"));

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", result.Value!.Name);
        Assert.Equal("NEW", result.Value.Code);
    }

    [Fact]
    public async Task CreateSubjectStageHandler_WhenSubjectNotFound_ReturnsNotFoundError()
    {
        var subjectRepo = new MockSubjectRepository();
        var stageRepo = new MockSubjectStageRepository();

        var handler = new CreateSubjectStageHandler(subjectRepo, stageRepo);
        var result = await handler.HandleAsync(Guid.NewGuid(), new CreateSubjectStageRequest("Stage 7", 1, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Subject.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateSubjectStageHandler_WhenSequenceOrderExists_ReturnsConflict()
    {
        var subject = Subject.Create("Matematika", "MTK").Value!;
        var subjectRepo = new MockSubjectRepository();
        subjectRepo.Subjects.Add(subject);

        var stageRepo = new MockSubjectStageRepository { SequenceOrderExists = true };

        var handler = new CreateSubjectStageHandler(subjectRepo, stageRepo);
        var result = await handler.HandleAsync(subject.Id, new CreateSubjectStageRequest("Stage 7", 1, null));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.SubjectStage.DuplicateSequence", result.Error.Code);
    }

    [Fact]
    public async Task CreateSubjectStageHandler_WhenValid_CreatesStage()
    {
        var subject = Subject.Create("Matematika", "MTK").Value!;
        var subjectRepo = new MockSubjectRepository();
        subjectRepo.Subjects.Add(subject);

        var stageRepo = new MockSubjectStageRepository();

        var handler = new CreateSubjectStageHandler(subjectRepo, stageRepo);
        var result = await handler.HandleAsync(subject.Id, new CreateSubjectStageRequest("Stage 7", 1, "Desc"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Stage 7", result.Value!.Name);
        Assert.Equal(1, result.Value.SequenceOrder);
        Assert.Equal("Desc", result.Value.Description);
        Assert.Single(stageRepo.Stages);
    }

    [Fact]
    public async Task UpdateSubjectStageHandler_WhenValid_UpdatesStage()
    {
        var subjectId = Guid.NewGuid();
        var stage = SubjectStage.Create(subjectId, "MTK", "Stage 7", 1, null).Value!;
        var stageRepo = new MockSubjectStageRepository();
        stageRepo.Stages.Add(stage);

        var handler = new UpdateSubjectStageHandler(stageRepo);
        var result = await handler.HandleAsync(subjectId, stage.Id, new UpdateSubjectStageRequest("Stage 7 Updated", 2, "New Desc"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Stage 7 Updated", result.Value!.Name);
        Assert.Equal(2, result.Value.SequenceOrder);
    }

    [Fact]
    public async Task CreateLearningUnitHandler_WhenStageNotFound_ReturnsNotFoundError()
    {
        var stageRepo = new MockSubjectStageRepository();
        var handler = new CreateLearningUnitHandler(stageRepo);

        var result = await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateLearningUnitRequest("Unit 1", 1, "MODULE", null));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.SubjectStage.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateLearningUnitHandler_WhenValid_AddsUnit()
    {
        var subjectId = Guid.NewGuid();
        var stage = SubjectStage.Create(subjectId, "MTK", "Stage 7", 1, null).Value!;
        var stageRepo = new MockSubjectStageRepository();
        stageRepo.Stages.Add(stage);

        var handler = new CreateLearningUnitHandler(stageRepo);
        var result = await handler.HandleAsync(subjectId, stage.Id, new CreateLearningUnitRequest("Aljabar", 1, "MODULE", "Intro"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Aljabar", result.Value!.Name);
        Assert.Equal("MODULE", result.Value.UnitType);
        Assert.Equal(1, result.Value.SequenceOrder);
    }

    [Fact]
    public async Task UpdateLearningUnitHandler_WhenValid_UpdatesUnit()
    {
        var subjectId = Guid.NewGuid();
        var stage = SubjectStage.Create(subjectId, "MTK", "Stage 7", 1, null).Value!;
        var unit = stage.AddLearningUnit("Aljabar", 1, "MODULE", null).Value!;
        var stageRepo = new MockSubjectStageRepository();
        stageRepo.Stages.Add(stage);

        var handler = new UpdateLearningUnitHandler(stageRepo);
        var result = await handler.HandleAsync(subjectId, stage.Id, unit.Id, new UpdateLearningUnitRequest("Aljabar 2", 1, "PACE", "Updated"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Aljabar 2", result.Value!.Name);
        Assert.Equal("PACE", result.Value.UnitType);
    }

    [Fact]
    public async Task RemoveLearningUnitHandler_WhenValid_RemovesUnit()
    {
        var subjectId = Guid.NewGuid();
        var stage = SubjectStage.Create(subjectId, "MTK", "Stage 7", 1, null).Value!;
        var unit = stage.AddLearningUnit("Aljabar", 1, "MODULE", null).Value!;
        var stageRepo = new MockSubjectStageRepository();
        stageRepo.Stages.Add(stage);

        var handler = new RemoveLearningUnitHandler(stageRepo);
        var result = await handler.HandleAsync(subjectId, stage.Id, unit.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(stage.LearningUnits);
    }

    [Fact]
    public async Task MapSubjectStageGradeHandler_WhenValid_AddsGradeMapping()
    {
        var subjectId = Guid.NewGuid();
        var stage = SubjectStage.Create(subjectId, "MTK", "Stage 7", 1, null).Value!;
        var stageRepo = new MockSubjectStageRepository();
        stageRepo.Stages.Add(stage);

        var handler = new MapSubjectStageGradeHandler(stageRepo);
        var result = await handler.HandleAsync(subjectId, stage.Id, new MapSubjectStageGradeRequest("SMP", "7"));

        Assert.True(result.IsSuccess);
        Assert.Equal("SMP", result.Value!.SchoolLevel);
        Assert.Equal("7", result.Value.Grade);
    }

    [Fact]
    public async Task RemoveSubjectStageGradeHandler_WhenValid_RemovesMapping()
    {
        var subjectId = Guid.NewGuid();
        var stage = SubjectStage.Create(subjectId, "MTK", "Stage 7", 1, null).Value!;
        var mapping = stage.AddGradeMapping("SMP", "7").Value!;
        var stageRepo = new MockSubjectStageRepository();
        stageRepo.Stages.Add(stage);

        var handler = new RemoveSubjectStageGradeHandler(stageRepo);
        var result = await handler.HandleAsync(subjectId, stage.Id, mapping.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(stage.GradeMappings);
    }

    [Fact]
    public void DependencyInjection_AddAcdModule_RegistersSubjectRepositories()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISqlConnectionFactory>(new MockSqlConnFactory());
        services.AddSingleton<ICurrentUser>(new MockUser());

        services.AddAcdModule();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<ISubjectRepository>());
        Assert.NotNull(scope.ServiceProvider.GetService<ISubjectStageRepository>());
        Assert.NotNull(scope.ServiceProvider.GetService<CreateSubjectHandler>());
        Assert.NotNull(scope.ServiceProvider.GetService<CreateSubjectStageHandler>());
        Assert.NotNull(scope.ServiceProvider.GetService<CreateLearningUnitHandler>());
        Assert.NotNull(scope.ServiceProvider.GetService<MapSubjectStageGradeHandler>());
    }

    private sealed class MockSubjectRepository : ISubjectRepository
    {
        public List<Subject> Subjects { get; } = [];
        public bool NameExists { get; set; }
        public bool CodeExists { get; set; }

        public Task<Subject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Subjects.FirstOrDefault(s => s.Id == id));

        public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(NameExists || Subjects.Any(s => s.Id != excludeId && s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(CodeExists || Subjects.Any(s => s.Id != excludeId && s.Code != null && s.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));

        public Task<Result> AddAsync(Subject subject, CancellationToken cancellationToken = default)
        {
            Subjects.Add(subject);
            return Task.FromResult(Result.Success());
        }

        public Task<Result> UpdateAsync(Subject subject, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class MockSubjectStageRepository : ISubjectStageRepository
    {
        public List<SubjectStage> Stages { get; } = [];
        public bool NameExists { get; set; }
        public bool SequenceOrderExists { get; set; }

        public Task<SubjectStage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stages.FirstOrDefault(s => s.Id == id));

        public Task<bool> ExistsByNameAsync(Guid subjectId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(NameExists || Stages.Any(s => s.SubjectId == subjectId && s.Id != excludeId && s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> ExistsBySequenceOrderAsync(Guid subjectId, int sequenceOrder, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(SequenceOrderExists || Stages.Any(s => s.SubjectId == subjectId && s.Id != excludeId && s.SequenceOrder == sequenceOrder));

        public Task<Result> AddAsync(SubjectStage stage, CancellationToken cancellationToken = default)
        {
            Stages.Add(stage);
            return Task.FromResult(Result.Success());
        }

        public Task<Result> UpdateAsync(SubjectStage stage, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class MockSqlConnFactory : ISqlConnectionFactory
    {
        public System.Data.IDbConnection CreateConnection() => throw new NotImplementedException();
        public Task<System.Data.IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockUser : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
        public string Username => "admin";
        public string FullName => "Admin User";
        public IReadOnlyList<string> Roles => ["SCHOOL_ADMIN"];
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? GuardianId => null;
        public IReadOnlyList<string> Permissions => [];
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => true;
        public bool HasPermission(string permission) => true;
    }
}
