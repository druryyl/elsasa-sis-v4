using System.Data;
using Dapper;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Fin.Infrastructure.Persistence;

public sealed class PaymentReceiptRepository(ISqlConnectionFactory connectionFactory) : IPaymentReceiptRepository
{
    private const string ReceiptColumns = """
        id, receipt_number, academic_year_id, academic_year_name, student_id,
        student_name, student_nis, study_group_id, study_group_name, payment_date,
        payment_method, bank_reference, total_amount, allocated_amount,
        unallocated_amount, received_by_id, received_by_name, notes,
        created_at, updated_at
        """;

    private const string AllocationColumns = """
        id, payment_receipt_id, receipt_number, student_charge_id, charge_type_name,
        billing_month, student_id, student_name, student_nis, allocated_amount,
        allocation_date, created_at, updated_at
        """;

    private const string SelectByIdSql = $"""
        SELECT {ReceiptColumns}
        FROM fin.payment_receipts
        WHERE id = @Id;

        SELECT {AllocationColumns}
        FROM fin.payment_allocations
        WHERE payment_receipt_id = @Id
        ORDER BY created_at ASC;
        """;

    private const string SelectByReceiptNumberSql = $"""
        SELECT {ReceiptColumns}
        FROM fin.payment_receipts
        WHERE receipt_number = @ReceiptNumber;

        SELECT {AllocationColumns}
        FROM fin.payment_allocations
        WHERE receipt_number = @ReceiptNumber
        ORDER BY created_at ASC;
        """;

    private const string InsertReceiptSql = """
        INSERT INTO fin.payment_receipts (
            id, receipt_number, academic_year_id, academic_year_name, student_id,
            student_name, student_nis, study_group_id, study_group_name, payment_date,
            payment_method, bank_reference, total_amount, allocated_amount,
            unallocated_amount, received_by_id, received_by_name, notes,
            created_at, updated_at
        ) VALUES (
            @Id, @ReceiptNumber, @AcademicYearId, @AcademicYearName, @StudentId,
            @StudentName, @StudentNis, @StudyGroupId, @StudyGroupName, @PaymentDate,
            @PaymentMethod, @BankReference, @TotalAmount, @AllocatedAmount,
            @UnallocatedAmount, @ReceivedById, @ReceivedByName, @Notes,
            @CreatedAt, @UpdatedAt
        );
        """;

    private const string InsertAllocationSql = """
        INSERT INTO fin.payment_allocations (
            id, payment_receipt_id, receipt_number, student_charge_id, charge_type_name,
            billing_month, student_id, student_name, student_nis, allocated_amount,
            allocation_date, created_at, updated_at
        ) VALUES (
            @Id, @PaymentReceiptId, @ReceiptNumber, @StudentChargeId, @ChargeTypeName,
            @BillingMonth, @StudentId, @StudentName, @StudentNis, @AllocatedAmount,
            @AllocationDate, @CreatedAt, @UpdatedAt
        );
        """;

    private const string UpdateReceiptSql = """
        UPDATE fin.payment_receipts
        SET allocated_amount = @AllocatedAmount,
            unallocated_amount = @UnallocatedAmount,
            notes = @Notes,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string UpdateChargeSql = """
        UPDATE fin.student_charges
        SET paid_amount = @PaidAmount,
            outstanding_amount = @OutstandingAmount,
            status = @Status,
            cancellation_reason = @CancellationReason,
            cancelled_at = @CancelledAt,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    public Task<PaymentReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, null, cancellationToken);

    public async Task<PaymentReceipt?> GetByIdAsync(
        Guid id,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        if (transaction is not null)
        {
            using var multi = await transaction.Connection!.QueryMultipleAsync(
                new CommandDefinition(SelectByIdSql, new { Id = id }, transaction: transaction, cancellationToken: cancellationToken));
            return await ReadReceiptWithAllocationsAsync(multi);
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var multiDirect = await connection.QueryMultipleAsync(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));
        return await ReadReceiptWithAllocationsAsync(multiDirect);
    }

    public Task<PaymentReceipt?> GetByReceiptNumberAsync(string receiptNumber, CancellationToken cancellationToken = default) =>
        GetByReceiptNumberAsync(receiptNumber, null, cancellationToken);

    public async Task<PaymentReceipt?> GetByReceiptNumberAsync(
        string receiptNumber,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new { ReceiptNumber = receiptNumber };

        if (transaction is not null)
        {
            using var multi = await transaction.Connection!.QueryMultipleAsync(
                new CommandDefinition(SelectByReceiptNumberSql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return await ReadReceiptWithAllocationsAsync(multi);
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var multiDirect = await connection.QueryMultipleAsync(
            new CommandDefinition(SelectByReceiptNumberSql, parameters, cancellationToken: cancellationToken));
        return await ReadReceiptWithAllocationsAsync(multiDirect);
    }

    public Task<IReadOnlyList<PaymentReceipt>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        GetByStudentIdAsync(studentId, null, cancellationToken);

    public async Task<IReadOnlyList<PaymentReceipt>> GetByStudentIdAsync(
        Guid studentId,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {ReceiptColumns}
            FROM fin.payment_receipts
            WHERE student_id = @StudentId
            ORDER BY payment_date DESC, created_at DESC;

            SELECT a.{AllocationColumns.Replace(", ", ", a.")}
            FROM fin.payment_allocations a
            INNER JOIN fin.payment_receipts r ON r.id = a.payment_receipt_id
            WHERE r.student_id = @StudentId
            ORDER BY a.created_at ASC;
            """;

        var parameters = new { StudentId = studentId };

        if (transaction is not null)
        {
            using var multi = await transaction.Connection!.QueryMultipleAsync(
                new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return await ReadMultipleReceiptsWithAllocationsAsync(multi);
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var multiDirect = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return await ReadMultipleReceiptsWithAllocationsAsync(multiDirect);
    }

    public Task<IReadOnlyList<PaymentReceipt>> GetByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default) =>
        GetByDateRangeAsync(startDate, endDate, null, cancellationToken);

    public async Task<IReadOnlyList<PaymentReceipt>> GetByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {ReceiptColumns}
            FROM fin.payment_receipts
            WHERE payment_date >= @StartDate AND payment_date <= @EndDate
            ORDER BY payment_date ASC, created_at ASC;

            SELECT a.{AllocationColumns.Replace(", ", ", a.")}
            FROM fin.payment_allocations a
            INNER JOIN fin.payment_receipts r ON r.id = a.payment_receipt_id
            WHERE r.payment_date >= @StartDate AND r.payment_date <= @EndDate
            ORDER BY a.created_at ASC;
            """;

        var parameters = new
        {
            StartDate = startDate.ToDateTime(TimeOnly.MinValue),
            EndDate = endDate.ToDateTime(TimeOnly.MinValue)
        };

        if (transaction is not null)
        {
            using var multi = await transaction.Connection!.QueryMultipleAsync(
                new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return await ReadMultipleReceiptsWithAllocationsAsync(multi);
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var multiDirect = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return await ReadMultipleReceiptsWithAllocationsAsync(multiDirect);
    }

    public Task InsertAsync(PaymentReceipt receipt, CancellationToken cancellationToken = default) =>
        InsertAsync(receipt, null, cancellationToken);

    public async Task InsertAsync(
        PaymentReceipt receipt,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var receiptParameters = new
        {
            receipt.Id,
            receipt.ReceiptNumber,
            receipt.AcademicYearId,
            receipt.AcademicYearName,
            receipt.StudentId,
            receipt.StudentName,
            receipt.StudentNis,
            receipt.StudyGroupId,
            receipt.StudyGroupName,
            PaymentDate = receipt.PaymentDate.ToDateTime(TimeOnly.MinValue),
            receipt.PaymentMethod,
            receipt.BankReference,
            receipt.TotalAmount,
            receipt.AllocatedAmount,
            receipt.UnallocatedAmount,
            receipt.ReceivedById,
            receipt.ReceivedByName,
            receipt.Notes,
            receipt.CreatedAt,
            receipt.UpdatedAt
        };

        var allocationParameters = receipt.Allocations.Select(a => new
        {
            a.Id,
            a.PaymentReceiptId,
            a.ReceiptNumber,
            a.StudentChargeId,
            a.ChargeTypeName,
            a.BillingMonth,
            a.StudentId,
            a.StudentName,
            a.StudentNis,
            a.AllocatedAmount,
            AllocationDate = a.AllocationDate.ToDateTime(TimeOnly.MinValue),
            a.CreatedAt,
            a.UpdatedAt
        }).ToList();

        if (transaction is not null)
        {
            await transaction.Connection!.ExecuteAsync(
                new CommandDefinition(InsertReceiptSql, receiptParameters, transaction: transaction, cancellationToken: cancellationToken));

            if (allocationParameters.Count > 0)
            {
                await transaction.Connection!.ExecuteAsync(
                    new CommandDefinition(InsertAllocationSql, allocationParameters, transaction: transaction, cancellationToken: cancellationToken));
            }
            return;
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(InsertReceiptSql, receiptParameters, cancellationToken: cancellationToken));

        if (allocationParameters.Count > 0)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(InsertAllocationSql, allocationParameters, cancellationToken: cancellationToken));
        }
    }

    public Task UpdateAsync(PaymentReceipt receipt, CancellationToken cancellationToken = default) =>
        UpdateAsync(receipt, null, cancellationToken);

    public async Task UpdateAsync(
        PaymentReceipt receipt,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            receipt.Id,
            receipt.AllocatedAmount,
            receipt.UnallocatedAmount,
            receipt.Notes,
            receipt.UpdatedAt
        };

        var allocationParameters = receipt.Allocations.Select(a => new
        {
            a.Id,
            a.PaymentReceiptId,
            a.ReceiptNumber,
            a.StudentChargeId,
            a.ChargeTypeName,
            a.BillingMonth,
            a.StudentId,
            a.StudentName,
            a.StudentNis,
            a.AllocatedAmount,
            AllocationDate = a.AllocationDate.ToDateTime(TimeOnly.MinValue),
            a.CreatedAt,
            a.UpdatedAt
        }).ToList();

        const string insertMissingAllocationSql = """
            IF NOT EXISTS (SELECT 1 FROM fin.payment_allocations WHERE id = @Id)
            BEGIN
                INSERT INTO fin.payment_allocations (
                    id, payment_receipt_id, receipt_number, student_charge_id, charge_type_name,
                    billing_month, student_id, student_name, student_nis, allocated_amount,
                    allocation_date, created_at, updated_at
                ) VALUES (
                    @Id, @PaymentReceiptId, @ReceiptNumber, @StudentChargeId, @ChargeTypeName,
                    @BillingMonth, @StudentId, @StudentName, @StudentNis, @AllocatedAmount,
                    @AllocationDate, @CreatedAt, @UpdatedAt
                );
            END;
            """;

        if (transaction is not null)
        {
            await transaction.Connection!.ExecuteAsync(
                new CommandDefinition(UpdateReceiptSql, parameters, transaction: transaction, cancellationToken: cancellationToken));

            if (allocationParameters.Count > 0)
            {
                await transaction.Connection!.ExecuteAsync(
                    new CommandDefinition(insertMissingAllocationSql, allocationParameters, transaction: transaction, cancellationToken: cancellationToken));
            }
            return;
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(UpdateReceiptSql, parameters, cancellationToken: cancellationToken));

        if (allocationParameters.Count > 0)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(insertMissingAllocationSql, allocationParameters, cancellationToken: cancellationToken));
        }
    }

    public async Task SavePaymentReceiptTransactionAsync(
        PaymentReceipt receipt,
        IReadOnlyList<StudentCharge> updatedCharges,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (connection is System.Data.Common.DbConnection dbConnection)
        {
            await using var transaction = await dbConnection.BeginTransactionAsync(cancellationToken);
            try
            {
                await InsertAsync(receipt, transaction, cancellationToken);
                await UpdateChargesInternalAsync(connection, transaction, updatedCharges, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        else
        {
            using var transaction = connection.BeginTransaction();
            try
            {
                await InsertAsync(receipt, transaction, cancellationToken);
                await UpdateChargesInternalAsync(connection, transaction, updatedCharges, cancellationToken);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public async Task SaveDepositAllocationTransactionAsync(
        PaymentReceipt sourceReceipt,
        IReadOnlyList<StudentCharge> updatedCharges,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (connection is System.Data.Common.DbConnection dbConnection)
        {
            await using var transaction = await dbConnection.BeginTransactionAsync(cancellationToken);
            try
            {
                await UpdateAsync(sourceReceipt, transaction, cancellationToken);
                await UpdateChargesInternalAsync(connection, transaction, updatedCharges, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        else
        {
            using var transaction = connection.BeginTransaction();
            try
            {
                await UpdateAsync(sourceReceipt, transaction, cancellationToken);
                await UpdateChargesInternalAsync(connection, transaction, updatedCharges, cancellationToken);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    private static async Task UpdateChargesInternalAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        IReadOnlyList<StudentCharge> updatedCharges,
        CancellationToken cancellationToken)
    {
        if (updatedCharges.Count == 0)
        {
            return;
        }

        var parameters = updatedCharges.Select(c => new
        {
            c.Id,
            c.PaidAmount,
            c.OutstandingAmount,
            c.Status,
            c.CancellationReason,
            c.CancelledAt,
            c.UpdatedAt
        }).ToList();

        await connection.ExecuteAsync(
            new CommandDefinition(UpdateChargeSql, parameters, transaction: transaction, cancellationToken: cancellationToken));
    }

    private static async Task<PaymentReceipt?> ReadReceiptWithAllocationsAsync(SqlMapper.GridReader multi)
    {
        var receiptRow = await multi.ReadSingleOrDefaultAsync<PaymentReceiptRow>();
        if (receiptRow is null)
        {
            return null;
        }

        var allocationRows = (await multi.ReadAsync<PaymentAllocationRow>()).ToList();
        var allocations = allocationRows.Select(MapAllocationRow).ToList();

        return MapReceiptRow(receiptRow, allocations);
    }

    private static async Task<IReadOnlyList<PaymentReceipt>> ReadMultipleReceiptsWithAllocationsAsync(SqlMapper.GridReader multi)
    {
        var receiptRows = (await multi.ReadAsync<PaymentReceiptRow>()).ToList();
        var allocationRows = (await multi.ReadAsync<PaymentAllocationRow>()).ToList();

        var allocationsByReceiptId = allocationRows
            .Select(MapAllocationRow)
            .GroupBy(a => a.PaymentReceiptId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return receiptRows
            .Select(r => MapReceiptRow(r, allocationsByReceiptId.TryGetValue(r.id, out var list) ? list : []))
            .ToList();
    }

    private static PaymentReceipt MapReceiptRow(PaymentReceiptRow row, IEnumerable<PaymentAllocation> allocations) =>
        PaymentReceipt.Rehydrate(
            row.id,
            row.receipt_number,
            row.academic_year_id,
            row.academic_year_name,
            row.student_id,
            row.student_name,
            row.student_nis,
            row.study_group_id,
            row.study_group_name,
            DateOnly.FromDateTime(row.payment_date),
            row.payment_method,
            row.bank_reference,
            row.total_amount,
            row.allocated_amount,
            row.unallocated_amount,
            row.received_by_id,
            row.received_by_name,
            row.notes,
            row.created_at,
            row.updated_at,
            allocations);

    private static PaymentAllocation MapAllocationRow(PaymentAllocationRow row) =>
        PaymentAllocation.Rehydrate(
            row.id,
            row.payment_receipt_id,
            row.receipt_number,
            row.student_charge_id,
            row.charge_type_name,
            row.billing_month,
            row.student_id,
            row.student_name,
            row.student_nis,
            row.allocated_amount,
            DateOnly.FromDateTime(row.allocation_date),
            row.created_at,
            row.updated_at);

    private sealed record PaymentReceiptRow(
        Guid id,
        string receipt_number,
        Guid academic_year_id,
        string academic_year_name,
        Guid student_id,
        string student_name,
        string student_nis,
        Guid? study_group_id,
        string? study_group_name,
        DateTime payment_date,
        string payment_method,
        string? bank_reference,
        decimal total_amount,
        decimal allocated_amount,
        decimal unallocated_amount,
        Guid received_by_id,
        string received_by_name,
        string? notes,
        DateTime created_at,
        DateTime updated_at);

    private sealed record PaymentAllocationRow(
        Guid id,
        Guid payment_receipt_id,
        string receipt_number,
        Guid student_charge_id,
        string charge_type_name,
        string? billing_month,
        Guid student_id,
        string student_name,
        string student_nis,
        decimal allocated_amount,
        DateTime allocation_date,
        DateTime created_at,
        DateTime updated_at);
}
