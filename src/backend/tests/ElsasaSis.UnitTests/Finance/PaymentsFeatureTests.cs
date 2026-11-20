using ElsasaSis.Contracts.Finance.Events;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.Modules.Fin.Features.Payments;
using ElsasaSis.Modules.Fin.Features.Payments.AllocateAdvanceDeposit;
using ElsasaSis.Modules.Fin.Features.Payments.RecordPaymentReceipt;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;
using ElsasaSis.SharedKernel.Events;
using ElsasaSis.SharedKernel.Security;
using Xunit;

namespace ElsasaSis.UnitTests.Finance;

public sealed class PaymentsFeatureTests
{
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private const string AcademicYearName = "2026/2027";
    private static readonly Guid StudyGroupId = Guid.NewGuid();
    private const string StudyGroupName = "VII-A";
    private static readonly Guid StudentId = Guid.NewGuid();
    private const string StudentName = "Ahmad Dahlan";
    private const string StudentNis = "2026001";
    private static readonly Guid CashierId = Guid.NewGuid();
    private const string CashierName = "Kasir Keuangan";

    #region Validator Tests

    [Fact]
    public async Task RecordPaymentReceiptValidator_WhenValid_Passes()
    {
        var validator = new RecordPaymentReceiptValidator();
        var request = new RecordPaymentReceiptRequest(
            StudentId,
            AcademicYearId,
            new DateOnly(2026, 7, 10),
            PaymentMethods.Cash,
            350_000m,
            [new PaymentAllocationItemRequest(Guid.NewGuid(), 350_000m)]);

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("BITCOIN")]
    [InlineData("CRYPTO")]
    public async Task RecordPaymentReceiptValidator_WhenInvalidPaymentMethod_Fails(string method)
    {
        var validator = new RecordPaymentReceiptValidator();
        var request = new RecordPaymentReceiptRequest(
            StudentId,
            AcademicYearId,
            new DateOnly(2026, 7, 10),
            method,
            350_000m);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PaymentMethod");
    }

    [Fact]
    public async Task RecordPaymentReceiptValidator_WhenTotalAmountNonPositive_Fails()
    {
        var validator = new RecordPaymentReceiptValidator();
        var request = new RecordPaymentReceiptRequest(
            StudentId,
            AcademicYearId,
            new DateOnly(2026, 7, 10),
            PaymentMethods.Cash,
            0m);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TotalAmount");
    }

    [Fact]
    public async Task RecordPaymentReceiptValidator_WhenAllocationExceedsTotal_Fails()
    {
        var validator = new RecordPaymentReceiptValidator();
        var request = new RecordPaymentReceiptRequest(
            StudentId,
            AcademicYearId,
            new DateOnly(2026, 7, 10),
            PaymentMethods.Cash,
            300_000m,
            [new PaymentAllocationItemRequest(Guid.NewGuid(), 350_000m)]);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Allocations");
    }

    [Fact]
    public async Task AllocateAdvanceDepositValidator_WhenValid_Passes()
    {
        var validator = new AllocateAdvanceDepositValidator();
        var request = new AllocateAdvanceDepositRequest(
            StudentId,
            Guid.NewGuid(),
            [new PaymentAllocationItemRequest(Guid.NewGuid(), 100_000m)]);

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task AllocateAdvanceDepositValidator_WhenAllocationsEmpty_Fails()
    {
        var validator = new AllocateAdvanceDepositValidator();
        var request = new AllocateAdvanceDepositRequest(
            StudentId,
            Guid.NewGuid(),
            []);

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Allocations");
    }

    #endregion

    #region RecordPaymentReceiptHandler Tests

    [Fact]
    public async Task RecordPaymentReceipt_WhenExactSinglePayment_MarksChargePaidAndCreatesReceipt()
    {
        var chargeRepo = new MockStudentChargeRepository();
        var receiptRepo = new MockPaymentReceiptRepository();
        var seqGen = new MockReceiptSequenceGenerator("KWT-202607-0001");
        var oprContract = new MockOperationalContract();
        var stdContract = new MockStudentContract();
        var dispatcher = new MockDomainEventDispatcher();
        var user = new MockCurrentUser(CashierId, "kasir1", CashierName);

        var charge = CreateSampleCharge(StudentId, 350_000m);
        chargeRepo.Charges.Add(charge);

        var handler = new RecordPaymentReceiptHandler(
            receiptRepo,
            seqGen,
            chargeRepo,
            oprContract,
            stdContract,
            user,
            dispatcher);

        var request = new RecordPaymentReceiptRequest(
            StudentId,
            AcademicYearId,
            new DateOnly(2026, 7, 10),
            PaymentMethods.Cash,
            350_000m,
            [new PaymentAllocationItemRequest(charge.Id, 350_000m)],
            AcademicYearName: AcademicYearName,
            StudentName: StudentName,
            StudentNis: StudentNis);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        var receipt = result.Value!;
        Assert.Equal("KWT-202607-0001", receipt.ReceiptNumber);
        Assert.Equal(350_000m, receipt.TotalAmount);
        Assert.Equal(350_000m, receipt.AllocatedAmount);
        Assert.Equal(0m, receipt.UnallocatedAmount);
        Assert.Single(receipt.Allocations);
        Assert.Equal(charge.Id, receipt.Allocations[0].StudentChargeId);
        Assert.Equal(350_000m, receipt.Allocations[0].AllocatedAmount);

        Assert.Equal(ChargeStatuses.Paid, charge.Status);
        Assert.Equal(350_000m, charge.PaidAmount);
        Assert.Equal(0m, charge.OutstandingAmount);

        Assert.Single(receiptRepo.SavedReceipts);
        Assert.Single(dispatcher.DispatchedEvents);
        Assert.IsType<PaymentReceiptPostedDomainEvent>(dispatcher.DispatchedEvents[0]);
    }

    [Fact]
    public async Task RecordPaymentReceipt_WhenMultiChargeWithPartial_AllocatesCorrectly()
    {
        var chargeRepo = new MockStudentChargeRepository();
        var receiptRepo = new MockPaymentReceiptRepository();
        var seqGen = new MockReceiptSequenceGenerator("KWT-202607-0002");
        var oprContract = new MockOperationalContract();
        var stdContract = new MockStudentContract();
        var dispatcher = new MockDomainEventDispatcher();
        var user = new MockCurrentUser(CashierId, "kasir1", CashierName);

        var charge1 = CreateSampleCharge(StudentId, 350_000m, "SPP Juli");
        var charge2 = CreateSampleCharge(StudentId, 250_000m, "Seragam");
        chargeRepo.Charges.Add(charge1);
        chargeRepo.Charges.Add(charge2);

        var handler = new RecordPaymentReceiptHandler(
            receiptRepo,
            seqGen,
            chargeRepo,
            oprContract,
            stdContract,
            user,
            dispatcher);

        // Pay charge1 full (350k) and charge2 partial (100k) from total 450k
        var request = new RecordPaymentReceiptRequest(
            StudentId,
            AcademicYearId,
            new DateOnly(2026, 7, 10),
            PaymentMethods.BankTransfer,
            450_000m,
            [
                new PaymentAllocationItemRequest(charge1.Id, 350_000m),
                new PaymentAllocationItemRequest(charge2.Id, 100_000m)
            ],
            BankReference: "TRX-123456",
            AcademicYearName: AcademicYearName,
            StudentName: StudentName,
            StudentNis: StudentNis);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        var receipt = result.Value!;
        Assert.Equal(450_000m, receipt.TotalAmount);
        Assert.Equal(450_000m, receipt.AllocatedAmount);
        Assert.Equal(0m, receipt.UnallocatedAmount);
        Assert.Equal(2, receipt.Allocations.Count);

        Assert.Equal(ChargeStatuses.Paid, charge1.Status);
        Assert.Equal(350_000m, charge1.PaidAmount);
        Assert.Equal(0m, charge1.OutstandingAmount);

        Assert.Equal(ChargeStatuses.PartiallyPaid, charge2.Status);
        Assert.Equal(100_000m, charge2.PaidAmount);
        Assert.Equal(150_000m, charge2.OutstandingAmount);
    }

    [Fact]
    public async Task RecordPaymentReceipt_WhenExcessPayment_HoldsSurplusAsAdvanceDeposit()
    {
        var chargeRepo = new MockStudentChargeRepository();
        var receiptRepo = new MockPaymentReceiptRepository();
        var seqGen = new MockReceiptSequenceGenerator("KWT-202607-0003");
        var oprContract = new MockOperationalContract();
        var stdContract = new MockStudentContract();
        var dispatcher = new MockDomainEventDispatcher();
        var user = new MockCurrentUser(CashierId, "kasir1", CashierName);

        var charge = CreateSampleCharge(StudentId, 350_000m);
        chargeRepo.Charges.Add(charge);

        var handler = new RecordPaymentReceiptHandler(
            receiptRepo,
            seqGen,
            chargeRepo,
            oprContract,
            stdContract,
            user,
            dispatcher);

        // Pay 500k total against 350k charge => 150k surplus deposit
        var request = new RecordPaymentReceiptRequest(
            StudentId,
            AcademicYearId,
            new DateOnly(2026, 7, 10),
            PaymentMethods.Cash,
            500_000m,
            [new PaymentAllocationItemRequest(charge.Id, 350_000m)],
            AcademicYearName: AcademicYearName,
            StudentName: StudentName,
            StudentNis: StudentNis);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        var receipt = result.Value!;
        Assert.Equal(500_000m, receipt.TotalAmount);
        Assert.Equal(350_000m, receipt.AllocatedAmount);
        Assert.Equal(150_000m, receipt.UnallocatedAmount);
        Assert.Single(receipt.Allocations);
    }

    [Fact]
    public async Task RecordPaymentReceipt_WhenPureAdvanceDeposit_HoldsFullAmountAsDeposit()
    {
        var chargeRepo = new MockStudentChargeRepository();
        var receiptRepo = new MockPaymentReceiptRepository();
        var seqGen = new MockReceiptSequenceGenerator("KWT-202607-0004");
        var oprContract = new MockOperationalContract();
        var stdContract = new MockStudentContract();
        var dispatcher = new MockDomainEventDispatcher();
        var user = new MockCurrentUser(CashierId, "kasir1", CashierName);

        var handler = new RecordPaymentReceiptHandler(
            receiptRepo,
            seqGen,
            chargeRepo,
            oprContract,
            stdContract,
            user,
            dispatcher);

        var request = new RecordPaymentReceiptRequest(
            StudentId,
            AcademicYearId,
            new DateOnly(2026, 7, 10),
            PaymentMethods.BankTransfer,
            1_000_000m,
            Allocations: [],
            BankReference: "TRX-DEPOSIT",
            AcademicYearName: AcademicYearName,
            StudentName: StudentName,
            StudentNis: StudentNis);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        var receipt = result.Value!;
        Assert.Equal(1_000_000m, receipt.TotalAmount);
        Assert.Equal(0m, receipt.AllocatedAmount);
        Assert.Equal(1_000_000m, receipt.UnallocatedAmount);
        Assert.Empty(receipt.Allocations);
    }

    [Fact]
    public async Task RecordPaymentReceipt_WhenChargeNotFound_Fails()
    {
        var chargeRepo = new MockStudentChargeRepository();
        var receiptRepo = new MockPaymentReceiptRepository();
        var seqGen = new MockReceiptSequenceGenerator("KWT-202607-0005");
        var oprContract = new MockOperationalContract();
        var stdContract = new MockStudentContract();
        var dispatcher = new MockDomainEventDispatcher();
        var user = new MockCurrentUser(CashierId, "kasir1", CashierName);

        var handler = new RecordPaymentReceiptHandler(
            receiptRepo,
            seqGen,
            chargeRepo,
            oprContract,
            stdContract,
            user,
            dispatcher);

        var nonExistentChargeId = Guid.NewGuid();
        var request = new RecordPaymentReceiptRequest(
            StudentId,
            AcademicYearId,
            new DateOnly(2026, 7, 10),
            PaymentMethods.Cash,
            350_000m,
            [new PaymentAllocationItemRequest(nonExistentChargeId, 350_000m)],
            AcademicYearName: AcademicYearName,
            StudentName: StudentName,
            StudentNis: StudentNis);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.StudentCharge.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task RecordPaymentReceipt_WhenChargeBelongsToOtherStudent_Fails()
    {
        var chargeRepo = new MockStudentChargeRepository();
        var receiptRepo = new MockPaymentReceiptRepository();
        var seqGen = new MockReceiptSequenceGenerator("KWT-202607-0006");
        var oprContract = new MockOperationalContract();
        var stdContract = new MockStudentContract();
        var dispatcher = new MockDomainEventDispatcher();
        var user = new MockCurrentUser(CashierId, "kasir1", CashierName);

        var otherStudentId = Guid.NewGuid();
        var charge = CreateSampleCharge(otherStudentId, 350_000m);
        chargeRepo.Charges.Add(charge);

        var handler = new RecordPaymentReceiptHandler(
            receiptRepo,
            seqGen,
            chargeRepo,
            oprContract,
            stdContract,
            user,
            dispatcher);

        var request = new RecordPaymentReceiptRequest(
            StudentId,
            AcademicYearId,
            new DateOnly(2026, 7, 10),
            PaymentMethods.Cash,
            350_000m,
            [new PaymentAllocationItemRequest(charge.Id, 350_000m)],
            AcademicYearName: AcademicYearName,
            StudentName: StudentName,
            StudentNis: StudentNis);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.StudentCharge.StudentMismatch", result.Error.Code);
    }

    [Fact]
    public async Task RecordPaymentReceipt_WhenChargeCancelled_Fails()
    {
        var chargeRepo = new MockStudentChargeRepository();
        var receiptRepo = new MockPaymentReceiptRepository();
        var seqGen = new MockReceiptSequenceGenerator("KWT-202607-0007");
        var oprContract = new MockOperationalContract();
        var stdContract = new MockStudentContract();
        var dispatcher = new MockDomainEventDispatcher();
        var user = new MockCurrentUser(CashierId, "kasir1", CashierName);

        var charge = CreateSampleCharge(StudentId, 350_000m);
        charge.Cancel("Dibatalkan karena salah input", DateTime.UtcNow);
        chargeRepo.Charges.Add(charge);

        var handler = new RecordPaymentReceiptHandler(
            receiptRepo,
            seqGen,
            chargeRepo,
            oprContract,
            stdContract,
            user,
            dispatcher);

        var request = new RecordPaymentReceiptRequest(
            StudentId,
            AcademicYearId,
            new DateOnly(2026, 7, 10),
            PaymentMethods.Cash,
            350_000m,
            [new PaymentAllocationItemRequest(charge.Id, 350_000m)],
            AcademicYearName: AcademicYearName,
            StudentName: StudentName,
            StudentNis: StudentNis);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.StudentCharge.AlreadyCancelled", result.Error.Code);
    }

    [Fact]
    public async Task RecordPaymentReceipt_WhenAllocationExceedsChargeOutstanding_Fails()
    {
        var chargeRepo = new MockStudentChargeRepository();
        var receiptRepo = new MockPaymentReceiptRepository();
        var seqGen = new MockReceiptSequenceGenerator("KWT-202607-0008");
        var oprContract = new MockOperationalContract();
        var stdContract = new MockStudentContract();
        var dispatcher = new MockDomainEventDispatcher();
        var user = new MockCurrentUser(CashierId, "kasir1", CashierName);

        var charge = CreateSampleCharge(StudentId, 200_000m);
        chargeRepo.Charges.Add(charge);

        var handler = new RecordPaymentReceiptHandler(
            receiptRepo,
            seqGen,
            chargeRepo,
            oprContract,
            stdContract,
            user,
            dispatcher);

        // Charge is 200k, allocating 250k from 250k total
        var request = new RecordPaymentReceiptRequest(
            StudentId,
            AcademicYearId,
            new DateOnly(2026, 7, 10),
            PaymentMethods.Cash,
            250_000m,
            [new PaymentAllocationItemRequest(charge.Id, 250_000m)],
            AcademicYearName: AcademicYearName,
            StudentName: StudentName,
            StudentNis: StudentNis);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.StudentCharge.PaymentExceedsOutstanding", result.Error.Code);
    }

    #endregion

    #region AllocateAdvanceDepositHandler Tests

    [Fact]
    public async Task AllocateAdvanceDeposit_WhenValid_AppliesDepositToOpenCharge()
    {
        var chargeRepo = new MockStudentChargeRepository();
        var receiptRepo = new MockPaymentReceiptRepository();

        var charge = CreateSampleCharge(StudentId, 350_000m);
        chargeRepo.Charges.Add(charge);

        // Prior receipt holding 500k deposit (0 allocated, 500k unallocated)
        var sourceReceipt = PaymentReceipt.Create(
            "KWT-202607-0010",
            AcademicYearId,
            AcademicYearName,
            StudentId,
            StudentName,
            StudentNis,
            StudyGroupId,
            StudyGroupName,
            new DateOnly(2026, 7, 1),
            PaymentMethods.Cash,
            500_000m,
            CashierId,
            CashierName).Value!;
        receiptRepo.Receipts.Add(sourceReceipt);

        var handler = new AllocateAdvanceDepositHandler(receiptRepo, chargeRepo);

        var request = new AllocateAdvanceDepositRequest(
            StudentId,
            sourceReceipt.Id,
            [new PaymentAllocationItemRequest(charge.Id, 350_000m)],
            new DateOnly(2026, 7, 15));

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        var updatedReceipt = result.Value!;
        Assert.Equal(350_000m, updatedReceipt.AllocatedAmount);
        Assert.Equal(150_000m, updatedReceipt.UnallocatedAmount);
        Assert.Single(updatedReceipt.Allocations);
        Assert.Equal(charge.Id, updatedReceipt.Allocations[0].StudentChargeId);

        Assert.Equal(ChargeStatuses.Paid, charge.Status);
        Assert.Equal(350_000m, charge.PaidAmount);
        Assert.Equal(0m, charge.OutstandingAmount);

        Assert.Single(receiptRepo.DepositAllocatedReceipts);
    }

    [Fact]
    public async Task AllocateAdvanceDeposit_WhenSourceReceiptNotFound_Fails()
    {
        var chargeRepo = new MockStudentChargeRepository();
        var receiptRepo = new MockPaymentReceiptRepository();
        var handler = new AllocateAdvanceDepositHandler(receiptRepo, chargeRepo);

        var request = new AllocateAdvanceDepositRequest(
            StudentId,
            Guid.NewGuid(),
            [new PaymentAllocationItemRequest(Guid.NewGuid(), 100_000m)]);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.PaymentReceipt.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task AllocateAdvanceDeposit_WhenStudentMismatch_Fails()
    {
        var chargeRepo = new MockStudentChargeRepository();
        var receiptRepo = new MockPaymentReceiptRepository();

        var otherStudentId = Guid.NewGuid();
        var sourceReceipt = PaymentReceipt.Create(
            "KWT-202607-0011",
            AcademicYearId,
            AcademicYearName,
            otherStudentId,
            "Other Student",
            "9999",
            null,
            null,
            new DateOnly(2026, 7, 1),
            PaymentMethods.Cash,
            500_000m,
            CashierId,
            CashierName).Value!;
        receiptRepo.Receipts.Add(sourceReceipt);

        var handler = new AllocateAdvanceDepositHandler(receiptRepo, chargeRepo);

        var request = new AllocateAdvanceDepositRequest(
            StudentId, // Mismatch with otherStudentId
            sourceReceipt.Id,
            [new PaymentAllocationItemRequest(Guid.NewGuid(), 100_000m)]);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.Deposit.StudentMismatch", result.Error.Code);
    }

    [Fact]
    public async Task AllocateAdvanceDeposit_WhenInsufficientDepositBalance_Fails()
    {
        var chargeRepo = new MockStudentChargeRepository();
        var receiptRepo = new MockPaymentReceiptRepository();

        var charge = CreateSampleCharge(StudentId, 350_000m);
        chargeRepo.Charges.Add(charge);

        // Prior receipt holding only 200k deposit
        var sourceReceipt = PaymentReceipt.Create(
            "KWT-202607-0012",
            AcademicYearId,
            AcademicYearName,
            StudentId,
            StudentName,
            StudentNis,
            null,
            null,
            new DateOnly(2026, 7, 1),
            PaymentMethods.Cash,
            200_000m,
            CashierId,
            CashierName).Value!;
        receiptRepo.Receipts.Add(sourceReceipt);

        var handler = new AllocateAdvanceDepositHandler(receiptRepo, chargeRepo);

        // Requested 350k > available 200k
        var request = new AllocateAdvanceDepositRequest(
            StudentId,
            sourceReceipt.Id,
            [new PaymentAllocationItemRequest(charge.Id, 350_000m)]);

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.Deposit.InsufficientBalance", result.Error.Code);
    }

    #endregion

    #region Helper Methods & Test Doubles

    private static StudentCharge CreateSampleCharge(
        Guid studentId,
        decimal amount,
        string description = "SPP Bulanan SMP") =>
        StudentCharge.Create(
            AcademicYearId,
            AcademicYearName,
            null,
            null,
            "2026-07",
            Guid.NewGuid(),
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            studentId,
            StudentName,
            StudentNis,
            "SMP",
            "7",
            StudyGroupId,
            StudyGroupName,
            description,
            unitAmount: amount,
            quantity: 1,
            dueDate: new DateOnly(2026, 7, 10)).Value!;

    private sealed class MockStudentChargeRepository : IStudentChargeRepository
    {
        public List<StudentCharge> Charges { get; set; } = [];

        public Task<StudentCharge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Charges.FirstOrDefault(c => c.Id == id));

        public Task<IReadOnlyList<StudentCharge>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idSet = ids.ToHashSet();
            return Task.FromResult<IReadOnlyList<StudentCharge>>(Charges.Where(c => idSet.Contains(c.Id)).ToList().AsReadOnly());
        }

        public Task<IReadOnlyList<StudentCharge>> GetByStudentIdAsync(Guid studentId, string? status = null, CancellationToken cancellationToken = default)
        {
            var query = Charges.Where(c => c.StudentId == studentId);
            if (!string.IsNullOrEmpty(status))
                query = query.Where(c => c.Status == status);
            return Task.FromResult<IReadOnlyList<StudentCharge>>(query.ToList().AsReadOnly());
        }

        public Task<IReadOnlyList<StudentCharge>> GetUnpaidOrPartiallyPaidByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentCharge>>(
                Charges.Where(c => c.StudentId == studentId && (c.Status == ChargeStatuses.Unpaid || c.Status == ChargeStatuses.PartiallyPaid)).ToList().AsReadOnly());

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
            Task.FromResult(false);

        public Task<bool> ExistsYearlyChargeAsync(Guid studentId, Guid chargeTypeId, Guid academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> ExistsSemesterChargeAsync(Guid studentId, Guid chargeTypeId, Guid operationalPeriodId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

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

        public Task UpdateAsync(StudentCharge charge, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MockPaymentReceiptRepository : IPaymentReceiptRepository
    {
        public List<PaymentReceipt> Receipts { get; set; } = [];
        public List<PaymentReceipt> SavedReceipts { get; set; } = [];
        public List<PaymentReceipt> DepositAllocatedReceipts { get; set; } = [];

        public Task<PaymentReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Receipts.FirstOrDefault(r => r.Id == id));

        public Task<PaymentReceipt?> GetByReceiptNumberAsync(string receiptNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(Receipts.FirstOrDefault(r => r.ReceiptNumber == receiptNumber));

        public Task<IReadOnlyList<PaymentReceipt>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PaymentReceipt>>(Receipts.Where(r => r.StudentId == studentId).ToList().AsReadOnly());

        public Task<IReadOnlyList<PaymentReceipt>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PaymentReceipt>>(Receipts.Where(r => r.PaymentDate >= startDate && r.PaymentDate <= endDate).ToList().AsReadOnly());

        public Task InsertAsync(PaymentReceipt receipt, CancellationToken cancellationToken = default)
        {
            Receipts.Add(receipt);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PaymentReceipt receipt, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SavePaymentReceiptTransactionAsync(
            PaymentReceipt receipt,
            IReadOnlyList<StudentCharge> updatedCharges,
            CancellationToken cancellationToken = default)
        {
            Receipts.Add(receipt);
            SavedReceipts.Add(receipt);
            return Task.CompletedTask;
        }

        public Task SaveDepositAllocationTransactionAsync(
            PaymentReceipt sourceReceipt,
            IReadOnlyList<StudentCharge> updatedCharges,
            CancellationToken cancellationToken = default)
        {
            DepositAllocatedReceipts.Add(sourceReceipt);
            return Task.CompletedTask;
        }
    }

    private sealed class MockReceiptSequenceGenerator(string sequenceToReturn) : IReceiptSequenceGenerator
    {
        public Task<string> GenerateNextReceiptNumberAsync(string month, CancellationToken cancellationToken = default) =>
            Task.FromResult(sequenceToReturn);
    }

    private sealed class MockOperationalContract : IOperationalContract
    {
        public Task<ActiveAcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ActiveAcademicYearDto?>(new ActiveAcademicYearDto(AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30)));

        public Task<ActiveOperationalPeriodDto?> GetActiveOperationalPeriodAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ActiveOperationalPeriodDto?>(null);

        public Task<AcademicYearSummaryDto?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<AcademicYearSummaryDto?>(new AcademicYearSummaryDto(AcademicYearId, AcademicYearName, new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "ACTIVE"));

        public Task<OperationalPeriodSummaryDto?> GetOperationalPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<OperationalPeriodSummaryDto?>(null);
    }

    private sealed class MockStudentContract : IStudentContract
    {
        public Task<StudyGroupSummaryDto?> GetStudyGroupByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<StudyGroupSummaryDto?>(null);

        public Task<IReadOnlyList<StudentPlacementScopeDto>> GetActiveStudentsByScopeAsync(
            Guid? studyGroupId,
            string? schoolLevel,
            string? grade,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentPlacementScopeDto>>([
                new StudentPlacementScopeDto(StudentId, StudentName, StudentNis, StudyGroupId, StudyGroupName, "SMP", "7")
            ]);
    }

    private sealed class MockCurrentUser(Guid userId, string username, string fullName) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId { get; } = userId;
        public string Username { get; } = username;
        public string FullName { get; } = fullName;
        public IReadOnlyList<string> Roles => ["FINANCE_OFFICER"];
        public IReadOnlyList<string> Permissions => [ElsasaSis.Modules.Fin.Permissions.Finance.Payments.Record, ElsasaSis.Modules.Fin.Permissions.Finance.Deposits.Allocate];
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? GuardianId => null;
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

    #endregion
}
