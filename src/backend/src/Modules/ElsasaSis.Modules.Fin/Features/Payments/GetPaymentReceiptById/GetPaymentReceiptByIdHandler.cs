using Dapper;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Fin.Features.Payments.GetPaymentReceiptById;

public sealed class GetPaymentReceiptByIdHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string CheckChildOwnershipSql = """
        SELECT 1
        FROM auth.user_students us
        WHERE us.user_id = @UserId
          AND us.student_id = @StudentId;
        """;

    private const string SelectReceiptSql = """
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
        WHERE id = @ReceiptId;

        SELECT
            id AS Id,
            student_charge_id AS StudentChargeId,
            charge_type_name AS ChargeTypeName,
            billing_month AS BillingMonth,
            allocated_amount AS AllocatedAmount,
            allocation_date AS AllocationDate,
            created_at AS CreatedAt
        FROM fin.payment_allocations
        WHERE payment_receipt_id = @ReceiptId
        ORDER BY created_at ASC;
        """;

    public async Task<Result<PaymentReceiptDetailDto>> HandleAsync(
        GetPaymentReceiptByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(SelectReceiptSql, new { query.ReceiptId }, cancellationToken: cancellationToken));

        var receiptRow = await multi.ReadSingleOrDefaultAsync<ReceiptRow>();
        if (receiptRow is null)
        {
            return Result<PaymentReceiptDetailDto>.Failure(
                FinErrors.PaymentReceiptNotFound(query.ReceiptId));
        }

        var allocationRows = (await multi.ReadAsync<AllocationRow>()).ToList();

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
                    new { UserId = currentUser.UserId, StudentId = receiptRow.StudentId },
                    cancellationToken: cancellationToken));

            if (!isAuthorized.HasValue)
            {
                return Result<PaymentReceiptDetailDto>.Failure(
                    FinErrors.Forbidden("You do not have permission to view this payment receipt."));
            }
        }

        var allocations = allocationRows.Select(a => new PaymentReceiptAllocationDetailDto(
            a.Id,
            a.StudentChargeId,
            a.ChargeTypeName,
            a.BillingMonth,
            a.AllocatedAmount,
            DateOnly.FromDateTime(a.AllocationDate),
            a.CreatedAt)).ToList();

        var detail = new PaymentReceiptDetailDto(
            receiptRow.Id,
            receiptRow.ReceiptNumber,
            receiptRow.AcademicYearId,
            receiptRow.AcademicYearName,
            receiptRow.StudentId,
            receiptRow.StudentName,
            receiptRow.StudentNis,
            receiptRow.StudyGroupId,
            receiptRow.StudyGroupName,
            DateOnly.FromDateTime(receiptRow.PaymentDate),
            receiptRow.PaymentMethod,
            receiptRow.BankReference,
            receiptRow.TotalAmount,
            receiptRow.AllocatedAmount,
            receiptRow.UnallocatedAmount,
            receiptRow.ReceivedById,
            receiptRow.ReceivedByName,
            receiptRow.Notes,
            receiptRow.CreatedAt,
            allocations);

        return Result<PaymentReceiptDetailDto>.Success(detail);
    }

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
        Guid StudentChargeId,
        string ChargeTypeName,
        string? BillingMonth,
        decimal AllocatedAmount,
        DateTime AllocationDate,
        DateTime CreatedAt);
}
