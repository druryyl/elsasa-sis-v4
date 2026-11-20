using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Common;
using Xunit;

namespace ElsasaSis.UnitTests.Finance;

public sealed class ChargeTypeTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AcademicYearId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidParameters_InitializesWithDraftStatus()
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
        Assert.NotEqual(Guid.Empty, chargeType.Id);
        Assert.Equal("SPP Bulanan SMP", chargeType.Name);
        Assert.Equal(ChargeFrequencies.Monthly, chargeType.Frequency);
        Assert.Equal(350_000m, chargeType.Amount);
        Assert.Equal(ChargeTypeStatuses.Draft, chargeType.Status);
        Assert.Equal("SMP", chargeType.SchoolLevel);
        Assert.Equal("7", chargeType.Grade);
        Assert.Equal(FixedNow, chargeType.CreatedAt);
        Assert.Equal(FixedNow, chargeType.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_Fails(string invalidName)
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
    public void Create_WithNegativeAmount_Fails()
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

    [Theory]
    [InlineData("")]
    [InlineData("WEEKLY")]
    [InlineData("BIWEEKLY")]
    public void Create_WithInvalidFrequency_Fails(string invalidFrequency)
    {
        var result = ChargeType.Create(
            AcademicYearId,
            "2026/2027",
            "SPP Bulanan SMP",
            invalidFrequency,
            350_000m);

        Assert.True(result.IsFailure);
        Assert.Equal("Fin.ChargeType.InvalidFrequency", result.Error.Code);
    }

    [Fact]
    public void Activate_TransitionsStatusFromDraftToActive()
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
    public void Update_WhenInDraftStatus_ModifiesPropertiesSuccessfully()
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
            380_000m,
            "SMP",
            "7",
            null,
            null,
            FixedNow.AddHours(2));

        Assert.True(updateResult.IsSuccess);
        Assert.Equal("SPP Bulanan SMP Revisi", chargeType.Name);
        Assert.Equal(380_000m, chargeType.Amount);
        Assert.Equal(FixedNow.AddHours(2), chargeType.UpdatedAt);
    }

    [Fact]
    public void Update_WhenInActiveStatus_IsImmutableAndFailsWithConflict()
    {
        // Enforces specification: OQ-011 Active charge types cannot be modified
        var chargeType = ChargeType.Create(
            AcademicYearId,
            "2026/2027",
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            350_000m,
            utcNow: FixedNow).Value!;

        chargeType.Activate(FixedNow.AddHours(1));

        var updateResult = chargeType.Update(
            "SPP Bulanan SMP Baru",
            ChargeFrequencies.Monthly,
            400_000m,
            utcNow: FixedNow.AddHours(2));

        Assert.True(updateResult.IsFailure);
        Assert.Equal("Fin.ChargeType.CannotBeModified", updateResult.Error.Code);
        Assert.Equal(ErrorType.Conflict, updateResult.Error.Type);
    }

    [Fact]
    public void Update_WithInvalidAmount_Fails()
    {
        var chargeType = ChargeType.Create(
            AcademicYearId,
            "2026/2027",
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            350_000m,
            utcNow: FixedNow).Value!;

        var updateResult = chargeType.Update(
            "SPP Bulanan SMP",
            ChargeFrequencies.Monthly,
            -50_000m,
            utcNow: FixedNow.AddHours(1));

        Assert.True(updateResult.IsFailure);
        Assert.Equal("Fin.ChargeType.InvalidAmount", updateResult.Error.Code);
    }

    [Fact]
    public void Rehydrate_RestoresExactState()
    {
        var id = Guid.NewGuid();
        var rehydrated = ChargeType.Rehydrate(
            id,
            AcademicYearId,
            "2026/2027",
            "Uang Seragam",
            ChargeFrequencies.OneTime,
            450_000m,
            "SMA",
            "10",
            null,
            null,
            ChargeTypeStatuses.Active,
            FixedNow,
            FixedNow.AddDays(1));

        Assert.Equal(id, rehydrated.Id);
        Assert.Equal("Uang Seragam", rehydrated.Name);
        Assert.Equal(ChargeFrequencies.OneTime, rehydrated.Frequency);
        Assert.Equal(450_000m, rehydrated.Amount);
        Assert.Equal(ChargeTypeStatuses.Active, rehydrated.Status);
    }
}
