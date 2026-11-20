using System.Data;
using System.Reflection;
using ElsasaSis.Contracts.Academic.Events;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Acd;
using ElsasaSis.Modules.Acd.Domain.ReportCards;
using ElsasaSis.Modules.Acd.Features.ReportCards.FinalizeHomeroomReportCards;
using ElsasaSis.Modules.Acd.Features.ReportCards.GenerateDraftReportCards;
using ElsasaSis.Modules.Acd.Features.ReportCards.GetHomeroomReportCards;
using ElsasaSis.Modules.Acd.Features.ReportCards.GetReportCardById;
using ElsasaSis.Modules.Acd.Features.ReportCards.OverrideReportCardStatus;
using ElsasaSis.Modules.Acd.Features.ReportCards.ProvideSubjectReportCardNotes;
using ElsasaSis.Modules.Acd.Features.ReportCards.ReviewAllReportCards;
using ElsasaSis.Modules.Acd.Features.ReportCards.WriteHomeroomRemarks;
using ElsasaSis.SharedKernel.Domain;
using ElsasaSis.SharedKernel.Events;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;
using Microsoft.Extensions.DependencyInjection;

namespace ElsasaSis.UnitTests.Academic;

public sealed class ReportCardsFeatureTests
{
    private static readonly Guid StudyGroupId = Guid.NewGuid();
    private static readonly Guid OperationalPeriodId = Guid.NewGuid();
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();
    private static readonly Guid SubjectStageId = Guid.NewGuid();
    private static readonly Guid Student1Id = Guid.NewGuid();
    private static readonly Guid Student2Id = Guid.NewGuid();

    // 1. Validator Tests
    [Fact]
    public async Task GenerateDraftReportCardsValidator_WhenValid_Passes()
    {
        var validator = new GenerateDraftReportCardsValidator();
        var request = new GenerateDraftReportCardsRequest(StudyGroupId, OperationalPeriodId);
        var result = await validator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GenerateDraftReportCardsValidator_WhenEmpty_Fails()
    {
        var validator = new GenerateDraftReportCardsValidator();
        var request = new GenerateDraftReportCardsRequest(Guid.Empty, Guid.Empty);
        var result = await validator.ValidateAsync(request);
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task ProvideSubjectNotesValidator_WhenValid_Passes()
    {
        var validator = new ProvideSubjectNotesValidator();
        var request = new ProvideSubjectNotesRequest(SubjectStageId, "Siswa sangat aktif.");
        var result = await validator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ProvideSubjectNotesValidator_WhenEmpty_Fails()
    {
        var validator = new ProvideSubjectNotesValidator();
        var request = new ProvideSubjectNotesRequest(Guid.Empty, "");
        var result = await validator.ValidateAsync(request);
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task WriteHomeroomRemarksValidator_WhenValid_Passes()
    {
        var validator = new WriteHomeroomRemarksValidator();
        var request = new WriteHomeroomRemarksRequest("Catatan wali kelas.");
        var result = await validator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task WriteHomeroomRemarksValidator_WhenExceeds1000Chars_Fails()
    {
        var validator = new WriteHomeroomRemarksValidator();
        var request = new WriteHomeroomRemarksRequest(new string('x', 1001));
        var result = await validator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task OverrideReportCardStatusValidator_WhenValid_Passes()
    {
        var validator = new OverrideReportCardStatusValidator();
        var request = new OverrideReportCardStatusRequest("Koreksi nilai remedial matematika.");
        var result = await validator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task OverrideReportCardStatusValidator_WhenEmpty_Fails()
    {
        var validator = new OverrideReportCardStatusValidator();
        var request = new OverrideReportCardStatusRequest("");
        var result = await validator.ValidateAsync(request);
        Assert.False(result.IsValid);
    }

    // 2. GenerateDraftReportCardsHandler Tests
    [Fact]
    public async Task GenerateDraftReportCards_WhenPeriodNotFound_ReturnsNotFound()
    {
        var mockRepo = new MockReportCardRepository();
        var mockOpr = new MockOperationalContract { PeriodResult = null };
        var mockStd = new MockStudentContract();

        var handler = new GenerateDraftReportCardsHandler(mockRepo, mockOpr, mockStd);
        var result = await handler.HandleAsync(new GenerateDraftReportCardsRequest(StudyGroupId, OperationalPeriodId));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.ReportCards.OperationalPeriodNotFound", result.Error.Code);
    }

    [Fact]
    public async Task GenerateDraftReportCards_WhenStudyGroupNotFound_ReturnsNotFound()
    {
        var mockRepo = new MockReportCardRepository();
        var mockOpr = new MockOperationalContract
        {
            PeriodResult = new OperationalPeriodSummaryDto(
                OperationalPeriodId, AcademicYearId, "2026/2027", "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31), "ACTIVE")
        };
        var mockStd = new MockStudentContract { StudyGroupResult = null };

        var handler = new GenerateDraftReportCardsHandler(mockRepo, mockOpr, mockStd);
        var result = await handler.HandleAsync(new GenerateDraftReportCardsRequest(StudyGroupId, OperationalPeriodId));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.ReportCards.StudyGroupNotFound", result.Error.Code);
    }

    [Fact]
    public async Task GenerateDraftReportCards_WhenNoStudentsInGroup_ReturnsZeroCounts()
    {
        var mockRepo = new MockReportCardRepository();
        var mockOpr = new MockOperationalContract
        {
            PeriodResult = new OperationalPeriodSummaryDto(
                OperationalPeriodId, AcademicYearId, "2026/2027", "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31), "ACTIVE")
        };
        var mockStd = new MockStudentContract
        {
            StudyGroupResult = new StudyGroupSummaryDto(
                StudyGroupId, AcademicYearId, "2026/2027", "SMP", "7", "7A", TeacherId, "Budi"),
            StudentsResult = []
        };

        var handler = new GenerateDraftReportCardsHandler(mockRepo, mockOpr, mockStd);
        var result = await handler.HandleAsync(new GenerateDraftReportCardsRequest(StudyGroupId, OperationalPeriodId));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.GeneratedCount);
        Assert.Equal(0, result.Value!.SkippedCount);
    }

    [Fact]
    public async Task GenerateDraftReportCards_SkipsExistingCards_AndGeneratesRemaining()
    {
        var mockRepo = new MockReportCardRepository
        {
            ExistingStudentIds = new HashSet<Guid> { Student1Id }, // Student 1 already has a card
            DraftEntries =
            [
                new StudentDraftEntryData(
                    Student2Id, SubjectId, "Matematika", "MTK", SubjectStageId, "Matematika 7",
                    TeacherId, "Budi", 85.50m)
            ],
            NisnMap = new Dictionary<Guid, string?> { { Student2Id, "0012345678" } }
        };

        var mockOpr = new MockOperationalContract
        {
            PeriodResult = new OperationalPeriodSummaryDto(
                OperationalPeriodId, AcademicYearId, "2026/2027", "Semester 1",
                new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31), "ACTIVE")
        };
        var mockStd = new MockStudentContract
        {
            StudyGroupResult = new StudyGroupSummaryDto(
                StudyGroupId, AcademicYearId, "2026/2027", "SMP", "7", "7A", TeacherId, "Budi"),
            StudentsResult =
            [
                new StudentPlacementScopeDto(Student1Id, "Ahmad Dahlan", "NIS001", StudyGroupId, "7A", "SMP", "7"),
                new StudentPlacementScopeDto(Student2Id, "Siti Nurhaliza", "NIS002", StudyGroupId, "7A", "SMP", "7")
            ]
        };

        var handler = new GenerateDraftReportCardsHandler(mockRepo, mockOpr, mockStd);
        var result = await handler.HandleAsync(new GenerateDraftReportCardsRequest(StudyGroupId, OperationalPeriodId));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.GeneratedCount);
        Assert.Equal(1, result.Value!.SkippedCount);
        Assert.Single(mockRepo.BatchInsertedItems);

        var insertedCard = mockRepo.BatchInsertedItems[0].ReportCard;
        Assert.Equal(Student2Id, insertedCard.StudentId);
        Assert.Equal("Siti Nurhaliza", insertedCard.StudentName);
        Assert.Equal(ReportCardStatus.Draft, insertedCard.Status);
        Assert.Single(insertedCard.Entries);
        Assert.Equal(85.50m, insertedCard.Entries.First().NumericScore);
    }

    // 3. ProvideSubjectReportCardNotesHandler Tests
    [Fact]
    public async Task ProvideSubjectNotes_WhenCardNotFound_ReturnsNotFound()
    {
        var mockRepo = new MockReportCardRepository { CardResult = null };
        var mockUser = new MockCurrentUser { TeacherId = TeacherId };

        var handler = new ProvideSubjectReportCardNotesHandler(mockRepo, mockUser);
        var result = await handler.HandleAsync(Guid.NewGuid(), new ProvideSubjectNotesRequest(SubjectStageId, "Bagus."));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.ReportCards.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ProvideSubjectNotes_WhenCardFinalized_ReturnsConflict()
    {
        var card = CreateSampleReportCard(ReportCardStatus.Finalized);
        var mockRepo = new MockReportCardRepository { CardResult = card };
        var mockUser = new MockCurrentUser { TeacherId = TeacherId };

        var handler = new ProvideSubjectReportCardNotesHandler(mockRepo, mockUser);
        var result = await handler.HandleAsync(card.Id, new ProvideSubjectNotesRequest(SubjectStageId, "Bagus."));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.ReportCards.AlreadyFinalized", result.Error.Code);
    }

    [Fact]
    public async Task ProvideSubjectNotes_WhenTeacherNotAssigned_ReturnsConflict()
    {
        var card = CreateSampleReportCard(ReportCardStatus.Draft);
        var entry = ReportCardEntry.Create(
            card.Id, SubjectId, "Matematika", "MTK", SubjectStageId, "Matematika 7", 90.00m).Value!;

        var mockRepo = new MockReportCardRepository
        {
            CardResult = card,
            EntriesResult = [entry],
            HasTeacherAssignmentResult = false // Not assigned!
        };
        var mockUser = new MockCurrentUser { TeacherId = TeacherId };

        var handler = new ProvideSubjectReportCardNotesHandler(mockRepo, mockUser);
        var result = await handler.HandleAsync(card.Id, new ProvideSubjectNotesRequest(SubjectStageId, "Catatan"));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.ReportCards.TeacherNotAssignedToSubject", result.Error.Code);
    }

    [Fact]
    public async Task ProvideSubjectNotes_WhenAssignedTeacher_UpdatesNotes()
    {
        var card = CreateSampleReportCard(ReportCardStatus.Draft);
        var entry = ReportCardEntry.Create(
            card.Id, SubjectId, "Matematika", "MTK", SubjectStageId, "Matematika 7", 90.00m).Value!;

        var mockRepo = new MockReportCardRepository
        {
            CardResult = card,
            EntriesResult = [entry],
            HasTeacherAssignmentResult = true
        };
        var mockUser = new MockCurrentUser
        {
            TeacherId = TeacherId,
            FullName = "Budi Santoso, S.Pd."
        };

        var handler = new ProvideSubjectReportCardNotesHandler(mockRepo, mockUser);
        var result = await handler.HandleAsync(card.Id, new ProvideSubjectNotesRequest(SubjectStageId, "Sangat baik dalam aljabar."));

        Assert.True(result.IsSuccess);
        Assert.Equal("Sangat baik dalam aljabar.", entry.TeacherNotes);
        Assert.Equal(TeacherId, entry.TeacherId);
        Assert.Equal("Budi Santoso, S.Pd.", entry.TeacherName);
        Assert.NotNull(mockRepo.UpdatedEntry);
    }

    // 4. WriteHomeroomRemarksHandler Tests
    [Fact]
    public async Task WriteHomeroomRemarks_WhenNotAssignedHomeroomTeacher_ReturnsConflict()
    {
        var card = CreateSampleReportCard(ReportCardStatus.Draft);
        var mockRepo = new MockReportCardRepository { CardResult = card };
        var otherTeacherId = Guid.NewGuid();
        var mockUser = new MockCurrentUser
        {
            TeacherId = otherTeacherId,
            Roles = ["HOMEROOM_TEACHER"] // Not the assigned homeroom teacher!
        };

        var handler = new WriteHomeroomRemarksHandler(mockRepo, mockUser);
        var result = await handler.HandleAsync(card.Id, new WriteHomeroomRemarksRequest("Catatan"));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.ReportCards.NotAssignedHomeroomTeacher", result.Error.Code);
    }

    [Fact]
    public async Task WriteHomeroomRemarks_WhenAssignedHomeroomTeacher_UpdatesRemarks()
    {
        var card = CreateSampleReportCard(ReportCardStatus.Draft);
        var mockRepo = new MockReportCardRepository { CardResult = card };
        var mockUser = new MockCurrentUser
        {
            TeacherId = card.HomeroomTeacherId!.Value,
            Roles = ["HOMEROOM_TEACHER"]
        };

        var handler = new WriteHomeroomRemarksHandler(mockRepo, mockUser);
        var result = await handler.HandleAsync(card.Id, new WriteHomeroomRemarksRequest("Siswa rajin dan berprestasi."));

        Assert.True(result.IsSuccess);
        Assert.Equal("Siswa rajin dan berprestasi.", card.GeneralRemarks);
        Assert.NotNull(mockRepo.UpdatedCard);
    }

    // 5. FinalizeHomeroomReportCardsHandler Tests
    [Fact]
    public async Task FinalizeHomeroomReportCards_WhenAssignedHomeroom_TransitionsToFinalizedAndDispatchesEvent()
    {
        var card = CreateSampleReportCard(ReportCardStatus.Draft);
        var mockRepo = new MockReportCardRepository { CardResult = card };
        var mockDispatcher = new MockDomainEventDispatcher();
        var mockUser = new MockCurrentUser
        {
            TeacherId = card.HomeroomTeacherId!.Value,
            Roles = ["HOMEROOM_TEACHER"]
        };

        var handler = new FinalizeHomeroomReportCardsHandler(mockRepo, mockDispatcher, mockUser);
        var result = await handler.HandleAsync(card.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportCardStatus.Finalized, card.Status);
        Assert.NotNull(card.FinalizedAt);
        Assert.Single(mockDispatcher.DispatchedEvents);
        Assert.IsType<ReportCardFinalizedDomainEvent>(mockDispatcher.DispatchedEvents[0]);
    }

    // 6. OverrideReportCardStatusHandler Tests
    [Fact]
    public async Task OverrideReportCardStatus_WhenDraft_ReturnsConflict()
    {
        var card = CreateSampleReportCard(ReportCardStatus.Draft);
        var mockRepo = new MockReportCardRepository { CardResult = card };

        var handler = new OverrideReportCardStatusHandler(mockRepo);
        var result = await handler.HandleAsync(card.Id, new OverrideReportCardStatusRequest("Alasan"));

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.ReportCards.NotFinalized", result.Error.Code);
    }

    [Fact]
    public async Task OverrideReportCardStatus_WhenFinalized_UnlocksToDraft()
    {
        var card = CreateSampleReportCard(ReportCardStatus.Finalized);
        var mockRepo = new MockReportCardRepository { CardResult = card };

        var handler = new OverrideReportCardStatusHandler(mockRepo);
        var result = await handler.HandleAsync(card.Id, new OverrideReportCardStatusRequest("Revisi nilai ujian akhir"));

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportCardStatus.Draft, card.Status);
        Assert.Null(card.FinalizedAt);
        Assert.Equal("Revisi nilai ujian akhir", card.OverrideReason);
        Assert.NotNull(mockRepo.UpdatedCard);
    }

    // 7. Architectural CQRS Discipline Test
    [Fact]
    public void WriteHandlers_InReportCards_DoNotDependOnISqlConnectionFactoryOrDapper()
    {
        var writeHandlerTypes = new[]
        {
            typeof(GenerateDraftReportCardsHandler),
            typeof(ProvideSubjectReportCardNotesHandler),
            typeof(WriteHomeroomRemarksHandler),
            typeof(FinalizeHomeroomReportCardsHandler),
            typeof(OverrideReportCardStatusHandler)
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

    // 8. Dependency Injection Verification
    [Fact]
    public void AddAcdModule_RegistersAllReportCardsHandlers()
    {
        var services = new ServiceCollection();
        services.AddScoped<ISqlConnectionFactory, MockSqlConnectionFactory>();
        services.AddScoped<ICurrentUser, MockCurrentUser>();
        services.AddScoped<IDomainEventDispatcher, MockDomainEventDispatcher>();
        services.AddScoped<IOperationalContract, MockOperationalContract>();
        services.AddScoped<IStudentContract, MockStudentContract>();

        services.AddAcdModule();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IReportCardRepository>());
        Assert.NotNull(provider.GetService<GenerateDraftReportCardsHandler>());
        Assert.NotNull(provider.GetService<ProvideSubjectReportCardNotesHandler>());
        Assert.NotNull(provider.GetService<WriteHomeroomRemarksHandler>());
        Assert.NotNull(provider.GetService<FinalizeHomeroomReportCardsHandler>());
        Assert.NotNull(provider.GetService<OverrideReportCardStatusHandler>());
        Assert.NotNull(provider.GetService<GetHomeroomReportCardsHandler>());
        Assert.NotNull(provider.GetService<GetReportCardsOverviewHandler>());
        Assert.NotNull(provider.GetService<GetReportCardByIdHandler>());
    }

    // Helper Factory
    private static ReportCard CreateSampleReportCard(string status)
    {
        var card = ReportCard.CreateDraft(
            Student1Id,
            "Ahmad Dahlan",
            "NIS001",
            "0012345678",
            AcademicYearId,
            "2026/2027",
            OperationalPeriodId,
            "Semester 1",
            StudyGroupId,
            "7A",
            TeacherId,
            "Budi Santoso",
            Guid.NewGuid(),
            DateTime.UtcNow).Value!;

        if (status == ReportCardStatus.Finalized)
        {
            card.Finalize(DateTime.UtcNow);
            card.ClearDomainEvents();
        }

        return card;
    }

    // Mocks
    private sealed class MockReportCardRepository : IReportCardRepository
    {
        public ReportCard? CardResult { get; set; }
        public IReadOnlyList<ReportCardEntry> EntriesResult { get; set; } = [];
        public IReadOnlySet<Guid> ExistingStudentIds { get; set; } = new HashSet<Guid>();
        public IReadOnlyList<StudentDraftEntryData> DraftEntries { get; set; } = [];
        public IReadOnlyDictionary<Guid, string?> NisnMap { get; set; } = new Dictionary<Guid, string?>();
        public bool HasTeacherAssignmentResult { get; set; } = true;

        public List<(ReportCard ReportCard, IEnumerable<ReportCardEntry> Entries)> BatchInsertedItems { get; } = [];
        public ReportCard? UpdatedCard { get; private set; }
        public ReportCardEntry? UpdatedEntry { get; private set; }

        public Task<ReportCard?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(CardResult);

        public Task<ReportCard?> GetByStudentAndPeriodAsync(Guid studentId, Guid periodId, CancellationToken ct = default) =>
            Task.FromResult(CardResult);

        public Task<IReadOnlyList<ReportCardEntry>> GetEntriesByReportCardIdAsync(Guid reportCardId, CancellationToken ct = default) =>
            Task.FromResult(EntriesResult);

        public Task InsertAsync(ReportCard reportCard, IEnumerable<ReportCardEntry> entries, CancellationToken ct = default)
        {
            BatchInsertedItems.Add((reportCard, entries));
            return Task.CompletedTask;
        }

        public Task InsertBatchAsync(IEnumerable<(ReportCard ReportCard, IEnumerable<ReportCardEntry> Entries)> items, CancellationToken ct = default)
        {
            BatchInsertedItems.AddRange(items);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ReportCard reportCard, CancellationToken ct = default)
        {
            UpdatedCard = reportCard;
            return Task.CompletedTask;
        }

        public Task UpdateEntryNotesAsync(ReportCardEntry entry, CancellationToken ct = default)
        {
            UpdatedEntry = entry;
            return Task.CompletedTask;
        }

        public Task<IReadOnlySet<Guid>> GetExistingCardStudentIdsAsync(Guid operationalPeriodId, IEnumerable<Guid> studentIds, CancellationToken ct = default) =>
            Task.FromResult(ExistingStudentIds);

        public Task<IReadOnlyDictionary<Guid, string?>> GetStudentNisnsAsync(IEnumerable<Guid> studentIds, CancellationToken ct = default) =>
            Task.FromResult(NisnMap);

        public Task<IReadOnlyList<StudentDraftEntryData>> ComputeDraftCardEntriesAsync(Guid studyGroupId, Guid operationalPeriodId, string schoolLevel, string grade, IEnumerable<Guid> studentIds, CancellationToken ct = default) =>
            Task.FromResult(DraftEntries);

        public Task<bool> HasTeacherAssignmentAsync(Guid teacherId, Guid studyGroupId, Guid subjectId, Guid operationalPeriodId, CancellationToken ct = default) =>
            Task.FromResult(HasTeacherAssignmentResult);
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
        public IReadOnlyList<StudentPlacementScopeDto> StudentsResult { get; set; } = [];

        public Task<StudyGroupSummaryDto?> GetStudyGroupByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(StudyGroupResult);

        public Task<IReadOnlyList<StudentPlacementScopeDto>> GetActiveStudentsByScopeAsync(Guid? studyGroupId, string? schoolLevel, string? grade, CancellationToken cancellationToken = default) =>
            Task.FromResult(StudentsResult);
    }

    private sealed class MockCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated { get; set; } = true;
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = "teacher.user";
        public string FullName { get; set; } = "Teacher User";
        public IReadOnlyList<string> Roles { get; set; } = ["TEACHER"];
        public IReadOnlyList<string> Permissions { get; set; } = [];
        public Guid? TeacherId { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? GuardianId { get; set; }
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

    private sealed class MockSqlConnectionFactory : ISqlConnectionFactory
    {
        public IDbConnection CreateConnection() => throw new NotImplementedException();
        public Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
