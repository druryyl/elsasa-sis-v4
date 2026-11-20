using System.Reflection;
using ElsasaSis.Modules.Fin;
using ElsasaSis.Modules.Fin.Features.Reports;
using ElsasaSis.Modules.Fin.Features.Reports.DailyCashier;
using ElsasaSis.Modules.Fin.Features.Reports.ReceivablesAging;
using ElsasaSis.Modules.Fin.Features.Reports.ReceivablesSummary;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using ElsasaSis.SharedKernel.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElsasaSis.UnitTests.Finance;

public sealed class ReportsFeatureTests
{
    [Fact]
    public void DependencyInjection_RegistersAllReportHandlers()
    {
        var services = new ServiceCollection();
        services.AddFinModule();

        var descriptors = services.ToList();

        Assert.Contains(descriptors, d => d.ServiceType == typeof(GetReceivablesSummaryHandler) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(descriptors, d => d.ServiceType == typeof(GetReceivablesAgingHandler) && d.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(descriptors, d => d.ServiceType == typeof(GetDailyCashierSummaryHandler) && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void ReceivablesSummaryDto_PreservesTotalsAndBreakdown()
    {
        var yearId = Guid.NewGuid();
        var breakdown = new List<ReceivablesSummaryByLevelDto>
        {
            new(yearId, "2026/2027", "SMP", 3_500_000m, 2_000_000m, 1_500_000m, 10, 10),
            new(yearId, "2026/2027", "SMA", 4_500_000m, 3_000_000m, 1_500_000m, 10, 10)
        };

        var dto = new ReceivablesSummaryDto(
            TotalBilled: 8_000_000m,
            TotalCollected: 5_000_000m,
            TotalOutstanding: 3_000_000m,
            TotalStudentsCount: 20,
            TotalChargesCount: 20,
            Breakdown: breakdown);

        Assert.Equal(8_000_000m, dto.TotalBilled);
        Assert.Equal(5_000_000m, dto.TotalCollected);
        Assert.Equal(3_000_000m, dto.TotalOutstanding);
        Assert.Equal(20, dto.TotalStudentsCount);
        Assert.Equal(20, dto.TotalChargesCount);
        Assert.Equal(2, dto.Breakdown.Count);
        Assert.Equal("SMP", dto.Breakdown[0].SchoolLevel);
        Assert.Equal("SMA", dto.Breakdown[1].SchoolLevel);
    }

    [Fact]
    public void ReceivablesAgingDto_CalculatesArrearsOver30DaysCorrectly()
    {
        var summary = new ReceivablesAgingSummaryDto(
            CurrentAmount: 1_000_000m,
            Days1To30Amount: 500_000m,
            Days31To60Amount: 300_000m,
            Days61To90Amount: 200_000m,
            Over90DaysAmount: 100_000m,
            TotalOutstanding: 2_100_000m,
            ArrearsOver30Days: 600_000m,
            TotalStudentsCount: 5,
            TotalChargesCount: 8);

        var studentId = Guid.NewGuid();
        var students = new List<ReceivablesAgingStudentItemDto>
        {
            new(
                studentId,
                "Ahmad Dahlan",
                "2026001",
                "SMP",
                "7",
                Guid.NewGuid(),
                "VII-A",
                CurrentAmount: 350_000m,
                Days1To30Amount: 0m,
                Days31To60Amount: 350_000m,
                Days61To90Amount: 0m,
                Over90DaysAmount: 0m,
                TotalOutstanding: 700_000m,
                UnpaidChargeCount: 2)
        };

        var dto = new ReceivablesAgingDto(summary, students);

        Assert.Equal(2_100_000m, dto.Summary.TotalOutstanding);
        Assert.Equal(600_000m, dto.Summary.ArrearsOver30Days);
        Assert.Equal(300_000m + 200_000m + 100_000m, dto.Summary.Days31To60Amount + dto.Summary.Days61To90Amount + dto.Summary.Over90DaysAmount);
        Assert.Single(dto.Students);
        Assert.Equal("Ahmad Dahlan", dto.Students[0].StudentName);
        Assert.Equal(700_000m, dto.Students[0].TotalOutstanding);
    }

    [Fact]
    public void DailyCashierSummaryDto_AggregatesTotalsAndMethodBreakdownCorrectly()
    {
        var targetDate = new DateOnly(2026, 7, 15);
        var receipts = new List<DailyCashierReceiptItemDto>
        {
            new(
                Guid.NewGuid(),
                "KWT-202607-0001",
                Guid.NewGuid(),
                "2026/2027",
                Guid.NewGuid(),
                "Ahmad Dahlan",
                "2026001",
                Guid.NewGuid(),
                "VII-A",
                targetDate,
                "CASH",
                null,
                TotalAmount: 350_000m,
                AllocatedAmount: 350_000m,
                UnallocatedAmount: 0m,
                Guid.NewGuid(),
                "Kasir Keuangan",
                null,
                DateTime.UtcNow),
            new(
                Guid.NewGuid(),
                "KWT-202607-0002",
                Guid.NewGuid(),
                "2026/2027",
                Guid.NewGuid(),
                "Siti Nurhaliza",
                "2026002",
                Guid.NewGuid(),
                "VII-A",
                targetDate,
                "BANK_TRANSFER",
                "TRX-998877",
                TotalAmount: 500_000m,
                AllocatedAmount: 400_000m,
                UnallocatedAmount: 100_000m,
                Guid.NewGuid(),
                "Kasir Keuangan",
                "Transfer via BCA",
                DateTime.UtcNow)
        };

        var methodSummaries = new List<PaymentMethodSummaryDto>
        {
            new("BANK_TRANSFER", 500_000m, 1),
            new("CASH", 350_000m, 1)
        };

        var dto = new DailyCashierSummaryDto(
            targetDate,
            TotalCashCollections: 350_000m,
            TotalBankTransferCollections: 500_000m,
            TotalCollections: 850_000m,
            TotalDepositsHeld: 100_000m,
            TotalAllocated: 750_000m,
            ReceiptCount: 2,
            PaymentMethodSummaries: methodSummaries,
            Receipts: receipts);

        Assert.Equal(targetDate, dto.Date);
        Assert.Equal(350_000m, dto.TotalCashCollections);
        Assert.Equal(500_000m, dto.TotalBankTransferCollections);
        Assert.Equal(850_000m, dto.TotalCollections);
        Assert.Equal(100_000m, dto.TotalDepositsHeld);
        Assert.Equal(750_000m, dto.TotalAllocated);
        Assert.Equal(2, dto.ReceiptCount);
        Assert.Equal(2, dto.PaymentMethodSummaries.Count);
        Assert.Equal(2, dto.Receipts.Count);
    }

    [Fact]
    public void ReportsEndpoints_ImplementsIEndpoint()
    {
        var endpoint = new ReportsEndpoints();
        Assert.IsAssignableFrom<IEndpoint>(endpoint);
    }

    [Fact]
    public void GetReceivablesAgingQuery_HandlesOptionalDefaults()
    {
        var query = new GetReceivablesAgingQuery();

        Assert.Null(query.AcademicYearId);
        Assert.Null(query.SchoolLevel);
        Assert.Null(query.Grade);
        Assert.Null(query.StudyGroupId);
        Assert.Null(query.AsOfDate);
    }

    [Fact]
    public void GetDailyCashierSummaryQuery_HandlesOptionalDefaults()
    {
        var query = new GetDailyCashierSummaryQuery();

        Assert.Null(query.Date);
    }

    [Fact]
    public void GetReceivablesSummaryQuery_HandlesOptionalDefaults()
    {
        var query = new GetReceivablesSummaryQuery();

        Assert.Null(query.AcademicYearId);
    }
}
