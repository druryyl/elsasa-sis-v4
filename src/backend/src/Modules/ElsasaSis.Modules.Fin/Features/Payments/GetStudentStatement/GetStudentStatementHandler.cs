using Dapper;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Fin.Features.Payments.GetStudentStatement;

public sealed class GetStudentStatementHandler(
    ISqlConnectionFactory connectionFactory,
    IStudentContract studentContract,
    ICurrentUser currentUser)
{
    private const string CheckChildOwnershipSql = """
        SELECT 1
        FROM auth.user_students
        WHERE user_id = @UserId
          AND student_id = @StudentId;
        """;

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

        var isParent = currentUser.Roles.Any(r => string.Equals(r, "PARENT", StringComparison.OrdinalIgnoreCase));
        var isElevatedStaff = currentUser.Roles.Any(r =>
            string.Equals(r, "FINANCE_OFFICER", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "SCHOOL_ADMIN", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "PRINCIPAL", StringComparison.OrdinalIgnoreCase));

        if (isParent && !isElevatedStaff)
        {
            var isAuthorized = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    CheckChildOwnershipSql,
                    new { UserId = currentUser.UserId, StudentId = query.StudentId },
                    cancellationToken: cancellationToken));

            if (!isAuthorized.HasValue)
            {
                return Result<StudentStatementDto>.Failure(
                    FinErrors.Forbidden("You do not have permission to view financial statements for this student."));
            }
        }

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

    private sealed class ReceiptRow
    {
        public Guid Id { get; init; }
        public string ReceiptNumber { get; init; } = string.Empty;
        public Guid AcademicYearId { get; init; }
        public string AcademicYearName { get; init; } = string.Empty;
        public Guid StudentId { get; init; }
        public string StudentName { get; init; } = string.Empty;
        public string StudentNis { get; init; } = string.Empty;
        public Guid? StudyGroupId { get; init; }
        public string? StudyGroupName { get; init; }
        public DateTime PaymentDate { get; init; }
        public string PaymentMethod { get; init; } = string.Empty;
        public string? BankReference { get; init; }
        public decimal TotalAmount { get; init; }
        public decimal AllocatedAmount { get; init; }
        public decimal UnallocatedAmount { get; init; }
        public Guid ReceivedById { get; init; }
        public string ReceivedByName { get; init; } = string.Empty;
        public string? Notes { get; init; }
        public DateTime CreatedAt { get; init; }
    }

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
