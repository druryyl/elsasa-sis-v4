using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Common;
using Xunit;

namespace ElsasaSis.UnitTests.Finance;

public sealed class StudentChargeTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private static readonly Guid ChargeTypeId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();

    [Fact]
    public void Create_WithDiscount_ComputesGrossNetAndOutstandingAccurately()
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
            discountReason: "Diskon Saudara Kandung 20%",
            dueDate: new DateOnly(2026, 7, 10),
            utcNow: FixedNow);

        Assert.True(result.IsSuccess);
        var charge = result.Value!;
        Assert.NotEqual(Guid.Empty, charge.Id);
        Assert.Equal(350_000m, charge.GrossAmount);
        Assert.Equal(70_000m, charge.DiscountAmount);
        Assert.Equal("Diskon Saudara Kandung 20%", charge.DiscountReason);
        Assert.Equal(280_000m, charge.NetAmount);
        Assert.Equal(0m, charge.PaidAmount);
        Assert.Equal(280_000m, charge.OutstandingAmount);
        Assert.Equal(ChargeStatuses.Unpaid, charge.Status);
    }

    [Fact]
    public void Create_WithoutDiscount_SetsNetEqualToGross()
    {
        var result = StudentCharge.Create(
            AcademicYearId,
            "2026/2027",
            null,
            null,
            null,
            ChargeTypeId,
            "Seragam Olahraga",
            ChargeFrequencies.OneTime,
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            "SMP",
            "7",
            unitAmount: 250_000m,
            quantity: 2,
            utcNow: FixedNow);

        Assert.True(result.IsSuccess);
        var charge = result.Value!;
        Assert.Equal(500_000m, charge.GrossAmount);
        Assert.Equal(0m, charge.DiscountAmount);
        Assert.Equal(500_000m, charge.NetAmount);
        Assert.Equal(500_000m, charge.OutstandingAmount);
        Assert.Equal(ChargeStatuses.Unpaid, charge.Status);
    }

    [Fact]
    public void ApplyPayment_PartialAmount_TransitionsStatusToPartiallyPaid()
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

        var result = charge.ApplyPayment(100_000m, FixedNow.AddDays(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(100_000m, charge.PaidAmount);
        Assert.Equal(200_000m, charge.OutstandingAmount);
        Assert.Equal(ChargeStatuses.PartiallyPaid, charge.Status);
        Assert.Equal(FixedNow.AddDays(1), charge.UpdatedAt);
    }

    [Fact]
    public void ApplyPayment_FullAmount_TransitionsStatusToPaid()
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

        charge.ApplyPayment(100_000m, FixedNow.AddDays(1));
        var result = charge.ApplyPayment(200_000m, FixedNow.AddDays(2));

        Assert.True(result.IsSuccess);
        Assert.Equal(300_000m, charge.PaidAmount);
        Assert.Equal(0m, charge.OutstandingAmount);
        Assert.Equal(ChargeStatuses.Paid, charge.Status);
        Assert.Equal(FixedNow.AddDays(2), charge.UpdatedAt);
    }

    [Fact]
    public void ApplyPayment_ExceedingOutstanding_FailsWithPaymentExceedsOutstanding()
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
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50000)]
    public void ApplyPayment_ZeroOrNegativeAmount_Fails(decimal invalidAmount)
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

        var result = charge.ApplyPayment(invalidAmount);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.Payment.InvalidAmount", result.Error.Code);
    }

    [Fact]
    public void Cancel_WhenUnpaid_SucceedsAndTransitionsToCancelled()
    {
        // Enforces specification: UC-FIN-05, OQ-020 cancellation of unpaid charge
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

        var cancelResult = charge.Cancel("Tagihan ganda terbit tidak sengaja", FixedNow.AddDays(1));

        Assert.True(cancelResult.IsSuccess);
        Assert.Equal(ChargeStatuses.Cancelled, charge.Status);
        Assert.Equal("Tagihan ganda terbit tidak sengaja", charge.CancellationReason);
        Assert.Equal(FixedNow.AddDays(1), charge.CancelledAt);
        Assert.Equal(FixedNow.AddDays(1), charge.UpdatedAt);
    }

    [Fact]
    public void Cancel_WhenPaidAmountGreaterThanZero_FailsWithCannotCancelPaidCharge()
    {
        // Enforces specification: GAP-002, OQ-020 Cancellation is strictly rejected if paid_amount > 0
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

        // Partial payment applied
        charge.ApplyPayment(50_000m);
        Assert.True(charge.PaidAmount > 0);

        var cancelResult = charge.Cancel("Pembatalan setelah bayar sebagian", FixedNow.AddDays(1));

        Assert.True(cancelResult.IsFailure);
        Assert.Equal("Fin.StudentCharge.CannotCancelPaidCharge", cancelResult.Error.Code);
        Assert.Equal(ErrorType.Conflict, cancelResult.Error.Type);
        Assert.NotEqual(ChargeStatuses.Cancelled, charge.Status);
    }

    [Fact]
    public void Cancel_WhenFullyPaid_FailsWithCannotCancelPaidCharge()
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

        charge.ApplyPayment(200_000m);
        Assert.Equal(ChargeStatuses.Paid, charge.Status);

        var cancelResult = charge.Cancel("Batalkan tagihan lunas", FixedNow.AddDays(1));

        Assert.True(cancelResult.IsFailure);
        Assert.Equal("Fin.StudentCharge.CannotCancelPaidCharge", cancelResult.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancel_WithEmptyCancellationReason_FailsWithReasonRequired(string invalidReason)
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

        var cancelResult = charge.Cancel(invalidReason, FixedNow.AddDays(1));

        Assert.True(cancelResult.IsFailure);
        Assert.Equal("Fin.StudentCharge.MissingCancellationReason", cancelResult.Error.Code);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_FailsWithAlreadyCancelled()
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

        charge.Cancel("Alasan pertama", FixedNow.AddDays(1));

        var secondCancel = charge.Cancel("Alasan kedua", FixedNow.AddDays(2));

        Assert.True(secondCancel.IsFailure);
        Assert.Equal("Fin.StudentCharge.AlreadyCancelled", secondCancel.Error.Code);
    }

    [Fact]
    public void Rehydrate_RestoresExactChargeState()
    {
        var id = Guid.NewGuid();
        var rehydrated = StudentCharge.Rehydrate(
            id,
            AcademicYearId,
            "2026/2027",
            null,
            null,
            "2026-07",
            ChargeTypeId,
            "SPP Bulanan",
            ChargeFrequencies.Monthly,
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            "SMP",
            "7",
            null,
            null,
            null,
            350_000m,
            1,
            350_000m,
            70_000m,
            "Diskon 20%",
            280_000m,
            100_000m,
            180_000m,
            ChargeStatuses.PartiallyPaid,
            new DateOnly(2026, 7, 10),
            null,
            null,
            FixedNow,
            FixedNow);

        Assert.Equal(id, rehydrated.Id);
        Assert.Equal(280_000m, rehydrated.NetAmount);
        Assert.Equal(100_000m, rehydrated.PaidAmount);
        Assert.Equal(180_000m, rehydrated.OutstandingAmount);
        Assert.Equal(ChargeStatuses.PartiallyPaid, rehydrated.Status);
    }
}
