using ElsasaSis.Contracts.Finance.Events;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using Xunit;

namespace ElsasaSis.UnitTests.Finance;

public sealed class FinanceDomainTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid ChargeTypeId = Guid.NewGuid();

    #region ChargeType Tests

    [Fact]
    public void ChargeType_Create_WithValidParameters_ReturnsDraft()
    {
        var result = ChargeType.Create(
            AcademicYearId,
            "2026/2027",
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            350_000m,
            "SMP",
            "7",
            null,
            null,
            FixedNow);

        Assert.True(result.IsSuccess);
        var chargeType = result.Value!;
        Assert.NotNull(chargeType);
        Assert.Equal("SPP Bulanan SMP", chargeType.Name);
        Assert.Equal(ChargeFrequencies.Monthly, chargeType.Frequency);
        Assert.Equal(350_000m, chargeType.Amount);
        Assert.Equal(ChargeTypeStatuses.Draft, chargeType.Status);
        Assert.Equal(FixedNow, chargeType.CreatedAt);
        Assert.Equal(FixedNow, chargeType.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ChargeType_Create_WithEmptyName_Fails(string invalidName)
    {
        var result = ChargeType.Create(
            AcademicYearId,
            "2026/2027",
            invalidName,
            ChargeFrequencies.Monthly,
            350_000m);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.ChargeType.InvalidName", result.Error.Code);
    }

    [Fact]
    public void ChargeType_Create_WithInvalidFrequency_Fails()
    {
        var result = ChargeType.Create(
            AcademicYearId,
            "2026/2027",
            "SPP Bulanan SMP",
            "WEEKLY",
            350_000m);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.ChargeType.InvalidFrequency", result.Error.Code);
    }

    [Fact]
    public void ChargeType_Create_WithNegativeAmount_Fails()
    {
        var result = ChargeType.Create(
            AcademicYearId,
            "2026/2027",
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            -100m);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.ChargeType.InvalidAmount", result.Error.Code);
    }

    [Fact]
    public void ChargeType_Activate_TransitionsStatusToActive()
    {
        var chargeType = ChargeType.Create(
            AcademicYearId,
            "2026/2027",
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            350_000m,
            utcNow: FixedNow).Value!;

        var activateResult = chargeType.Activate(FixedNow.AddHours(1));

        Assert.True(activateResult.IsSuccess);
        Assert.Equal(ChargeTypeStatuses.Active, chargeType.Status);
        Assert.Equal(FixedNow.AddHours(1), chargeType.UpdatedAt);
    }

    [Fact]
    public void ChargeType_Update_WhenDraft_Succeeds()
    {
        var chargeType = ChargeType.Create(
            AcademicYearId,
            "2026/2027",
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            350_000m,
            utcNow: FixedNow).Value!;

        var updateResult = chargeType.Update(
            "SPP Bulanan SMP Revisi",
            ChargeFrequencies.Monthly,
            375_000m,
            "SMP",
            "7",
            null,
            null,
            FixedNow.AddHours(2));

        Assert.True(updateResult.IsSuccess);
        Assert.Equal("SPP Bulanan SMP Revisi", chargeType.Name);
        Assert.Equal(375_000m, chargeType.Amount);
        Assert.Equal(FixedNow.AddHours(2), chargeType.UpdatedAt);
    }

    [Fact]
    public void ChargeType_Update_WhenActive_FailsWithConflict()
    {
        var chargeType = ChargeType.Create(
            AcademicYearId,
            "2026/2027",
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            350_000m,
            utcNow: FixedNow).Value!;

        chargeType.Activate(FixedNow.AddHours(1));

        var updateResult = chargeType.Update(
            "SPP Bulanan SMP Revisi",
            ChargeFrequencies.Monthly,
            400_000m,
            utcNow: FixedNow.AddHours(2));

        Assert.True(updateResult.IsFailure);
        Assert.Equal("Fin.ChargeType.CannotBeModified", updateResult.Error.Code);
    }

    [Fact]
    public void ChargeType_Rehydrate_RestoresExactState()
    {
        var id = Guid.NewGuid();
        var rehydrated = ChargeType.Rehydrate(
            id,
            AcademicYearId,
            "2026/2027",
            "Uang Gedung",
            ChargeFrequencies.OneTime,
            1_500_000m,
            "SMA",
            "10",
            null,
            null,
            ChargeTypeStatuses.Active,
            FixedNow,
            FixedNow.AddDays(1));

        Assert.Equal(id, rehydrated.Id);
        Assert.Equal("Uang Gedung", rehydrated.Name);
        Assert.Equal(ChargeFrequencies.OneTime, rehydrated.Frequency);
        Assert.Equal(1_500_000m, rehydrated.Amount);
        Assert.Equal(ChargeTypeStatuses.Active, rehydrated.Status);
    }

    #endregion

    #region StudentDiscount Tests

    [Fact]
    public void StudentDiscount_Create_PercentageDiscount_Succeeds()
    {
        var result = StudentDiscount.Create(
            AcademicYearId,
            "2026/2027",
            ChargeTypeId,
            "SPP Bulanan SMP",
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            DiscountTypes.Percentage,
            25m,
            "Diskon Saudara Kandung 25%",
            FixedNow);

        Assert.True(result.IsSuccess);
        var discount = result.Value!;
        Assert.Equal(DiscountTypes.Percentage, discount.DiscountType);
        Assert.Equal(25m, discount.DiscountValue);
        Assert.True(discount.IsActive);
        Assert.Equal(FixedNow, discount.CreatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(101)]
    public void StudentDiscount_Create_InvalidPercentage_Fails(decimal invalidPercent)
    {
        var result = StudentDiscount.Create(
            AcademicYearId,
            "2026/2027",
            ChargeTypeId,
            "SPP Bulanan SMP",
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            DiscountTypes.Percentage,
            invalidPercent,
            "Diskon Saudara");

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.StudentDiscount.InvalidValue", result.Error.Code);
    }

    [Fact]
    public void StudentDiscount_ComputeDiscount_PercentageAndFixedAmountCalculations()
    {
        var percentDiscount = StudentDiscount.Create(
            AcademicYearId,
            "2026/2027",
            ChargeTypeId,
            "SPP Bulanan SMP",
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            DiscountTypes.Percentage,
            20m,
            "Diskon 20%").Value!;

        var fixedDiscount = StudentDiscount.Create(
            AcademicYearId,
            "2026/2027",
            ChargeTypeId,
            "SPP Bulanan SMP",
            StudentId,
            "Siti Nurhaliza",
            "202607002",
            DiscountTypes.FixedAmount,
            100_000m,
            "Beasiswa").Value!;

        // Gross 350,000 * 20% = 70,000
        Assert.Equal(70_000m, percentDiscount.ComputeDiscount(350_000m));
        // Gross 350,000 - Fixed 100,000 discount = 100,000
        Assert.Equal(100_000m, fixedDiscount.ComputeDiscount(350_000m));
        // Fixed discount greater than gross is capped at gross
        Assert.Equal(50_000m, fixedDiscount.ComputeDiscount(50_000m));
    }

    [Fact]
    public void StudentDiscount_Revoke_DeactivatesDiscount()
    {
        var discount = StudentDiscount.Create(
            AcademicYearId,
            "2026/2027",
            ChargeTypeId,
            "SPP Bulanan SMP",
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            DiscountTypes.FixedAmount,
            50_000m,
            "Bantuan",
            FixedNow).Value!;

        var revokeResult = discount.Revoke(FixedNow.AddDays(1));
        Assert.True(revokeResult.IsSuccess);
        Assert.False(discount.IsActive);
        Assert.Equal(FixedNow.AddDays(1), discount.UpdatedAt);

        // Deactivated discount computes 0
        Assert.Equal(0m, discount.ComputeDiscount(300_000m));

        // Revoking again fails
        var secondRevoke = discount.Revoke();
        Assert.True(secondRevoke.IsFailure);
        Assert.Equal("Fin.StudentDiscount.AlreadyRevoked", secondRevoke.Error.Code);
    }

    [Fact]
    public void StudentDiscount_Rehydrate_RestoresState()
    {
        var id = Guid.NewGuid();
        var rehydrated = StudentDiscount.Rehydrate(
            id,
            AcademicYearId,
            "2026/2027",
            ChargeTypeId,
            "SPP Bulanan",
            StudentId,
            "Ahmad",
            "2026001",
            DiscountTypes.FixedAmount,
            75_000m,
            "Beasiswa Tahfidz",
            true,
            FixedNow,
            FixedNow);

        Assert.Equal(id, rehydrated.Id);
        Assert.Equal(75_000m, rehydrated.DiscountValue);
        Assert.Equal("Beasiswa Tahfidz", rehydrated.Reason);
        Assert.True(rehydrated.IsActive);
    }

    #endregion

    #region StudentCharge Tests

    [Fact]
    public void StudentCharge_Create_ComputesGrossNetAndOutstandingCorrectly()
    {
        var result = StudentCharge.Create(
            AcademicYearId,
            "2026/2027",
            null,
            null,
            "2026-07",
            ChargeTypeId,
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            "SMP",
            "7",
            unitAmount: 350_000m,
            quantity: 1,
            discountAmount: 70_000m,
            discountReason: "Diskon 20%",
            dueDate: new DateOnly(2026, 7, 10),
            utcNow: FixedNow);

        Assert.True(result.IsSuccess);
        var charge = result.Value!;
        Assert.Equal(350_000m, charge.GrossAmount);
        Assert.Equal(70_000m, charge.DiscountAmount);
        Assert.Equal(280_000m, charge.NetAmount);
        Assert.Equal(0m, charge.PaidAmount);
        Assert.Equal(280_000m, charge.OutstandingAmount);
        Assert.Equal(ChargeStatuses.Unpaid, charge.Status);
    }

    [Fact]
    public void StudentCharge_ApplyPayment_TransitionsToPartiallyPaidAndPaid()
    {
        var charge = StudentCharge.Create(
            AcademicYearId,
            "2026/2027",
            null,
            null,
            "2026-07",
            ChargeTypeId,
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            "SMP",
            "7",
            unitAmount: 300_000m,
            quantity: 1,
            utcNow: FixedNow).Value!;

        // Partial payment 100,000
        var p1Result = charge.ApplyPayment(100_000m, FixedNow.AddDays(1));
        Assert.True(p1Result.IsSuccess);
        Assert.Equal(100_000m, charge.PaidAmount);
        Assert.Equal(200_000m, charge.OutstandingAmount);
        Assert.Equal(ChargeStatuses.PartiallyPaid, charge.Status);

        // Final payment 200,000
        var p2Result = charge.ApplyPayment(200_000m, FixedNow.AddDays(2));
        Assert.True(p2Result.IsSuccess);
        Assert.Equal(300_000m, charge.PaidAmount);
        Assert.Equal(0m, charge.OutstandingAmount);
        Assert.Equal(ChargeStatuses.Paid, charge.Status);
    }

    [Fact]
    public void StudentCharge_ApplyPayment_ExceedingOutstanding_Fails()
    {
        var charge = StudentCharge.Create(
            AcademicYearId,
            "2026/2027",
            null,
            null,
            "2026-07",
            ChargeTypeId,
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            "SMP",
            "7",
            unitAmount: 200_000m,
            quantity: 1,
            utcNow: FixedNow).Value!;

        var result = charge.ApplyPayment(250_000m);
        Assert.True(result.IsFailure);
        Assert.Equal("Fin.StudentCharge.PaymentExceedsOutstanding", result.Error.Code);
    }

    [Fact]
    public void StudentCharge_Cancel_WhenUnpaid_Succeeds()
    {
        var charge = StudentCharge.Create(
            AcademicYearId,
            "2026/2027",
            null,
            null,
            "2026-07",
            ChargeTypeId,
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            "SMP",
            "7",
            unitAmount: 200_000m,
            quantity: 1,
            utcNow: FixedNow).Value!;

        var cancelResult = charge.Cancel("Tagihan salah input", FixedNow.AddDays(1));
        Assert.True(cancelResult.IsSuccess);
        Assert.Equal(ChargeStatuses.Cancelled, charge.Status);
        Assert.Equal("Tagihan salah input", charge.CancellationReason);
        Assert.Equal(FixedNow.AddDays(1), charge.CancelledAt);
    }

    [Fact]
    public void StudentCharge_Cancel_WhenPaidAmountExists_Fails()
    {
        var charge = StudentCharge.Create(
            AcademicYearId,
            "2026/2027",
            null,
            null,
            "2026-07",
            ChargeTypeId,
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            "SMP",
            "7",
            unitAmount: 200_000m,
            quantity: 1,
            utcNow: FixedNow).Value!;

        charge.ApplyPayment(50_000m);

        var cancelResult = charge.Cancel("Salah tagih", FixedNow.AddDays(1));
        Assert.True(cancelResult.IsFailure);
        Assert.Equal("Fin.StudentCharge.CannotCancelPaidCharge", cancelResult.Error.Code);
    }

    #endregion

    #region PaymentReceipt & Allocations Tests

    [Fact]
    public void PaymentReceipt_Create_SetsUnallocatedTotalAndRaisesEvent()
    {
        var cashierId = Guid.NewGuid();
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
            cashierId,
            "Kasir Keuangan",
            notes: "Pembayaran SPP dan kegiatan",
            utcNow: FixedNow);

        Assert.True(result.IsSuccess);
        var receipt = result.Value!;
        Assert.Equal("KWT-202611-0001", receipt.ReceiptNumber);
        Assert.Equal(500_000m, receipt.TotalAmount);
        Assert.Equal(0m, receipt.AllocatedAmount);
        Assert.Equal(500_000m, receipt.UnallocatedAmount);

        // Verify Domain Event raised
        var evt = Assert.Single(receipt.DomainEvents);
        var postedEvent = Assert.IsType<PaymentReceiptPostedDomainEvent>(evt);
        Assert.Equal(receipt.Id, postedEvent.ReceiptId);
        Assert.Equal("KWT-202611-0001", postedEvent.ReceiptNumber);
        Assert.Equal(StudentId, postedEvent.StudentId);
        Assert.Equal(500_000m, postedEvent.TotalAmount);
    }

    [Fact]
    public void PaymentReceipt_AllocateCharge_AddsAllocationAndUpdatesBalances()
    {
        var cashierId = Guid.NewGuid();
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
            500_000m,
            cashierId,
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
        Assert.Equal(150_000m, receipt.UnallocatedAmount); // 500k - 350k = 150k surplus deposit

        var alloc = Assert.Single(receipt.Allocations);
        Assert.Equal(chargeId, alloc.StudentChargeId);
        Assert.Equal(350_000m, alloc.AllocatedAmount);
    }

    [Fact]
    public void PaymentReceipt_AllocateCharge_ExceedingTotalAmount_Fails()
    {
        var cashierId = Guid.NewGuid();
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
            300_000m,
            cashierId,
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
    }

    [Fact]
    public void PaymentReceipt_Rehydrate_WithAllocations_RestoresState()
    {
        var receiptId = Guid.NewGuid();
        var allocId = Guid.NewGuid();
        var chargeId = Guid.NewGuid();

        var allocation = PaymentAllocation.Rehydrate(
            allocId,
            receiptId,
            "KWT-202611-0002",
            chargeId,
            "SPP Bulanan",
            "2026-11",
            StudentId,
            "Ahmad",
            "2026001",
            350_000m,
            new DateOnly(2026, 11, 20),
            FixedNow,
            FixedNow);

        var rehydrated = PaymentReceipt.Rehydrate(
            receiptId,
            "KWT-202611-0002",
            AcademicYearId,
            "2026/2027",
            StudentId,
            "Ahmad",
            "2026001",
            null,
            null,
            new DateOnly(2026, 11, 20),
            PaymentMethods.BankTransfer,
            "TRX-12345",
            400_000m,
            350_000m,
            50_000m,
            Guid.NewGuid(),
            "Kasir",
            "Transfer bank",
            FixedNow,
            FixedNow,
            [allocation]);

        Assert.Equal(receiptId, rehydrated.Id);
        Assert.Equal("KWT-202611-0002", rehydrated.ReceiptNumber);
        Assert.Equal(400_000m, rehydrated.TotalAmount);
        Assert.Equal(350_000m, rehydrated.AllocatedAmount);
        Assert.Equal(50_000m, rehydrated.UnallocatedAmount);
        Assert.Single(rehydrated.Allocations);
    }

    #endregion
}
