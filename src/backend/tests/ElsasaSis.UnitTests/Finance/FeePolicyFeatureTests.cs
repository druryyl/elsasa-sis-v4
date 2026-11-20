using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.Modules.Fin.Features.Discounts;
using ElsasaSis.Modules.Fin.Features.Discounts.CreateStudentDiscount;
using ElsasaSis.Modules.Fin.Features.Discounts.GetStudentDiscounts;
using ElsasaSis.Modules.Fin.Features.Discounts.RevokeStudentDiscount;
using ElsasaSis.Modules.Fin.Features.FeePolicy;
using ElsasaSis.Modules.Fin.Features.FeePolicy.ActivateChargeType;
using ElsasaSis.Modules.Fin.Features.FeePolicy.CreateChargeType;
using ElsasaSis.Modules.Fin.Features.FeePolicy.GetChargeTypes;
using ElsasaSis.Modules.Fin.Features.FeePolicy.UpdateChargeType;
using Xunit;

namespace ElsasaSis.UnitTests.Finance;

public sealed class FeePolicyFeatureTests
{
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private const string AcademicYearName = "2026/2027";
    private static readonly Guid StudyGroupId = Guid.NewGuid();
    private const string StudyGroupName = "VII-A";
    private static readonly Guid StudentId = Guid.NewGuid();
    private const string StudentName = "Farhan Pratama";
    private const string StudentNis = "2026001";

    #region Validator Tests

    [Fact]
    public async Task CreateChargeTypeValidator_WhenValid_Passes()
    {
        var validator = new CreateChargeTypeValidator();
        var request = new CreateChargeTypeRequest(AcademicYearId, AcademicYearName, "SPP Bulanan SMP", ChargeFrequencies.Monthly, 350_000m);

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "MONTHLY", 100)]
    [InlineData("SPP", "INVALID_FREQ", 100)]
    [InlineData("SPP", "MONTHLY", -1)]
    public async Task CreateChargeTypeValidator_WhenInvalid_Fails(string name, string frequency, decimal amount)
    {
        var validator = new CreateChargeTypeValidator();
        var request = new CreateChargeTypeRequest(AcademicYearId, AcademicYearName, name, frequency, amount);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpdateChargeTypeValidator_WhenValid_Passes()
    {
        var validator = new UpdateChargeTypeValidator();
        var request = new UpdateChargeTypeRequest("SPP Baru", ChargeFrequencies.Monthly, 400_000m);

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateStudentDiscountValidator_WhenValidPercentage_Passes()
    {
        var validator = new CreateStudentDiscountValidator();
        var request = new CreateStudentDiscountRequest(
            StudentId,
            Guid.NewGuid(),
            AcademicYearId,
            AcademicYearName,
            StudentName,
            StudentNis,
            DiscountTypes.Percentage,
            50m,
            "Beasiswa Prestasi");

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateStudentDiscountValidator_WhenPercentageExceeds100_Fails()
    {
        var validator = new CreateStudentDiscountValidator();
        var request = new CreateStudentDiscountRequest(
            StudentId,
            Guid.NewGuid(),
            AcademicYearId,
            AcademicYearName,
            StudentName,
            StudentNis,
            DiscountTypes.Percentage,
            120m,
            "Beasiswa Prestasi");

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateStudentDiscountValidator_WhenMissingRequiredFields_Fails()
    {
        var validator = new CreateStudentDiscountValidator();
        var request = new CreateStudentDiscountRequest(
            Guid.Empty,
            Guid.Empty,
            AcademicYearId,
            AcademicYearName,
            null,
            null,
            "INVALID_TYPE",
            -10m,
            "");

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    #endregion

    #region ChargeType Handler Tests

    [Fact]
    public async Task CreateChargeType_WithExplicitYearAndStudyGroup_CreatesDraft()
    {
        var repo = new MockChargeTypeRepository();
        var oprContract = new MockOperationalContract
        {
            Years = [new AcademicYearSummaryDto(AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "ACTIVE")]
        };
        var stdContract = new MockStudentContract
        {
            StudyGroup = new StudyGroupSummaryDto(StudyGroupId, AcademicYearId, AcademicYearName, "SMP", "7", StudyGroupName, null, null)
        };

        var handler = new CreateChargeTypeHandler(repo, oprContract, stdContract);
        var request = new CreateChargeTypeRequest(
            AcademicYearId,
            null,
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            350_000m,
            "SMP",
            "7",
            StudyGroupId);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("SPP Bulanan SMP", result.Value.Name);
        Assert.Equal(ChargeFrequencies.Monthly, result.Value.Frequency);
        Assert.Equal(350_000m, result.Value.Amount);
        Assert.Equal(ChargeTypeStatuses.Draft, result.Value.Status);
        Assert.Equal(StudyGroupName, result.Value.StudyGroupName);
        Assert.Equal(AcademicYearName, result.Value.AcademicYearName);
        Assert.NotNull(repo.InsertedChargeType);
    }

    [Fact]
    public async Task CreateChargeType_WithActiveYearFallback_CreatesDraft()
    {
        var repo = new MockChargeTypeRepository();
        var oprContract = new MockOperationalContract
        {
            ActiveYear = new ActiveAcademicYearDto(AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30))
        };
        var stdContract = new MockStudentContract();

        var handler = new CreateChargeTypeHandler(repo, oprContract, stdContract);
        var request = new CreateChargeTypeRequest(
            null,
            null,
            "Seragam SMP",
            ChargeFrequencies.OneTime,
            500_000m);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(AcademicYearId, result.Value!.AcademicYearId);
        Assert.Equal(AcademicYearName, result.Value.AcademicYearName);
    }

    [Fact]
    public async Task CreateChargeType_WhenDuplicateNameInYear_ReturnsConflict()
    {
        var repo = new MockChargeTypeRepository();
        repo.ExistingNamesInYear.Add(("SPP Bulanan SMP".ToLowerInvariant(), AcademicYearId));

        var oprContract = new MockOperationalContract
        {
            ActiveYear = new ActiveAcademicYearDto(AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30))
        };
        var stdContract = new MockStudentContract();

        var handler = new CreateChargeTypeHandler(repo, oprContract, stdContract);
        var request = new CreateChargeTypeRequest(
            AcademicYearId,
            AcademicYearName,
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            350_000m);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.ChargeType.DuplicateName", result.Error.Code);
    }

    [Fact]
    public async Task GetChargeTypes_ReturnsFilteredList()
    {
        var ct1 = ChargeType.Create(AcademicYearId, AcademicYearName, "SPP SMP", ChargeFrequencies.Monthly, 350_000m).Value!;
        var ct2 = ChargeType.Create(AcademicYearId, AcademicYearName, "Seragam", ChargeFrequencies.OneTime, 500_000m).Value!;
        ct2.Activate();

        var repo = new MockChargeTypeRepository
        {
            ChargeTypes = [ct1, ct2]
        };

        var handler = new GetChargeTypesHandler(repo);
        var query = new GetChargeTypesQuery(AcademicYearId, ChargeFrequencies.Monthly, ChargeTypeStatuses.Draft);

        var result = await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("SPP SMP", result.Value![0].Name);
    }

    [Fact]
    public async Task UpdateChargeType_WhenDraft_UpdatesSuccessfully()
    {
        var ct = ChargeType.Create(AcademicYearId, AcademicYearName, "SPP SMP", ChargeFrequencies.Monthly, 350_000m).Value!;
        var repo = new MockChargeTypeRepository
        {
            ChargeTypes = [ct]
        };
        var stdContract = new MockStudentContract();

        var handler = new UpdateChargeTypeHandler(repo, stdContract);
        var request = new UpdateChargeTypeRequest("SPP SMP Revisi", ChargeFrequencies.Monthly, 380_000m);

        var result = await handler.HandleAsync(ct.Id, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("SPP SMP Revisi", result.Value!.Name);
        Assert.Equal(380_000m, result.Value.Amount);
        Assert.NotNull(repo.UpdatedChargeType);
    }

    [Fact]
    public async Task UpdateChargeType_WhenActive_ReturnsConflict()
    {
        var ct = ChargeType.Create(AcademicYearId, AcademicYearName, "SPP SMP", ChargeFrequencies.Monthly, 350_000m).Value!;
        ct.Activate();
        var repo = new MockChargeTypeRepository
        {
            ChargeTypes = [ct]
        };
        var stdContract = new MockStudentContract();

        var handler = new UpdateChargeTypeHandler(repo, stdContract);
        var request = new UpdateChargeTypeRequest("SPP SMP Revisi", ChargeFrequencies.Monthly, 380_000m);

        var result = await handler.HandleAsync(ct.Id, request);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.ChargeType.CannotBeModified", result.Error.Code);
    }

    [Fact]
    public async Task UpdateChargeType_WhenNotFound_ReturnsNotFound()
    {
        var repo = new MockChargeTypeRepository();
        var stdContract = new MockStudentContract();

        var handler = new UpdateChargeTypeHandler(repo, stdContract);
        var request = new UpdateChargeTypeRequest("SPP SMP", ChargeFrequencies.Monthly, 350_000m);

        var result = await handler.HandleAsync(Guid.NewGuid(), request);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.ChargeType.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ActivateChargeType_TransitionsDraftToActive()
    {
        var ct = ChargeType.Create(AcademicYearId, AcademicYearName, "SPP SMP", ChargeFrequencies.Monthly, 350_000m).Value!;
        var repo = new MockChargeTypeRepository
        {
            ChargeTypes = [ct]
        };

        var handler = new ActivateChargeTypeHandler(repo);
        var result = await handler.HandleAsync(ct.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChargeTypeStatuses.Active, result.Value!.Status);
        Assert.NotNull(repo.UpdatedChargeType);
    }

    [Fact]
    public async Task ActivateChargeType_WhenNotFound_ReturnsNotFound()
    {
        var repo = new MockChargeTypeRepository();
        var handler = new ActivateChargeTypeHandler(repo);

        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.ChargeType.NotFound", result.Error.Code);
    }

    #endregion

    #region StudentDiscount Handler Tests

    [Fact]
    public async Task CreateStudentDiscount_WithStudentLookup_CreatesSuccessfully()
    {
        var ct = ChargeType.Create(AcademicYearId, AcademicYearName, "SPP Bulanan SMP", ChargeFrequencies.Monthly, 350_000m).Value!;
        var ctRepo = new MockChargeTypeRepository { ChargeTypes = [ct] };
        var discountRepo = new MockStudentDiscountRepository();
        var stdContract = new MockStudentContract
        {
            ActiveStudents = [new StudentPlacementScopeDto(StudentId, StudentName, StudentNis, StudyGroupId, StudyGroupName, "SMP", "7")]
        };
        var oprContract = new MockOperationalContract();

        var handler = new CreateStudentDiscountHandler(discountRepo, ctRepo, stdContract, oprContract);
        var request = new CreateStudentDiscountRequest(
            StudentId,
            ct.Id,
            null,
            null,
            null,
            null,
            DiscountTypes.FixedAmount,
            100_000m,
            "Keringanan Yatim");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(StudentName, result.Value.StudentName);
        Assert.Equal(StudentNis, result.Value.StudentNis);
        Assert.Equal(100_000m, result.Value.DiscountValue);
        Assert.Equal(DiscountTypes.FixedAmount, result.Value.DiscountType);
        Assert.True(result.Value.IsActive);
        Assert.NotNull(discountRepo.InsertedDiscount);
    }

    [Fact]
    public async Task CreateStudentDiscount_WhenDuplicate_ReturnsConflict()
    {
        var ct = ChargeType.Create(AcademicYearId, AcademicYearName, "SPP Bulanan SMP", ChargeFrequencies.Monthly, 350_000m).Value!;
        var ctRepo = new MockChargeTypeRepository { ChargeTypes = [ct] };
        var discountRepo = new MockStudentDiscountRepository();
        discountRepo.ExistingDiscounts.Add((StudentId, ct.Id, AcademicYearId));

        var stdContract = new MockStudentContract
        {
            ActiveStudents = [new StudentPlacementScopeDto(StudentId, StudentName, StudentNis, StudyGroupId, StudyGroupName, "SMP", "7")]
        };
        var oprContract = new MockOperationalContract();

        var handler = new CreateStudentDiscountHandler(discountRepo, ctRepo, stdContract, oprContract);
        var request = new CreateStudentDiscountRequest(
            StudentId,
            ct.Id,
            AcademicYearId,
            AcademicYearName,
            StudentName,
            StudentNis,
            DiscountTypes.Percentage,
            25m,
            "Diskon Saudara Kandung");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.StudentDiscount.AlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task CreateStudentDiscount_WhenChargeTypeNotFound_ReturnsNotFound()
    {
        var ctRepo = new MockChargeTypeRepository();
        var discountRepo = new MockStudentDiscountRepository();
        var stdContract = new MockStudentContract();
        var oprContract = new MockOperationalContract();

        var handler = new CreateStudentDiscountHandler(discountRepo, ctRepo, stdContract, oprContract);
        var request = new CreateStudentDiscountRequest(
            StudentId,
            Guid.NewGuid(),
            AcademicYearId,
            AcademicYearName,
            StudentName,
            StudentNis,
            DiscountTypes.Percentage,
            25m,
            "Diskon");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.ChargeType.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetStudentDiscounts_ReturnsFilteredList()
    {
        var d1 = StudentDiscount.Create(AcademicYearId, AcademicYearName, Guid.NewGuid(), "SPP", StudentId, StudentName, StudentNis, DiscountTypes.Percentage, 50m, "Prestasi").Value!;
        var d2 = StudentDiscount.Create(AcademicYearId, AcademicYearName, Guid.NewGuid(), "Gedung", StudentId, StudentName, StudentNis, DiscountTypes.FixedAmount, 500_000m, "Bantuan").Value!;
        d2.Revoke();

        var discountRepo = new MockStudentDiscountRepository
        {
            Discounts = [d1, d2]
        };

        var handler = new GetStudentDiscountsHandler(discountRepo);
        var query = new GetStudentDiscountsQuery(AcademicYearId, StudentId, null, true);

        var result = await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Prestasi", result.Value![0].Reason);
    }

    [Fact]
    public async Task RevokeStudentDiscount_WhenActive_RevokesSuccessfully()
    {
        var discount = StudentDiscount.Create(AcademicYearId, AcademicYearName, Guid.NewGuid(), "SPP", StudentId, StudentName, StudentNis, DiscountTypes.Percentage, 50m, "Prestasi").Value!;
        var discountRepo = new MockStudentDiscountRepository
        {
            Discounts = [discount]
        };

        var handler = new RevokeStudentDiscountHandler(discountRepo);
        var result = await handler.HandleAsync(discount.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsActive);
        Assert.NotNull(discountRepo.UpdatedDiscount);
    }

    [Fact]
    public async Task RevokeStudentDiscount_WhenAlreadyRevoked_ReturnsConflict()
    {
        var discount = StudentDiscount.Create(AcademicYearId, AcademicYearName, Guid.NewGuid(), "SPP", StudentId, StudentName, StudentNis, DiscountTypes.Percentage, 50m, "Prestasi").Value!;
        discount.Revoke();
        var discountRepo = new MockStudentDiscountRepository
        {
            Discounts = [discount]
        };

        var handler = new RevokeStudentDiscountHandler(discountRepo);
        var result = await handler.HandleAsync(discount.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.StudentDiscount.AlreadyRevoked", result.Error.Code);
    }

    [Fact]
    public async Task RevokeStudentDiscount_WhenNotFound_ReturnsNotFound()
    {
        var discountRepo = new MockStudentDiscountRepository();
        var handler = new RevokeStudentDiscountHandler(discountRepo);

        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.StudentDiscount.NotFound", result.Error.Code);
    }

    #endregion

    #region Mock Implementations

    private sealed class MockChargeTypeRepository : IChargeTypeRepository
    {
        public List<ChargeType> ChargeTypes { get; set; } = [];
        public List<(string Name, Guid AcademicYearId)> ExistingNamesInYear { get; set; } = [];
        public ChargeType? InsertedChargeType { get; private set; }
        public ChargeType? UpdatedChargeType { get; private set; }

        public Task<ChargeType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(ChargeTypes.FirstOrDefault(c => c.Id == id));

        public Task<IReadOnlyList<ChargeType>> GetByAcademicYearAsync(
            Guid? academicYearId = null,
            string? status = null,
            string? frequency = null,
            CancellationToken cancellationToken = default)
        {
            var query = ChargeTypes.AsEnumerable();
            if (academicYearId.HasValue)
                query = query.Where(c => c.AcademicYearId == academicYearId.Value);
            if (!string.IsNullOrEmpty(status))
                query = query.Where(c => string.Equals(c.Status, status, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(frequency))
                query = query.Where(c => string.Equals(c.Frequency, frequency, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult<IReadOnlyList<ChargeType>>(query.ToList().AsReadOnly());
        }

        public Task<IReadOnlyList<ChargeType>> GetActiveByScopeAsync(
            Guid academicYearId,
            string? schoolLevel = null,
            string? grade = null,
            Guid? studyGroupId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChargeType>>(ChargeTypes.Where(c => c.Status == ChargeTypeStatuses.Active).ToList().AsReadOnly());

        public Task<bool> ExistsByNameInYearAsync(string name, Guid academicYearId, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var normalized = name.Trim().ToLowerInvariant();
            var exists = ExistingNamesInYear.Any(e => e.Name == normalized && e.AcademicYearId == academicYearId)
                || ChargeTypes.Any(c => c.AcademicYearId == academicYearId && c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && (!excludeId.HasValue || c.Id != excludeId.Value));
            return Task.FromResult(exists);
        }

        public Task InsertAsync(ChargeType chargeType, CancellationToken cancellationToken = default)
        {
            InsertedChargeType = chargeType;
            ChargeTypes.Add(chargeType);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ChargeType chargeType, CancellationToken cancellationToken = default)
        {
            UpdatedChargeType = chargeType;
            return Task.CompletedTask;
        }
    }

    private sealed class MockStudentDiscountRepository : IStudentDiscountRepository
    {
        public List<StudentDiscount> Discounts { get; set; } = [];
        public List<(Guid StudentId, Guid ChargeTypeId, Guid AcademicYearId)> ExistingDiscounts { get; set; } = [];
        public StudentDiscount? InsertedDiscount { get; private set; }
        public StudentDiscount? UpdatedDiscount { get; private set; }

        public Task<StudentDiscount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Discounts.FirstOrDefault(d => d.Id == id));

        public Task<IReadOnlyList<StudentDiscount>> GetByStudentIdAsync(Guid studentId, Guid academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentDiscount>>(Discounts.Where(d => d.StudentId == studentId && d.AcademicYearId == academicYearId).ToList().AsReadOnly());

        public Task<StudentDiscount?> GetActiveDiscountAsync(Guid studentId, Guid chargeTypeId, Guid academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Discounts.FirstOrDefault(d => d.StudentId == studentId && d.ChargeTypeId == chargeTypeId && d.AcademicYearId == academicYearId && d.IsActive));

        public Task<bool> ExistsForStudentAndChargeTypeAsync(Guid studentId, Guid chargeTypeId, Guid academicYearId, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var exists = ExistingDiscounts.Any(e => e.StudentId == studentId && e.ChargeTypeId == chargeTypeId && e.AcademicYearId == academicYearId)
                || Discounts.Any(d => d.StudentId == studentId && d.ChargeTypeId == chargeTypeId && d.AcademicYearId == academicYearId && (!excludeId.HasValue || d.Id != excludeId.Value));
            return Task.FromResult(exists);
        }

        public Task<IReadOnlyList<StudentDiscount>> GetByChargeTypeIdAsync(Guid chargeTypeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentDiscount>>(Discounts.Where(d => d.ChargeTypeId == chargeTypeId).ToList().AsReadOnly());

        public Task<IReadOnlyList<StudentDiscount>> GetDiscountsAsync(
            Guid? academicYearId = null,
            Guid? studentId = null,
            Guid? chargeTypeId = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var query = Discounts.AsEnumerable();
            if (academicYearId.HasValue)
                query = query.Where(d => d.AcademicYearId == academicYearId.Value);
            if (studentId.HasValue)
                query = query.Where(d => d.StudentId == studentId.Value);
            if (chargeTypeId.HasValue)
                query = query.Where(d => d.ChargeTypeId == chargeTypeId.Value);
            if (isActive.HasValue)
                query = query.Where(d => d.IsActive == isActive.Value);

            return Task.FromResult<IReadOnlyList<StudentDiscount>>(query.ToList().AsReadOnly());
        }

        public Task InsertAsync(StudentDiscount discount, CancellationToken cancellationToken = default)
        {
            InsertedDiscount = discount;
            Discounts.Add(discount);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(StudentDiscount discount, CancellationToken cancellationToken = default)
        {
            UpdatedDiscount = discount;
            return Task.CompletedTask;
        }
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

    private sealed class MockStudentContract : IStudentContract
    {
        public StudyGroupSummaryDto? StudyGroup { get; set; }
        public List<StudentPlacementScopeDto> ActiveStudents { get; set; } = [];

        public Task<StudyGroupSummaryDto?> GetStudyGroupByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(StudyGroup?.Id == id ? StudyGroup : StudyGroup);

        public Task<IReadOnlyList<StudentPlacementScopeDto>> GetActiveStudentsByScopeAsync(
            Guid? studyGroupId,
            string? schoolLevel,
            string? grade,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentPlacementScopeDto>>(ActiveStudents.AsReadOnly());
    }

    #endregion
}
