using System.Data;
using System.Data.Common;
using ElsasaSis.Contracts.Development.Events;
using ElsasaSis.Modules.Dev;
using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.Modules.Dev.Features.Achievements;
using ElsasaSis.Modules.Dev.Features.Behaviour;
using ElsasaSis.Modules.Dev.Features.Discipline;
using ElsasaSis.Modules.Dev.Features.Dossier;
using ElsasaSis.Modules.Dev.Features.Overview;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;
using ElsasaSis.SharedKernel.Events;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;
using Microsoft.Extensions.DependencyInjection;

namespace ElsasaSis.UnitTests.Development;

public sealed class DevelopmentFeatureTests
{
    private static readonly Guid StudentId = Guid.NewGuid();
    private const string StudentName = "Ahmad Dahlan";
    private const string StudentNis = "2026001";
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private const string AcademicYearName = "2026/2027";
    private static readonly Guid RecorderId = Guid.NewGuid();
    private const string RecorderName = "Budi Santoso";

    #region Validator Tests

    [Fact]
    public async Task RecordBehaviourNoteValidator_WhenValid_Passes()
    {
        var validator = new RecordBehaviourNoteValidator();
        var request = new RecordBehaviourNoteRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            "POSITIVE",
            new DateOnly(2026, 9, 1),
            "Siswa sangat aktif.");

        var result = await validator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task RecordBehaviourNoteValidator_WhenInvalid_Fails()
    {
        var validator = new RecordBehaviourNoteValidator();
        var request = new RecordBehaviourNoteRequest(
            Guid.Empty,
            "",
            "",
            null,
            null,
            "INVALID_TYPE",
            default,
            "");

        var result = await validator.ValidateAsync(request);
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 5);
    }

    [Fact]
    public async Task RecordDisciplineIncidentValidator_WhenTeacherAllowed_Passes()
    {
        var validator = new RecordDisciplineIncidentValidator();
        var request = new RecordDisciplineIncidentRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 2),
            "KETERLAMBATAN",
            "Terlambat 3 kali berturut-turut.",
            "VERBAL_WARNING");

        var result = await validator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task RecordDisciplineIncidentValidator_WhenTeacherDisallowedEscalated_Fails()
    {
        var validator = new RecordDisciplineIncidentValidator();
        var request = new RecordDisciplineIncidentRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 2),
            "PERKELAHIAN",
            "Terlibat perkelahian di luar sekolah.",
            "SUSPENSION");

        var result = await validator.ValidateAsync(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Sanction");
    }

    [Fact]
    public async Task RecordEscalatedSanctionValidator_WhenPrincipalAllowed_Passes()
    {
        var validator = new RecordEscalatedSanctionValidator();
        var request = new RecordEscalatedSanctionRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 3),
            "PERKELAHIAN_BERAT",
            "Pelanggaran disiplin berat.",
            "SUSPENSION");

        var result = await validator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task RecordEscalatedSanctionValidator_WhenPrincipalDisallowedClassroom_Fails()
    {
        var validator = new RecordEscalatedSanctionValidator();
        var request = new RecordEscalatedSanctionRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 3),
            "PELANGGARAN_SERAGAM",
            "Tidak memakai dasi.",
            "VERBAL_WARNING");

        var result = await validator.ValidateAsync(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Sanction");
    }

    [Fact]
    public async Task RecordAchievementValidator_WhenValid_Passes()
    {
        var validator = new RecordAchievementValidator();
        var request = new RecordAchievementRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            "SPORTS",
            "Kejuaraan Futsal Daerah",
            "PROVINCE",
            new DateOnly(2026, 9, 4),
            "Juara 1 Tingkat Provinsi.");

        var result = await validator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task RecordAchievementValidator_WhenInvalid_Fails()
    {
        var validator = new RecordAchievementValidator();
        var request = new RecordAchievementRequest(
            StudentId,
            StudentName,
            StudentNis,
            null,
            null,
            "INVALID_TYPE",
            "",
            "INVALID_LEVEL",
            default,
            new string('a', 501));

        var result = await validator.ValidateAsync(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "AchievementType");
        Assert.Contains(result.Errors, e => e.PropertyName == "EventName");
        Assert.Contains(result.Errors, e => e.PropertyName == "RecognitionLevel");
        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }

    #endregion

    #region Handler Tests

    [Fact]
    public async Task RecordBehaviourNoteHandler_WhenValid_SavesAndReturnsDto()
    {
        var repo = new FakeBehaviourNoteRepository();
        var currentUser = new FakeCurrentUser(RecorderId, "budi.teacher", RecorderName);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new RecordBehaviourNoteHandler(repo, currentUser, dispatcher);

        var request = new RecordBehaviourNoteRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            "POSITIVE",
            new DateOnly(2026, 9, 1),
            "Membantu rekan yang tertinggal pelajaran fisika.");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(StudentId, result.Value.StudentId);
        Assert.Equal(RecorderId, result.Value.RecorderId);
        Assert.Equal(RecorderName, result.Value.RecorderName);
        Assert.Equal("POSITIVE", result.Value.BehaviourType);
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task RecordBehaviourNoteHandler_WhenDomainError_ReturnsFailure()
    {
        var repo = new FakeBehaviourNoteRepository();
        var currentUser = new FakeCurrentUser(Guid.Empty, "", "");
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new RecordBehaviourNoteHandler(repo, currentUser, dispatcher);

        var request = new RecordBehaviourNoteRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            "POSITIVE",
            new DateOnly(2026, 9, 1),
            "Deskripsi");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.RecorderRequired", result.Error.Code);
        Assert.Empty(repo.Items);
    }

    [Fact]
    public async Task RecordDisciplineIncidentHandler_WhenClassroomSanction_Succeeds()
    {
        var repo = new FakeDisciplineRecordRepository();
        var currentUser = new FakeCurrentUser(RecorderId, "guru.piket", RecorderName);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new RecordDisciplineIncidentHandler(repo, currentUser, dispatcher);

        var request = new RecordDisciplineIncidentRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 2),
            "SERAGAM",
            "Tidak menggunakan atribut seragam lengkap.",
            "WRITTEN_WARNING");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("WRITTEN_WARNING", result.Value.Sanction);
        Assert.Single(repo.Items);
        Assert.Empty(dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task RecordDisciplineIncidentHandler_WhenTeacherSanctionDisallowed_ReturnsFailure()
    {
        var repo = new FakeDisciplineRecordRepository();
        var currentUser = new FakeCurrentUser(RecorderId, "guru.piket", RecorderName);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new RecordDisciplineIncidentHandler(repo, currentUser, dispatcher);

        var request = new RecordDisciplineIncidentRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 2),
            "MEROKOK",
            "Merokok di belakang sekolah.",
            "SUSPENSION");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Dev.DisciplineRecord.SanctionNotAllowedForRole", result.Error.Code);
        Assert.Empty(repo.Items);
    }

    [Fact]
    public async Task RecordEscalatedSanctionHandler_WhenPrincipal_SucceedsAndDispatchesDomainEvent()
    {
        var repo = new FakeDisciplineRecordRepository();
        var principalId = Guid.NewGuid();
        var currentUser = new FakeCurrentUser(principalId, "kepala.sekolah", "Dr. H. Mulyadi, M.Pd.");
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new RecordEscalatedSanctionHandler(repo, currentUser, dispatcher);

        var request = new RecordEscalatedSanctionRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            new DateOnly(2026, 9, 5),
            "PERKELAHIAN_MASSAL",
            "Terlibat perkelahian massal antar sekolah.",
            "SUSPENSION");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("SUSPENSION", result.Value.Sanction);
        Assert.Equal(principalId, result.Value.RecorderId);
        Assert.Single(repo.Items);

        Assert.Single(dispatcher.DispatchedEvents);
        var sanctionEvent = Assert.IsType<SanctionImposedDomainEvent>(dispatcher.DispatchedEvents[0]);
        Assert.Equal(result.Value.Id, sanctionEvent.DisciplineRecordId);
        Assert.Equal(StudentId, sanctionEvent.StudentId);
        Assert.Equal("SUSPENSION", sanctionEvent.Sanction);
    }

    [Fact]
    public async Task RecordAchievementHandler_WhenValid_SavesAndReturnsDto()
    {
        var repo = new FakeAchievementRepository();
        var currentUser = new FakeCurrentUser(RecorderId, "guru.pembina", RecorderName);
        var dispatcher = new FakeDomainEventDispatcher();
        var handler = new RecordAchievementHandler(repo, currentUser, dispatcher);

        var request = new RecordAchievementRequest(
            StudentId,
            StudentName,
            StudentNis,
            AcademicYearId,
            AcademicYearName,
            "SCIENCE",
            "Olimpiade Sains Nasional",
            "NATIONAL",
            new DateOnly(2026, 9, 10),
            "Meraih Medali Perunggu OSN Fisika.");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("SCIENCE", result.Value.AchievementType);
        Assert.Equal("NATIONAL", result.Value.RecognitionLevel);
        Assert.Equal("Olimpiade Sains Nasional", result.Value.EventName);
        Assert.Single(repo.Items);
    }

    #endregion

    #region Dependency Injection Tests

    [Fact]
    public void AddDevModule_RegistersAllExpectedHandlersAndRepositories()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISqlConnectionFactory>(new FakeSqlConnectionFactory());
        services.AddDevModule();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IBehaviourNoteRepository>());
        Assert.NotNull(provider.GetService<IDisciplineRecordRepository>());
        Assert.NotNull(provider.GetService<IExtracurricularActivityRepository>());
        Assert.NotNull(provider.GetService<IAchievementRepository>());

        Assert.NotNull(services.FirstOrDefault(s => s.ServiceType == typeof(RecordBehaviourNoteHandler)));
        Assert.NotNull(services.FirstOrDefault(s => s.ServiceType == typeof(RecordDisciplineIncidentHandler)));
        Assert.NotNull(services.FirstOrDefault(s => s.ServiceType == typeof(RecordEscalatedSanctionHandler)));
        Assert.NotNull(services.FirstOrDefault(s => s.ServiceType == typeof(RecordAchievementHandler)));
        Assert.NotNull(services.FirstOrDefault(s => s.ServiceType == typeof(GetStudentDossierHandler)));
        Assert.NotNull(services.FirstOrDefault(s => s.ServiceType == typeof(GetDevelopmentOverviewHandler)));
    }

    #endregion

    #region Test Fakes

    private sealed class FakeSqlConnectionFactory : ISqlConnectionFactory
    {
        public IDbConnection CreateConnection() =>
            throw new NotImplementedException();

        public Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakeBehaviourNoteRepository : IBehaviourNoteRepository
    {
        public List<BehaviourNote> Items { get; } = [];

        public Task<BehaviourNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<BehaviourNote>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BehaviourNote>>(Items.Where(x => x.StudentId == studentId).ToList());

        public Task<IReadOnlyList<BehaviourNote>> GetAllAsync(Guid? studentId = null, Guid? academicYearId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BehaviourNote>>(Items.Where(x => (!studentId.HasValue || x.StudentId == studentId.Value) && (!academicYearId.HasValue || x.AcademicYearId == academicYearId.Value)).ToList());

        public Task InsertAsync(BehaviourNote note, CancellationToken cancellationToken = default)
        {
            Items.Add(note);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDisciplineRecordRepository : IDisciplineRecordRepository
    {
        public List<DisciplineRecord> Items { get; } = [];

        public Task<DisciplineRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<DisciplineRecord>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DisciplineRecord>>(Items.Where(x => x.StudentId == studentId).ToList());

        public Task<IReadOnlyList<DisciplineRecord>> GetAllAsync(Guid? studentId = null, Guid? academicYearId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DisciplineRecord>>(Items.Where(x => (!studentId.HasValue || x.StudentId == studentId.Value) && (!academicYearId.HasValue || x.AcademicYearId == academicYearId.Value)).ToList());

        public Task InsertAsync(DisciplineRecord record, CancellationToken cancellationToken = default)
        {
            Items.Add(record);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAchievementRepository : IAchievementRepository
    {
        public List<Achievement> Items { get; } = [];

        public Task<Achievement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Achievement>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Achievement>>(Items.Where(x => x.StudentId == studentId).ToList());

        public Task<IReadOnlyList<Achievement>> GetAllAsync(Guid? studentId = null, Guid? academicYearId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Achievement>>(Items.Where(x => (!studentId.HasValue || x.StudentId == studentId.Value) && (!academicYearId.HasValue || x.AcademicYearId == academicYearId.Value)).ToList());

        public Task InsertAsync(Achievement achievement, CancellationToken cancellationToken = default)
        {
            Items.Add(achievement);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCurrentUser(Guid userId, string username, string fullName) : ICurrentUser
    {
        public bool IsAuthenticated => userId != Guid.Empty;
        public Guid UserId => userId;
        public string Username => username;
        public string FullName => fullName;
        public IReadOnlyList<string> Roles => [];
        public IReadOnlyList<string> Permissions => [];
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? GuardianId => null;
    }

    private sealed class FakeDomainEventDispatcher : IDomainEventDispatcher
    {
        public List<DomainEventBase> DispatchedEvents { get; } = [];

        public Task DispatchAsync(IEnumerable<DomainEventBase> domainEvents, CancellationToken cancellationToken = default)
        {
            DispatchedEvents.AddRange(domainEvents);
            return Task.CompletedTask;
        }
    }

    #endregion
}
