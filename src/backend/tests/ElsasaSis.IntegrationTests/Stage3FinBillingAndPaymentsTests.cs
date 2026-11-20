using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Dapper;
using ElsasaSis.Database;
using ElsasaSis.IntegrationTests.Fixtures;
using ElsasaSis.Modules.Fin.Features.Billing;
using ElsasaSis.Modules.Fin.Features.Billing.CancelStudentCharge;
using ElsasaSis.Modules.Fin.Features.Billing.CreateAdHocCharge;
using ElsasaSis.Modules.Fin.Features.Billing.GenerateBatchTuitionCharges;
using ElsasaSis.Modules.Fin.Features.Discounts;
using ElsasaSis.Modules.Fin.Features.Discounts.CreateStudentDiscount;
using ElsasaSis.Modules.Fin.Features.FeePolicy;
using ElsasaSis.Modules.Fin.Features.FeePolicy.CreateChargeType;
using ElsasaSis.Modules.Fin.Features.FeePolicy.UpdateChargeType;
using ElsasaSis.Modules.Fin.Features.Payments;
using ElsasaSis.Modules.Fin.Features.Payments.AllocateAdvanceDeposit;
using ElsasaSis.Modules.Fin.Features.Payments.RecordPaymentReceipt;
using ElsasaSis.Modules.Fin.Features.Reports.DailyCashier;
using ElsasaSis.Modules.Fin.Features.Reports.ReceivablesAging;
using ElsasaSis.Modules.Fin.Features.Reports.ReceivablesSummary;
using Microsoft.Data.SqlClient;

namespace ElsasaSis.IntegrationTests;

public sealed class Stage3FinBillingAndPaymentsTests : IntegrationTestBase
{
    public Stage3FinBillingAndPaymentsTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedStage1FixturesAsync();
        await SeedStage3FixturesAsync();
    }

    private async Task<(Guid AcademicYearId, string AcademicYearName, Guid StudyGroupId7A, Guid Student1Id, string Student1Name, string Student1Nis, Guid SppChargeTypeId, Guid UniformChargeTypeId, Guid AhmadOpenChargeId, decimal AhmadOpenChargeAmount)> GetFixtureContextAsync()
    {
        await using var conn = new SqlConnection(Factory.ConnectionString);
        await conn.OpenAsync();

        var (academicYearId, academicYearName) = await conn.QuerySingleAsync<(Guid, string)>(
            "SELECT TOP (1) [id], [name] FROM [opr].[academic_years] WHERE [status] = 'ACTIVE' ORDER BY [created_at] ASC");

        var studyGroupId = await conn.QuerySingleAsync<Guid>(
            "SELECT TOP (1) [id] FROM [std].[study_groups] WHERE [academic_year_id] = @YearId AND [name] = '7A'",
            new { YearId = academicYearId });

        var (student1Id, student1Name, student1Nis) = await conn.QuerySingleAsync<(Guid, string, string)>(
            "SELECT TOP (1) [id], [full_name], [nis] FROM [std].[students] WHERE [nis] = '202607001' OR [full_name] = N'Ahmad Dahlan'");

        var sppChargeTypeId = await conn.QuerySingleAsync<Guid>(
            "SELECT TOP (1) [id] FROM [fin].[charge_types] WHERE [name] = N'SPP Bulanan SMP'");

        var uniformChargeTypeId = await conn.QuerySingleAsync<Guid>(
            "SELECT TOP (1) [id] FROM [fin].[charge_types] WHERE [name] = N'Seragam Sekolah'");

        var openCharge = await conn.QuerySingleAsync<(Guid Id, decimal NetAmount)>(
            "SELECT TOP (1) [id], [net_amount] FROM [fin].[student_charges] WHERE [student_id] = @StudentId AND [status] = 'UNPAID'",
            new { StudentId = student1Id });

        return (academicYearId, academicYearName, studyGroupId, student1Id, student1Name, student1Nis, sppChargeTypeId, uniformChargeTypeId, openCharge.Id, openCharge.NetAmount);
    }

    [Fact]
    public async Task ChargeType_Lifecycle_CreateDraft_Update_ActivateLock_AndRejectSubsequentUpdates()
    {
        var ctx = await GetFixtureContextAsync();
        using var financeClient = await CreateAuthenticatedClientAsync(
            Stage3Seeder.DefaultFinanceUsername,
            Stage3Seeder.DefaultFinancePassword);

        // 1. Create draft fee structure -> 201 Created
        var createReq = new CreateChargeTypeRequest(
            ctx.AcademicYearId,
            ctx.AcademicYearName,
            "Uang Pembangunan 2026",
            "ONE_TIME",
            2000000.00m,
            "SMP");

        var createResp = await financeClient.PostAsJsonAsync("/api/fin/charge-types", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var chargeType = await createResp.Content.ReadFromJsonAsync<ChargeTypeDto>(JsonOptions);
        Assert.NotNull(chargeType);
        Assert.Equal("DRAFT", chargeType!.Status);
        var chargeTypeId = chargeType.Id;

        // 2. Update draft fee structure -> 200 OK
        var updateReq = new UpdateChargeTypeRequest(
            "Uang Pembangunan 2026 (Revisi Diskon)",
            "ONE_TIME",
            1800000.00m,
            "SMP");

        var updateResp = await financeClient.PutAsJsonAsync($"/api/fin/charge-types/{chargeTypeId}", updateReq);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        var updated = await updateResp.Content.ReadFromJsonAsync<ChargeTypeDto>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(1800000.00m, updated!.Amount);

        // 3. Activate fee structure (Tier 2 lock) -> 200 OK
        var activateResp = await financeClient.PostAsJsonAsync($"/api/fin/charge-types/{chargeTypeId}/activate", new { });
        Assert.Equal(HttpStatusCode.OK, activateResp.StatusCode);

        var activated = await activateResp.Content.ReadFromJsonAsync<ChargeTypeDto>(JsonOptions);
        Assert.NotNull(activated);
        Assert.Equal("ACTIVE", activated!.Status);

        // 4. Updating active fee structure returns 409 Conflict
        var conflictResp = await financeClient.PutAsJsonAsync(
            $"/api/fin/charge-types/{chargeTypeId}",
            new UpdateChargeTypeRequest("Uang Pembangunan Edit Terlarang", "ONE_TIME", 1900000.00m, "SMP"));
        Assert.Equal(HttpStatusCode.Conflict, conflictResp.StatusCode);
    }

    [Fact]
    public async Task StudentStandingDiscount_Creation_Succeeds()
    {
        var ctx = await GetFixtureContextAsync();
        using var financeClient = await CreateAuthenticatedClientAsync(
            Stage3Seeder.DefaultFinanceUsername,
            Stage3Seeder.DefaultFinancePassword);

        // Create a new distinct charge type for discount testing
        var ctReq = new CreateChargeTypeRequest(
            ctx.AcademicYearId,
            ctx.AcademicYearName,
            "Biaya Ekskul Khusus",
            "MONTHLY",
            100000.00m,
            "SMP");
        var ctResp = await financeClient.PostAsJsonAsync("/api/fin/charge-types", ctReq);
        var ct = await ctResp.Content.ReadFromJsonAsync<ChargeTypeDto>(JsonOptions);
        Assert.NotNull(ct);

        // Create standing discount
        var discountReq = new CreateStudentDiscountRequest(
            ctx.Student1Id,
            ct!.Id,
            ctx.AcademicYearId,
            ctx.AcademicYearName,
            ctx.Student1Name,
            ctx.Student1Nis,
            "PERCENTAGE",
            50.00m,
            "Beasiswa Minat Bakat");

        var discResp = await financeClient.PostAsJsonAsync("/api/fin/discounts", discountReq);
        Assert.Equal(HttpStatusCode.Created, discResp.StatusCode);

        var discount = await discResp.Content.ReadFromJsonAsync<StudentDiscountDto>(JsonOptions);
        Assert.NotNull(discount);
        Assert.Equal(50.00m, discount!.DiscountValue);
        Assert.Equal("PERCENTAGE", discount.DiscountType);
        Assert.True(discount.IsActive);
    }

    [Fact]
    public async Task BatchBillingGeneration_AppliesDiscounts_AndSkipsDuplicatesIdempotently()
    {
        var ctx = await GetFixtureContextAsync();
        using var financeClient = await CreateAuthenticatedClientAsync(
            Stage3Seeder.DefaultFinanceUsername,
            Stage3Seeder.DefaultFinancePassword);

        // Batch generation for August 2026 ("2026-08")
        var batchReq = new GenerateBatchTuitionChargesRequest(
            ctx.AcademicYearId,
            "2026-08",
            ctx.StudyGroupId7A,
            SchoolLevel: "SMP");

        // 1. First execution creates charges and applies student discounts
        var postResp = await financeClient.PostAsJsonAsync("/api/fin/charges/generate-batch", batchReq);
        Assert.Equal(HttpStatusCode.OK, postResp.StatusCode);

        var batchResult = await postResp.Content.ReadFromJsonAsync<GenerateBatchTuitionChargesResponse>(JsonOptions);
        Assert.NotNull(batchResult);
        Assert.True(batchResult!.GeneratedCount >= 1);

        // 2. Second execution skips duplicate student charges idempotently
        var secondResp = await financeClient.PostAsJsonAsync("/api/fin/charges/generate-batch", batchReq);
        Assert.Equal(HttpStatusCode.OK, secondResp.StatusCode);

        var secondResult = await secondResp.Content.ReadFromJsonAsync<GenerateBatchTuitionChargesResponse>(JsonOptions);
        Assert.NotNull(secondResult);
        Assert.Equal(0, secondResult!.GeneratedCount);
        Assert.True(secondResult.SkippedCount >= 1);
    }

    [Fact]
    public async Task AdHocCharge_Creation_AndCancellationWithAuditReason()
    {
        var ctx = await GetFixtureContextAsync();
        using var financeClient = await CreateAuthenticatedClientAsync(
            Stage3Seeder.DefaultFinanceUsername,
            Stage3Seeder.DefaultFinancePassword);

        // 1. Create ad-hoc charge for Ahmad Dahlan -> 201 Created
        var createReq = new CreateAdHocChargeRequest(
            ctx.AcademicYearId,
            ctx.Student1Id,
            ctx.UniformChargeTypeId,
            "Seragam Batik Cadangan",
            120000.00m,
            1,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)));

        var createResp = await financeClient.PostAsJsonAsync("/api/fin/charges/adhoc", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var charge = await createResp.Content.ReadFromJsonAsync<StudentChargeDto>(JsonOptions);
        Assert.NotNull(charge);
        Assert.Equal(120000.00m, charge!.NetAmount);
        Assert.Equal("UNPAID", charge.Status);
        var chargeId = charge.Id;

        // 2. Reject cancellation with empty reason -> 422 UnprocessableEntity
        var emptyReq = new CancelStudentChargeRequest(string.Empty);
        var invalidResp = await financeClient.PostAsJsonAsync($"/api/fin/charges/{chargeId}/cancel", emptyReq);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidResp.StatusCode);

        // 3. Cancel with mandatory audit reason -> 200 OK
        var cancelReq = new CancelStudentChargeRequest("Dibatalkan karena wali murid membawa seragam sendiri.");
        var cancelResp = await financeClient.PostAsJsonAsync($"/api/fin/charges/{chargeId}/cancel", cancelReq);
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);

        var cancelled = await cancelResp.Content.ReadFromJsonAsync<StudentChargeDto>(JsonOptions);
        Assert.NotNull(cancelled);
        Assert.Equal("CANCELLED", cancelled!.Status);
        Assert.Equal(cancelReq.CancellationReason, cancelled.CancellationReason);
        Assert.NotNull(cancelled.CancelledAt);
    }

    [Fact]
    public async Task CashierPaymentPosting_SequenceCounter_MultiAllocation_AndExcessDepositHolding()
    {
        var ctx = await GetFixtureContextAsync();
        using var financeClient = await CreateAuthenticatedClientAsync(
            Stage3Seeder.DefaultFinanceUsername,
            Stage3Seeder.DefaultFinancePassword);

        // Ahmad Dahlan has an unpaid Uniform charge with net_amount = 250,000
        var openChargeId = ctx.AhmadOpenChargeId;
        var chargeAmount = ctx.AhmadOpenChargeAmount;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Cashier receives chargeAmount + 50,000 (settles charge, 50,000 surplus held as unallocated deposit)
        var totalPaid = chargeAmount + 50000.00m;
        var paymentReq = new RecordPaymentReceiptRequest(
            ctx.Student1Id,
            ctx.AcademicYearId,
            today,
            "CASH",
            totalPaid,
            Allocations: [new PaymentAllocationItemRequest(openChargeId, chargeAmount)],
            Notes: "Pembayaran seragam tunai lebih bayar");

        var postResp = await financeClient.PostAsJsonAsync("/api/fin/payments", paymentReq);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);

        var receipt = await postResp.Content.ReadFromJsonAsync<PaymentReceiptDto>(JsonOptions);
        Assert.NotNull(receipt);

        // Concurrency-safe atomic receipt sequence formatting: KWT-YYYYMM-NNNN
        Assert.Matches(@"^KWT-\d{6}-\d{4}$", receipt!.ReceiptNumber);
        Assert.Equal(totalPaid, receipt.TotalAmount);
        Assert.Equal(chargeAmount, receipt.AllocatedAmount);
        Assert.Equal(50000.00m, receipt.UnallocatedAmount);

        // Verify settled charge status transitioned to PAID
        var getReceiptResp = await financeClient.GetAsync($"/api/fin/payments/{receipt.Id}");
        Assert.Equal(HttpStatusCode.OK, getReceiptResp.StatusCode);

        var receiptDetail = await getReceiptResp.Content.ReadFromJsonAsync<PaymentReceiptDetailDto>(JsonOptions);
        Assert.NotNull(receiptDetail);
        Assert.NotEmpty(receiptDetail!.Allocations);
        Assert.Equal(chargeAmount, receiptDetail.Allocations[0].AllocatedAmount);
    }

    [Fact]
    public async Task AdvanceDeposit_Allocation_AppliesHeldFundsToOpenCharge()
    {
        var ctx = await GetFixtureContextAsync();
        using var financeClient = await CreateAuthenticatedClientAsync(
            Stage3Seeder.DefaultFinanceUsername,
            Stage3Seeder.DefaultFinancePassword);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Post payment holding 100,000 advance deposit (pure unallocated deposit)
        var depositPaymentReq = new RecordPaymentReceiptRequest(
            ctx.Student1Id,
            ctx.AcademicYearId,
            today,
            "BANK_TRANSFER",
            100000.00m,
            Allocations: [],
            BankReference: "TRX-DEP-001",
            Notes: "Titipan deposit SPP bulan depan");

        var depResp = await financeClient.PostAsJsonAsync("/api/fin/payments", depositPaymentReq);
        Assert.Equal(HttpStatusCode.Created, depResp.StatusCode);

        var depositReceipt = await depResp.Content.ReadFromJsonAsync<PaymentReceiptDto>(JsonOptions);
        Assert.NotNull(depositReceipt);
        Assert.Equal(100000.00m, depositReceipt!.UnallocatedAmount);

        // 2. Create an ad-hoc charge for 60,000
        var adhocReq = new CreateAdHocChargeRequest(
            ctx.AcademicYearId,
            ctx.Student1Id,
            ctx.UniformChargeTypeId,
            "Kaus Kaki Sekolah",
            60000.00m,
            1,
            today.AddDays(7));

        var adhocResp = await financeClient.PostAsJsonAsync("/api/fin/charges/adhoc", adhocReq);
        var adhocCharge = await adhocResp.Content.ReadFromJsonAsync<StudentChargeDto>(JsonOptions);
        Assert.NotNull(adhocCharge);

        // 3. Allocate 60,000 from the advance deposit to the ad-hoc charge -> 200 OK
        var allocReq = new AllocateAdvanceDepositRequest(
            ctx.Student1Id,
            depositReceipt.Id,
            [new PaymentAllocationItemRequest(adhocCharge!.Id, 60000.00m)],
            today);

        var allocResp = await financeClient.PostAsJsonAsync("/api/fin/deposits/allocate", allocReq);
        Assert.Equal(HttpStatusCode.OK, allocResp.StatusCode);

        var updatedReceipt = await allocResp.Content.ReadFromJsonAsync<PaymentReceiptDto>(JsonOptions);
        Assert.NotNull(updatedReceipt);
        Assert.Equal(40000.00m, updatedReceipt!.UnallocatedAmount); // 100,000 - 60,000 = 40,000 remaining
        Assert.Equal(60000.00m, updatedReceipt.AllocatedAmount);
    }

    [Fact]
    public async Task FinancialReports_StudentStatement_DailyCashier_AndReceivablesQueries()
    {
        var ctx = await GetFixtureContextAsync();
        using var financeClient = await CreateAuthenticatedClientAsync(
            Stage3Seeder.DefaultFinanceUsername,
            Stage3Seeder.DefaultFinancePassword);

        // 1. Student ledger statement query (UC-FIN-08)
        var stmtResp = await financeClient.GetAsync($"/api/fin/students/{ctx.Student1Id}/statement");
        Assert.Equal(HttpStatusCode.OK, stmtResp.StatusCode);

        var statement = await stmtResp.Content.ReadFromJsonAsync<StudentStatementDto>(JsonOptions);
        Assert.NotNull(statement);
        Assert.Equal(ctx.Student1Id, statement!.StudentId);
        Assert.NotEmpty(statement.Charges);
        Assert.NotEmpty(statement.Receipts);

        // 2. Daily cashier collection summary query (UC-FIN-11)
        var cashierResp = await financeClient.GetAsync("/api/fin/reports/daily-cashier?date=2026-07-05");
        Assert.Equal(HttpStatusCode.OK, cashierResp.StatusCode);

        var dailyCashier = await cashierResp.Content.ReadFromJsonAsync<DailyCashierSummaryDto>(JsonOptions);
        Assert.NotNull(dailyCashier);
        Assert.True(dailyCashier!.ReceiptCount >= 1);
        Assert.NotEmpty(dailyCashier.PaymentMethodSummaries);

        // 3. Receivables summary KPI metrics query (UC-FIN-09)
        var summaryResp = await financeClient.GetAsync($"/api/fin/reports/receivables-summary?academicYearId={ctx.AcademicYearId}");
        Assert.Equal(HttpStatusCode.OK, summaryResp.StatusCode);

        var summary = await summaryResp.Content.ReadFromJsonAsync<ReceivablesSummaryDto>(JsonOptions);
        Assert.NotNull(summary);
        Assert.True(summary!.TotalBilled >= 0);
        Assert.True(summary.TotalCollected >= 0);

        // 4. Receivables aging analysis query
        var agingResp = await financeClient.GetAsync($"/api/fin/reports/receivables-aging?academicYearId={ctx.AcademicYearId}");
        Assert.Equal(HttpStatusCode.OK, agingResp.StatusCode);

        var aging = await agingResp.Content.ReadFromJsonAsync<ReceivablesAgingDto>(JsonOptions);
        Assert.NotNull(aging);
        Assert.NotNull(aging!.Summary);
    }
}
