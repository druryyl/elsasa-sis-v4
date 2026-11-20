using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Contracts.Student.Events;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.Modules.Fin.Features.Billing.CancelStudentCharge;
using ElsasaSis.Modules.Fin.Features.Billing.CreateAdHocCharge;
using ElsasaSis.Modules.Fin.Features.Billing.GenerateBatchTuitionCharges;
using ElsasaSis.Modules.Fin.Features.Billing.GetBatchBillingPreview;
using ElsasaSis.Modules.Fin.Features.Billing.GetStudentCharges;
using ElsasaSis.Modules.Fin.Subscribers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ElsasaSis.UnitTests.Finance;

public sealed class BillingFeatureTests
{
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private const string AcademicYearName = "2026/2027";
    private static readonly Guid StudyGroupId = Guid.NewGuid();
    private const string StudyGroupName = "VII-A";
    private static readonly Guid Student1Id = Guid.NewGuid();
    private const string Student1Name = "Ahmad Dahlan";
    private const string Student1Nis = "2026001";
    private static readonly Guid Student2Id = Guid.NewGuid();
    private const string Student2Name = "Siti Nurhaliza";
    private const string Student2Nis = "2026002";

    #region Validator Tests

    [Fact]
    public async Task BatchBillingPreviewValidator_WhenValid_Passes()
    {
        var validator = new BatchBillingPreviewValidator();
        var request = new BatchBillingPreviewRequest(AcademicYearId, "2026-07");

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("2026-7")]
    [InlineData("2026-13")]
    [InlineData("invalid")]
    [InlineData("")]
    public async Task BatchBillingPreviewValidator_WhenInvalidMonth_Fails(string billingMonth)
    {
        var validator = new BatchBillingPreviewValidator();
        var request = new BatchBillingPreviewRequest(AcademicYearId, billingMonth);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GenerateBatchTuitionChargesValidator_WhenValid_Passes()
    {
        var validator = new GenerateBatchTuitionChargesValidator();
        var request = new GenerateBatchTuitionChargesRequest(AcademicYearId, "2026-08");

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateAdHocChargeValidator_WhenValid_Passes()
    {
        var validator = new CreateAdHocChargeValidator();
        var request = new CreateAdHocChargeRequest(
            AcademicYearId,
            Student1Id,
            Guid.NewGuid(),
            "Seragam Batik Sekolah",
            150_000m,
            2);

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", 100, 1)]
    [InlineData("Valid Description", -10, 1)]
    [InlineData("Valid Description", 100, 0)]
    public async Task CreateAdHocChargeValidator_WhenInvalid_Fails(string description, decimal unitAmount, int quantity)
    {
        var validator = new CreateAdHocChargeValidator();
        var request = new CreateAdHocChargeRequest(
            AcademicYearId,
            Student1Id,
            Guid.NewGuid(),
            description,
            unitAmount,
            quantity);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CancelStudentChargeValidator_WhenValid_Passes()
    {
        var validator = new CancelStudentChargeValidator();
        var request = new CancelStudentChargeRequest("Salah entri tagihan.");

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CancelStudentChargeValidator_WhenEmptyReason_Fails()
    {
        var validator = new CancelStudentChargeValidator();
        var request = new CancelStudentChargeRequest("");

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    #endregion

    #region Batch Preview Tests

    [Fact]
    public async Task GetBatchBillingPreview_ComputesProspectiveSummaryAndItems()
    {
        var opr = new MockOperationalContract
        {
            Years = [new AcademicYearSummaryDto(AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "ACTIVE")]
        };
        var std = new MockStudentContract
        {
            ActiveStudents =
            [
                new StudentPlacementScopeDto(Student1Id, Student1Name, Student1Nis, StudyGroupId, StudyGroupName, "SMP", "7"),
                new StudentPlacementScopeDto(Student2Id, Student2Name, Student2Nis, StudyGroupId, StudyGroupName, "SMP", "7")
            ]
        };

        var chargeType = ChargeType.Create(AcademicYearId, AcademicYearName, "SPP Bulanan SMP", ChargeFrequencies.Monthly, 350_000m, "SMP", "7").Value!;
        chargeType.Activate();

        var discount = StudentDiscount.Create(AcademicYearId, AcademicYearName, chargeType.Id, chargeType.Name, Student2Id, Student2Name, Student2Nis, DiscountTypes.Percentage, 20m, "Sibling discount").Value!;

        var chargeRepo = new MockChargeTypeRepository { ChargeTypes = [chargeType] };
        var discountRepo = new MockStudentDiscountRepository { Discounts = [discount] };
        var studentChargeRepo = new MockStudentChargeRepository();

        var handler = new GetBatchBillingPreviewHandler(opr, std, chargeRepo, discountRepo, studentChargeRepo);
        var request = new BatchBillingPreviewRequest(AcademicYearId, "2026-07");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.StudentCount);
        Assert.Equal(2, result.Value.ChargeCount);
        Assert.Equal(0, result.Value.SkippedCount);
        Assert.Equal(700_000m, result.Value.TotalGrossAmount);
        Assert.Equal(70_000m, result.Value.TotalDiscountAmount); // 20% of 350_000 for student 2
        Assert.Equal(630_000m, result.Value.TotalNetAmount);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task GetBatchBillingPreview_WhenAlreadyBilled_SkipsInProspectiveTotals()
    {
        var opr = new MockOperationalContract
        {
            Years = [new AcademicYearSummaryDto(AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "ACTIVE")]
        };
        var std = new MockStudentContract
        {
            ActiveStudents =
            [
                new StudentPlacementScopeDto(Student1Id, Student1Name, Student1Nis, StudyGroupId, StudyGroupName, "SMP", "7")
            ]
        };

        var chargeType = ChargeType.Create(AcademicYearId, AcademicYearName, "SPP Bulanan SMP", ChargeFrequencies.Monthly, 350_000m, "SMP", "7").Value!;
        chargeType.Activate();

        var existingCharge = StudentCharge.Create(
            AcademicYearId, AcademicYearName, null, null, "2026-07",
            chargeType.Id, chargeType.Name, chargeType.Frequency,
            Student1Id, Student1Name, Student1Nis, "SMP", "7", unitAmount: 350_000m).Value!;

        var chargeRepo = new MockChargeTypeRepository { ChargeTypes = [chargeType] };
        var discountRepo = new MockStudentDiscountRepository();
        var studentChargeRepo = new MockStudentChargeRepository { Charges = [existingCharge] };

        var handler = new GetBatchBillingPreviewHandler(opr, std, chargeRepo, discountRepo, studentChargeRepo);
        var request = new BatchBillingPreviewRequest(AcademicYearId, "2026-07");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.ChargeCount);
        Assert.Equal(1, result.Value.SkippedCount);
        Assert.True(result.Value.Items[0].IsAlreadyBilled);
    }

    #endregion

    #region Generate Batch Charges Tests

    [Fact]
    public async Task GenerateBatchTuitionCharges_GeneratesChargesAndSkipsDuplicates()
    {
        var opr = new MockOperationalContract
        {
            Years = [new AcademicYearSummaryDto(AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "ACTIVE")]
        };
        var std = new MockStudentContract
        {
            ActiveStudents =
            [
                new StudentPlacementScopeDto(Student1Id, Student1Name, Student1Nis, StudyGroupId, StudyGroupName, "SMP", "7"),
                new StudentPlacementScopeDto(Student2Id, Student2Name, Student2Nis, StudyGroupId, StudyGroupName, "SMP", "7")
            ]
        };

        var chargeType = ChargeType.Create(AcademicYearId, AcademicYearName, "SPP Bulanan SMP", ChargeFrequencies.Monthly, 350_000m, "SMP", "7").Value!;
        chargeType.Activate();

        var existingCharge = StudentCharge.Create(
            AcademicYearId, AcademicYearName, null, null, "2026-07",
            chargeType.Id, chargeType.Name, chargeType.Frequency,
            Student1Id, Student1Name, Student1Nis, "SMP", "7", unitAmount: 350_000m).Value!;

        var chargeRepo = new MockChargeTypeRepository { ChargeTypes = [chargeType] };
        var discountRepo = new MockStudentDiscountRepository();
        var studentChargeRepo = new MockStudentChargeRepository { Charges = [existingCharge] };

        var handler = new GenerateBatchTuitionChargesHandler(opr, std, chargeRepo, discountRepo, studentChargeRepo);
        var request = new GenerateBatchTuitionChargesRequest(AcademicYearId, "2026-07");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.GeneratedCount);
        Assert.Equal(1, result.Value.SkippedCount);
        Assert.Equal(2, studentChargeRepo.Charges.Count);
    }

    #endregion

    #region Ad-Hoc Charge Tests

    [Fact]
    public async Task CreateAdHocCharge_WhenValid_InsertsStudentCharge()
    {
        var opr = new MockOperationalContract
        {
            Years = [new AcademicYearSummaryDto(AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "ACTIVE")]
        };
        var std = new MockStudentContract
        {
            ActiveStudents = [new StudentPlacementScopeDto(Student1Id, Student1Name, Student1Nis, StudyGroupId, StudyGroupName, "SMP", "7")]
        };

        var chargeType = ChargeType.Create(AcademicYearId, AcademicYearName, "Seragam Sekolah", ChargeFrequencies.OneTime, 250_000m).Value!;
        chargeType.Activate();

        var chargeRepo = new MockChargeTypeRepository { ChargeTypes = [chargeType] };
        var discountRepo = new MockStudentDiscountRepository();
        var studentChargeRepo = new MockStudentChargeRepository();

        var handler = new CreateAdHocChargeHandler(opr, std, chargeRepo, discountRepo, studentChargeRepo);
        var request = new CreateAdHocChargeRequest(
            AcademicYearId,
            Student1Id,
            chargeType.Id,
            "Pembelian Seragam Olahraga Tambahan",
            125_000m,
            2,
            new DateOnly(2026, 8, 1));

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(250_000m, result.Value.GrossAmount);
        Assert.Equal(250_000m, result.Value.NetAmount);
        Assert.Equal(ChargeStatuses.Unpaid, result.Value.Status);
        Assert.Single(studentChargeRepo.Charges);
    }

    [Fact]
    public async Task CreateAdHocCharge_WhenStudentNotFound_Fails()
    {
        var opr = new MockOperationalContract
        {
            Years = [new AcademicYearSummaryDto(AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "ACTIVE")]
        };
        var std = new MockStudentContract { ActiveStudents = [] };

        var chargeType = ChargeType.Create(AcademicYearId, AcademicYearName, "Seragam", ChargeFrequencies.OneTime, 200_000m).Value!;
        var chargeRepo = new MockChargeTypeRepository { ChargeTypes = [chargeType] };
        var discountRepo = new MockStudentDiscountRepository();
        var studentChargeRepo = new MockStudentChargeRepository();

        var handler = new CreateAdHocChargeHandler(opr, std, chargeRepo, discountRepo, studentChargeRepo);
        var request = new CreateAdHocChargeRequest(AcademicYearId, Guid.NewGuid(), chargeType.Id, "Seragam", 200_000m);

        var result = await handler.HandleAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("Fin.StudentNotFound", result.Error.Code);
    }

    #endregion

    #region Cancel Charge Tests

    [Fact]
    public async Task CancelStudentCharge_WhenUnpaid_CancelsSuccessfully()
    {
        var charge = StudentCharge.Create(
            AcademicYearId, AcademicYearName, null, null, "2026-07",
            Guid.NewGuid(), "SPP Bulanan SMP", ChargeFrequencies.Monthly,
            Student1Id, Student1Name, Student1Nis, "SMP", "7", unitAmount: 350_000m).Value!;

        var studentChargeRepo = new MockStudentChargeRepository { Charges = [charge] };
        var handler = new CancelStudentChargeHandler(studentChargeRepo);
        var request = new CancelStudentChargeRequest("Dibatalkan karena salah entri.");

        var result = await handler.HandleAsync(charge.Id, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChargeStatuses.Cancelled, result.Value!.Status);
        Assert.Equal("Dibatalkan karena salah entri.", result.Value.CancellationReason);
        Assert.NotNull(result.Value.CancelledAt);
    }

    [Fact]
    public async Task CancelStudentCharge_WhenPaymentsApplied_FailsWithConflict()
    {
        var charge = StudentCharge.Create(
            AcademicYearId, AcademicYearName, null, null, "2026-07",
            Guid.NewGuid(), "SPP Bulanan SMP", ChargeFrequencies.Monthly,
            Student1Id, Student1Name, Student1Nis, "SMP", "7", unitAmount: 350_000m).Value!;

        charge.ApplyPayment(100_000m);

        var studentChargeRepo = new MockStudentChargeRepository { Charges = [charge] };
        var handler = new CancelStudentChargeHandler(studentChargeRepo);
        var request = new CancelStudentChargeRequest("Mencoba membatalkan yang sudah dibayar.");

        var result = await handler.HandleAsync(charge.Id, request);

        Assert.False(result.IsSuccess);
        Assert.Equal("Fin.StudentCharge.CannotCancelPaidCharge", result.Error.Code);
    }

    #endregion

    #region Event Subscriber Tests

    [Fact]
    public async Task StudentPlacedEventHandler_InitializesTuitionCharge()
    {
        var opr = new MockOperationalContract
        {
            Years = [new AcademicYearSummaryDto(AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "ACTIVE")]
        };
        var std = new MockStudentContract
        {
            ActiveStudents = [new StudentPlacementScopeDto(Student1Id, Student1Name, Student1Nis, StudyGroupId, StudyGroupName, "SMP", "7")]
        };

        var chargeType = ChargeType.Create(AcademicYearId, AcademicYearName, "SPP Bulanan SMP", ChargeFrequencies.Monthly, 350_000m, "SMP", "7").Value!;
        chargeType.Activate();

        var chargeRepo = new MockChargeTypeRepository { ChargeTypes = [chargeType] };
        var discountRepo = new MockStudentDiscountRepository();
        var studentChargeRepo = new MockStudentChargeRepository();

        var subscriber = new StudentPlacedEventHandler(
            opr,
            std,
            chargeRepo,
            discountRepo,
            studentChargeRepo,
            NullLogger<StudentPlacedEventHandler>.Instance);

        var domainEvent = new StudentPlacedDomainEvent(Guid.NewGuid(), Student1Id, AcademicYearId, StudyGroupId);

        await subscriber.Handle(domainEvent, CancellationToken.None);

        Assert.Single(studentChargeRepo.Charges);
        var created = studentChargeRepo.Charges[0];
        Assert.Equal(Student1Id, created.StudentId);
        Assert.Equal(350_000m, created.UnitAmount);
        Assert.Equal(DateTime.UtcNow.ToString("yyyy-MM"), created.BillingMonth);
    }

    [Fact]
    public async Task CandidateEnrolledEventHandler_InitializesTuitionChargeWhenPlaced()
    {
        var opr = new MockOperationalContract
        {
            ActiveYear = new ActiveAcademicYearDto(AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30))
        };
        var std = new MockStudentContract
        {
            ActiveStudents = [new StudentPlacementScopeDto(Student1Id, Student1Name, Student1Nis, StudyGroupId, StudyGroupName, "SMP", "7")]
        };

        var chargeType = ChargeType.Create(AcademicYearId, AcademicYearName, "SPP Bulanan SMP", ChargeFrequencies.Monthly, 350_000m, "SMP", "7").Value!;
        chargeType.Activate();

        var chargeRepo = new MockChargeTypeRepository { ChargeTypes = [chargeType] };
        var discountRepo = new MockStudentDiscountRepository();
        var studentChargeRepo = new MockStudentChargeRepository();

        var subscriber = new CandidateEnrolledEventHandler(
            opr,
            std,
            chargeRepo,
            discountRepo,
            studentChargeRepo,
            NullLogger<CandidateEnrolledEventHandler>.Instance);

        var domainEvent = new CandidateEnrolledDomainEvent(Student1Id, Guid.NewGuid(), Student1Nis, Student1Name);

        await subscriber.Handle(domainEvent, CancellationToken.None);

        Assert.Single(studentChargeRepo.Charges);
    }

    #endregion

    #region Mock Classes

    private sealed class MockOperationalContract : IOperationalContract
    {
        public ActiveAcademicYearDto? ActiveYear { get; set; }
        public List<AcademicYearSummaryDto> Years { get; set; } = [];

        public Task<ActiveAcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ActiveYear ?? (Years.Count > 0 ? new ActiveAcademicYearDto(Years[0].Id, Years[0].Name, Years[0].StartDate, Years[0].EndDate) : null));

        public Task<ActiveOperationalPeriodDto?> GetActiveOperationalPeriodAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ActiveOperationalPeriodDto?>(null);

        public Task<AcademicYearSummaryDto?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Years.FirstOrDefault(y => y.Id == id));

        public Task<OperationalPeriodSummaryDto?> GetOperationalPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<OperationalPeriodSummaryDto?>(null);
    }

    private sealed class MockStudentContract : IStudentContract
    {
        public List<StudentPlacementScopeDto> ActiveStudents { get; set; } = [];

        public Task<StudyGroupSummaryDto?> GetStudyGroupByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<StudyGroupSummaryDto?>(null);

        public Task<IReadOnlyList<StudentPlacementScopeDto>> GetActiveStudentsByScopeAsync(
            Guid? studyGroupId,
            string? schoolLevel,
            string? grade,
            CancellationToken cancellationToken = default)
        {
            var query = ActiveStudents.AsEnumerable();
            if (studyGroupId.HasValue)
                query = query.Where(s => s.StudyGroupId == studyGroupId.Value);
            if (!string.IsNullOrEmpty(schoolLevel))
                query = query.Where(s => string.Equals(s.SchoolLevel, schoolLevel, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(grade))
                query = query.Where(s => string.Equals(s.Grade, grade, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult<IReadOnlyList<StudentPlacementScopeDto>>(query.ToList().AsReadOnly());
        }
    }

    private sealed class MockChargeTypeRepository : IChargeTypeRepository
    {
        public List<ChargeType> ChargeTypes { get; set; } = [];

        public Task<ChargeType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(ChargeTypes.FirstOrDefault(c => c.Id == id));

        public Task<IReadOnlyList<ChargeType>> GetByAcademicYearAsync(
            Guid? academicYearId = null,
            string? status = null,
            string? frequency = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChargeType>>(ChargeTypes.AsReadOnly());

        public Task<IReadOnlyList<ChargeType>> GetActiveByScopeAsync(
            Guid academicYearId,
            string? schoolLevel = null,
            string? grade = null,
            Guid? studyGroupId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChargeType>>(ChargeTypes.Where(c => c.Status == ChargeTypeStatuses.Active).ToList().AsReadOnly());

        public Task<bool> ExistsByNameInYearAsync(string name, Guid academicYearId, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task InsertAsync(ChargeType chargeType, CancellationToken cancellationToken = default)
        {
            ChargeTypes.Add(chargeType);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ChargeType chargeType, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MockStudentDiscountRepository : IStudentDiscountRepository
    {
        public List<StudentDiscount> Discounts { get; set; } = [];

        public Task<StudentDiscount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Discounts.FirstOrDefault(d => d.Id == id));

        public Task<IReadOnlyList<StudentDiscount>> GetByStudentIdAsync(Guid studentId, Guid academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentDiscount>>(Discounts.Where(d => d.StudentId == studentId && d.AcademicYearId == academicYearId).ToList().AsReadOnly());

        public Task<StudentDiscount?> GetActiveDiscountAsync(Guid studentId, Guid chargeTypeId, Guid academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Discounts.FirstOrDefault(d => d.StudentId == studentId && d.ChargeTypeId == chargeTypeId && d.AcademicYearId == academicYearId && d.IsActive));

        public Task<bool> ExistsForStudentAndChargeTypeAsync(Guid studentId, Guid chargeTypeId, Guid academicYearId, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<StudentDiscount>> GetByChargeTypeIdAsync(Guid chargeTypeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentDiscount>>(Discounts.AsReadOnly());

        public Task<IReadOnlyList<StudentDiscount>> GetDiscountsAsync(
            Guid? academicYearId = null,
            Guid? studentId = null,
            Guid? chargeTypeId = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentDiscount>>(Discounts.AsReadOnly());

        public Task InsertAsync(StudentDiscount discount, CancellationToken cancellationToken = default)
        {
            Discounts.Add(discount);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(StudentDiscount discount, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MockStudentChargeRepository : IStudentChargeRepository
    {
        public List<StudentCharge> Charges { get; set; } = [];

        public Task<StudentCharge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Charges.FirstOrDefault(c => c.Id == id));

        public Task<IReadOnlyList<StudentCharge>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentCharge>>(Charges.Where(c => ids.Contains(c.Id)).ToList().AsReadOnly());

        public Task<IReadOnlyList<StudentCharge>> GetByStudentIdAsync(Guid studentId, string? status = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentCharge>>(Charges.Where(c => c.StudentId == studentId && (status == null || c.Status == status)).ToList().AsReadOnly());

        public Task<IReadOnlyList<StudentCharge>> GetUnpaidOrPartiallyPaidByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentCharge>>(Charges.Where(c => c.StudentId == studentId && (c.Status == ChargeStatuses.Unpaid || c.Status == ChargeStatuses.PartiallyPaid)).ToList().AsReadOnly());

        public Task<IReadOnlyList<StudentCharge>> GetChargesAsync(
            Guid? academicYearId = null,
            string? billingMonth = null,
            string? schoolLevel = null,
            string? grade = null,
            Guid? studyGroupId = null,
            Guid? studentId = null,
            string? status = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentCharge>>(Charges.AsReadOnly());

        public Task<bool> ExistsMonthlyChargeAsync(Guid studentId, Guid chargeTypeId, string billingMonth, CancellationToken cancellationToken = default) =>
            Task.FromResult(Charges.Any(c => c.StudentId == studentId && c.ChargeTypeId == chargeTypeId && c.BillingMonth == billingMonth && c.Status != ChargeStatuses.Cancelled));

        public Task<bool> ExistsYearlyChargeAsync(Guid studentId, Guid chargeTypeId, Guid academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Charges.Any(c => c.StudentId == studentId && c.ChargeTypeId == chargeTypeId && c.AcademicYearId == academicYearId && c.ChargeFrequency == "YEARLY" && c.Status != ChargeStatuses.Cancelled));

        public Task<bool> ExistsSemesterChargeAsync(Guid studentId, Guid chargeTypeId, Guid operationalPeriodId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Charges.Any(c => c.StudentId == studentId && c.ChargeTypeId == chargeTypeId && c.OperationalPeriodId == operationalPeriodId && c.ChargeFrequency == "SEMESTER" && c.Status != ChargeStatuses.Cancelled));

        public Task InsertAsync(StudentCharge charge, CancellationToken cancellationToken = default)
        {
            Charges.Add(charge);
            return Task.CompletedTask;
        }

        public Task InsertBatchAsync(IEnumerable<StudentCharge> charges, CancellationToken cancellationToken = default)
        {
            Charges.AddRange(charges);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(StudentCharge charge, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    #endregion
}
