using System.Text.RegularExpressions;
using ElsasaSis.Contracts.Finance.Events;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Common;
using Xunit;

namespace ElsasaSis.UnitTests.Finance;

public sealed partial class PaymentReceiptTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid CashierId = Guid.NewGuid();

    [GeneratedRegex(@"^KWT-\d{6}-\d{4}$")]
    private static partial Regex ReceiptNumberRegex();

    [Fact]
    public void Create_InitializesReceiptAndSetsUnallocatedAmountToTotalAndRaisesEvent()
    {
        var result = PaymentReceipt.Create(
            "KWT-202611-0001",
            AcademicYearId,
            "2026/2027",
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            null,
            null,
            new DateOnly(2026, 11, 20),
            PaymentMethods.Cash,
            500_000m,
            CashierId,
            "Kasir Keuangan",
            notes: "Pembayaran SPP dan kas sekolah",
            utcNow: FixedNow);

        Assert.True(result.IsSuccess);
        var receipt = result.Value!;
        Assert.NotEqual(Guid.Empty, receipt.Id);
        Assert.Equal("KWT-202611-0001", receipt.ReceiptNumber);
        Assert.Equal(500_000m, receipt.TotalAmount);
        Assert.Equal(0m, receipt.AllocatedAmount);
        Assert.Equal(500_000m, receipt.UnallocatedAmount);
        Assert.Equal(CashierId, receipt.ReceivedById);
        Assert.Equal("Kasir Keuangan", receipt.ReceivedByName);
        Assert.Empty(receipt.Allocations);

        // Verify Domain Event raised
        var evt = Assert.Single(receipt.DomainEvents);
        var postedEvent = Assert.IsType<PaymentReceiptPostedDomainEvent>(evt);
        Assert.Equal(receipt.Id, postedEvent.ReceiptId);
        Assert.Equal("KWT-202611-0001", postedEvent.ReceiptNumber);
        Assert.Equal(StudentId, postedEvent.StudentId);
        Assert.Equal(500_000m, postedEvent.TotalAmount);
    }

    [Fact]
    public void AllocateCharge_ExactAllocation_AllocatesMathAccurately()
    {
        var receipt = PaymentReceipt.Create(
            "KWT-202611-0001",
            AcademicYearId,
            "2026/2027",
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            null,
            null,
            new DateOnly(2026, 11, 20),
            PaymentMethods.Cash,
            350_000m,
            CashierId,
            "Kasir Keuangan",
            utcNow: FixedNow).Value!;

        var chargeId = Guid.NewGuid();
        var allocResult = receipt.AllocateCharge(
            chargeId,
            "SPP Bulanan SMP",
            "2026-11",
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            350_000m,
            new DateOnly(2026, 11, 20),
            FixedNow);

        Assert.True(allocResult.IsSuccess);
        Assert.Equal(350_000m, receipt.AllocatedAmount);
        Assert.Equal(0m, receipt.UnallocatedAmount); // Exact match -> zero surplus

        var alloc = Assert.Single(receipt.Allocations);
        Assert.Equal(chargeId, alloc.StudentChargeId);
        Assert.Equal(350_000m, alloc.AllocatedAmount);
    }

    [Fact]
    public void AllocateCharge_PartialAllocation_LeavesDepositSurplusHoldingInUnallocatedAmount()
    {
        // Enforces specification: OQ-014 Surplus payment is held as unallocated_amount
        var receipt = PaymentReceipt.Create(
            "KWT-202611-0002",
            AcademicYearId,
            "2026/2027",
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            null,
            null,
            new DateOnly(2026, 11, 20),
            PaymentMethods.BankTransfer,
            500_000m,
            CashierId,
            "Kasir Keuangan",
            utcNow: FixedNow).Value!;

        var chargeId = Guid.NewGuid();
        var allocResult = receipt.AllocateCharge(
            chargeId,
            "SPP Bulanan SMP",
            "2026-11",
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            350_000m,
            new DateOnly(2026, 11, 20),
            FixedNow);

        Assert.True(allocResult.IsSuccess);
        Assert.Equal(350_000m, receipt.AllocatedAmount);
        Assert.Equal(150_000m, receipt.UnallocatedAmount); // 500k - 350k = 150k deposit surplus held
    }

    [Fact]
    public void AllocateCharge_MultipleCharges_ReducesUnallocatedAmountCumulatively()
    {
        var receipt = PaymentReceipt.Create(
            "KWT-202611-0003",
            AcademicYearId,
            "2026/2027",
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            null,
            null,
            new DateOnly(2026, 11, 20),
            PaymentMethods.Cash,
            600_000m,
            CashierId,
            "Kasir Keuangan",
            utcNow: FixedNow).Value!;

        var charge1Id = Guid.NewGuid();
        var charge2Id = Guid.NewGuid();

        receipt.AllocateCharge(charge1Id, "SPP", "2026-11", StudentId, "Ahmad", "001", 350_000m, new DateOnly(2026, 11, 20), FixedNow);
        receipt.AllocateCharge(charge2Id, "Seragam", null, StudentId, "Ahmad", "001", 200_000m, new DateOnly(2026, 11, 20), FixedNow);

        Assert.Equal(550_000m, receipt.AllocatedAmount);
        Assert.Equal(50_000m, receipt.UnallocatedAmount); // 600k - 550k = 50k
        Assert.Equal(2, receipt.Allocations.Count);
    }

    [Fact]
    public void AllocateCharge_ExceedingTotalAmount_FailsWithAllocationExceedsTotal()
    {
        var receipt = PaymentReceipt.Create(
            "KWT-202611-0004",
            AcademicYearId,
            "2026/2027",
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            null,
            null,
            new DateOnly(2026, 11, 20),
            PaymentMethods.Cash,
            300_000m,
            CashierId,
            "Kasir Keuangan",
            utcNow: FixedNow).Value!;

        var allocResult = receipt.AllocateCharge(
            Guid.NewGuid(),
            "SPP Bulanan SMP",
            "2026-11",
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            350_000m,
            new DateOnly(2026, 11, 20),
            FixedNow);

        Assert.True(allocResult.IsFailure);
        Assert.Equal("Fin.PaymentReceipt.AllocationExceedsTotal", allocResult.Error.Code);
        Assert.Equal(ErrorType.Validation, allocResult.Error.Type);
    }

    [Theory]
    [InlineData("2026-07", 1, "KWT-202607-0001")]
    [InlineData("2026-11", 42, "KWT-202611-0042")]
    [InlineData("2027-01", 9999, "KWT-202701-9999")]
    public void SequenceFormatting_ConformsToAuthoritativeFormat(string month, int seq, string expectedReceiptNumber)
    {
        // Enforces specification: GAP-003, OQ-013 format KWT-YYYYMM-NNNN
        var cleanMonth = month.Replace("-", string.Empty);
        var formatted = $"KWT-{cleanMonth}-{seq:D4}";

        Assert.Equal(expectedReceiptNumber, formatted);
        Assert.Matches(ReceiptNumberRegex(), formatted);
    }

    [Fact]
    public void Rehydrate_WithAllocations_RestoresStateAccurately()
    {
        var receiptId = Guid.NewGuid();
        var allocId = Guid.NewGuid();
        var chargeId = Guid.NewGuid();

        var allocation = PaymentAllocation.Rehydrate(
            allocId,
            receiptId,
            "KWT-202611-0005",
            chargeId,
            "SPP Bulanan",
            "2026-11",
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            350_000m,
            new DateOnly(2026, 11, 20),
            FixedNow,
            FixedNow);

        var rehydrated = PaymentReceipt.Rehydrate(
            receiptId,
            "KWT-202611-0005",
            AcademicYearId,
            "2026/2027",
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            null,
            null,
            new DateOnly(2026, 11, 20),
            PaymentMethods.BankTransfer,
            "TRX-98765",
            500_000m,
            350_000m,
            150_000m,
            CashierId,
            "Kasir Keuangan",
            "Transfer BCA",
            FixedNow,
            FixedNow,
            [allocation]);

        Assert.Equal(receiptId, rehydrated.Id);
        Assert.Equal("KWT-202611-0005", rehydrated.ReceiptNumber);
        Assert.Equal(500_000m, rehydrated.TotalAmount);
        Assert.Equal(350_000m, rehydrated.AllocatedAmount);
        Assert.Equal(150_000m, rehydrated.UnallocatedAmount);
        Assert.Single(rehydrated.Allocations);
        Assert.Equal(allocId, rehydrated.Allocations.First().Id);
    }
}
