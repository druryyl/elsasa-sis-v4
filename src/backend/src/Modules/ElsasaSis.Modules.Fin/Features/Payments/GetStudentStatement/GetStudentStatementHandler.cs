using Dapper;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Fin.Features.Payments.GetStudentStatement;

public sealed class GetStudentStatementHandler(
    ISqlConnectionFactory connectionFactory,
    IStudentContract studentContract)
{
    private const string SelectChargesSql = """
        SELECT
            id AS Id,
            academic_year_id AS AcademicYearId,
            academic_year_name AS AcademicYearName,
            operational_period_id AS OperationalPeriodId,
            operational_period_name AS OperationalPeriodName,
            billing_month AS BillingMonth,
            charge_type_id AS ChargeTypeId,
            charge_type_name AS ChargeTypeName,
            charge_frequency AS ChargeFrequency,
            student_id AS StudentId,
            student_name AS StudentName,
            student_nis AS StudentNis,
            study_group_id AS StudyGroupId,
            study_group_name AS StudyGroupName,
            description AS Description,
            unit_amount AS UnitAmount,
            quantity AS Quantity,
            gross_amount AS GrossAmount,
            discount_amount AS DiscountAmount,
            discount_reason AS DiscountReason,
            net_amount AS NetAmount,
            paid_amount AS PaidAmount,
            outstanding_amount AS OutstandingAmount,
            status AS Status,
            due_date AS DueDate,
            created_at AS CreatedAt
        FROM fin.student_charges
        WHERE student_id = @StudentId
        ORDER BY created_at DESC;
        """;

    private const string SelectReceiptsSql = """
        SELECT
            id AS Id,
            receipt_number AS ReceiptNumber,
            academic_year_id AS AcademicYearId,
            academic_year_name AS AcademicYearName,
            student_id AS StudentId,
            student_name AS StudentName,
            student_nis AS StudentNis,
            study_group_id AS StudyGroupId,
            study_group_name AS StudyGroupName,
            payment_date AS PaymentDate,
            payment_method AS PaymentMethod,
            bank_reference AS BankReference,
            total_amount AS TotalAmount,
            allocated_amount AS AllocatedAmount,
            unallocated_amount AS UnallocatedAmount,
            received_by_id AS ReceivedById,
            received_by_name AS ReceivedByName,
            notes AS Notes,
            created_at AS CreatedAt
        FROM fin.payment_receipts
        WHERE student_id = @StudentId
        ORDER BY payment_date DESC, created_at DESC;
        """;

    private const string SelectAllocationsSql = """
        SELECT
            id AS Id,
            payment_receipt_id AS PaymentReceiptId,
            receipt_number AS ReceiptNumber,
            student_charge_id AS StudentChargeId,
            charge_type_name AS ChargeTypeName,
            billing_month AS BillingMonth,
            student_id AS StudentId,
            student_name AS StudentName,
            student_nis AS StudentNis,
            allocated_amount AS AllocatedAmount,
            allocation_date AS AllocationDate,
            created_at AS CreatedAt
        FROM fin.payment_allocations
        WHERE student_id = @StudentId
        ORDER BY allocation_date DESC, created_at DESC;
        """;

    public async Task<Result<StudentStatementDto>> HandleAsync(
        GetStudentStatementQuery query,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var chargesSqlCmd = new CommandDefinition(SelectChargesSql, new { query.StudentId }, cancellationToken: cancellationToken);
        var charges = (await connection.QueryAsync<ChargeRow>(chargesSqlCmd)).ToList();

        var receiptsSqlCmd = new CommandDefinition(SelectReceiptsSql, new { query.StudentId }, cancellationToken: cancellationToken);
        var receipts = (await connection.QueryAsync<ReceiptRow>(receiptsSqlCmd)).ToList();

        var allocationsSqlCmd = new CommandDefinition(SelectAllocationsSql, new { query.StudentId }, cancellationToken: cancellationToken);
        var allocations = (await connection.QueryAsync<AllocationRow>(allocationsSqlCmd)).ToList();

        string studentName = string.Empty;
        string studentNis = string.Empty;
        Guid? studyGroupId = null;
        string? studyGroupName = null;

        var firstCharge = charges.FirstOrDefault();
        if (firstCharge is not null)
        {
            studentName = firstCharge.StudentName;
            studentNis = firstCharge.StudentNis;
            studyGroupId = firstCharge.StudyGroupId;
            studyGroupName = firstCharge.StudyGroupName;
        }
        else
        {
            var firstReceipt = receipts.FirstOrDefault();
            if (firstReceipt is not null)
            {
                studentName = firstReceipt.StudentName;
                studentNis = firstReceipt.StudentNis;
                studyGroupId = firstReceipt.StudyGroupId;
                studyGroupName = firstReceipt.StudyGroupName;
            }
            else
            {
                var scopeStudents = await studentContract.GetActiveStudentsByScopeAsync(null, null, null, cancellationToken);
                var matched = scopeStudents.FirstOrDefault(s => s.StudentId == query.StudentId);
                if (matched is null)
                {
                    return Result<StudentStatementDto>.Failure(FinErrors.StudentNotFound(query.StudentId));
                }

                studentName = matched.StudentName;
                studentNis = matched.StudentNis;
                studyGroupId = matched.StudyGroupId;
                studyGroupName = matched.StudyGroupName;
            }
        }

        var activeCharges = charges.Where(c => !string.Equals(c.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase)).ToList();
        var totalBilled = activeCharges.Sum(c => c.NetAmount);
        var totalPaid = activeCharges.Sum(c => c.PaidAmount);
        var totalOutstanding = activeCharges.Sum(c => c.OutstandingAmount);
        var totalDepositBalance = receipts.Sum(r => r.UnallocatedAmount);

        var allocationsByReceiptId = allocations
            .GroupBy(a => a.PaymentReceiptId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var chargeDtos = charges.Select(c => new StudentStatementChargeDto(
            c.Id,
            c.AcademicYearId,
            c.AcademicYearName,
            c.OperationalPeriodId,
            c.OperationalPeriodName,
            c.BillingMonth,
            c.ChargeTypeId,
            c.ChargeTypeName,
            c.ChargeFrequency,
            c.Description,
            c.UnitAmount,
            c.Quantity,
            c.GrossAmount,
            c.DiscountAmount,
            c.DiscountReason,
            c.NetAmount,
            c.PaidAmount,
            c.OutstandingAmount,
            c.Status,
            c.DueDate.HasValue ? DateOnly.FromDateTime(c.DueDate.Value) : null,
            c.CreatedAt)).ToList();

        var receiptDtos = receipts.Select(r =>
        {
            var receiptAllocations = allocationsByReceiptId.TryGetValue(r.Id, out var allocs)
                ? allocs.Select(a => new StudentStatementAllocationDto(
                    a.Id,
                    a.StudentChargeId,
                    a.ChargeTypeName,
                    a.BillingMonth,
                    a.AllocatedAmount,
                    DateOnly.FromDateTime(a.AllocationDate),
                    a.CreatedAt)).ToList()
                : (IReadOnlyList<StudentStatementAllocationDto>)[];

            return new StudentStatementReceiptDto(
                r.Id,
                r.ReceiptNumber,
                r.AcademicYearId,
                r.AcademicYearName,
                DateOnly.FromDateTime(r.PaymentDate),
                r.PaymentMethod,
                r.BankReference,
                r.TotalAmount,
                r.AllocatedAmount,
                r.UnallocatedAmount,
                r.ReceivedById,
                r.ReceivedByName,
                r.Notes,
                r.CreatedAt,
                receiptAllocations);
        }).ToList();

        var statement = new StudentStatementDto(
            query.StudentId,
            studentName,
            studentNis,
            studyGroupId,
            studyGroupName,
            totalBilled,
            totalPaid,
            totalOutstanding,
            totalDepositBalance,
            chargeDtos,
            receiptDtos);

        return Result<StudentStatementDto>.Success(statement);
    }

    private sealed record ChargeRow(
        Guid Id,
        Guid AcademicYearId,
        string AcademicYearName,
        Guid? OperationalPeriodId,
        string? OperationalPeriodName,
        string? BillingMonth,
        Guid ChargeTypeId,
        string ChargeTypeName,
        string ChargeFrequency,
        Guid StudentId,
        string StudentName,
        string StudentNis,
        Guid? StudyGroupId,
        string? StudyGroupName,
        string? Description,
        decimal UnitAmount,
        int Quantity,
        decimal GrossAmount,
        decimal DiscountAmount,
        string? DiscountReason,
        decimal NetAmount,
        decimal PaidAmount,
        decimal OutstandingAmount,
        string Status,
        DateTime? DueDate,
        DateTime CreatedAt);

    private sealed record ReceiptRow(
        Guid Id,
        string ReceiptNumber,
        Guid AcademicYearId,
        string AcademicYearName,
        Guid StudentId,
        string StudentName,
        string StudentNis,
        Guid? StudyGroupId,
        string? StudyGroupName,
        DateTime PaymentDate,
        string PaymentMethod,
        string? BankReference,
        decimal TotalAmount,
        decimal AllocatedAmount,
        decimal UnallocatedAmount,
        Guid ReceivedById,
        string ReceivedByName,
        string? Notes,
        DateTime CreatedAt);

    private sealed record AllocationRow(
        Guid Id,
        Guid PaymentReceiptId,
        string ReceiptNumber,
        Guid StudentChargeId,
        string ChargeTypeName,
        string? BillingMonth,
        Guid StudentId,
        string StudentName,
        string StudentNis,
        decimal AllocatedAmount,
        DateTime AllocationDate,
        DateTime CreatedAt);
}
