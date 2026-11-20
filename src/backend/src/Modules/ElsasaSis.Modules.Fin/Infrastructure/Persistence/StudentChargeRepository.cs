using System.Data;
using Dapper;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Fin.Infrastructure.Persistence;

public sealed class StudentChargeRepository(ISqlConnectionFactory connectionFactory) : IStudentChargeRepository
{
    private const string SelectColumns = """
        id, academic_year_id, academic_year_name, operational_period_id, operational_period_name,
        billing_month, charge_type_id, charge_type_name, charge_frequency,
        student_id, student_name, student_nis, school_level, grade,
        study_group_id, study_group_name, description, unit_amount, quantity,
        gross_amount, discount_amount, discount_reason, net_amount, paid_amount,
        outstanding_amount, status, due_date, cancellation_reason, cancelled_at,
        created_at, updated_at
        """;

    private const string SelectByIdSql = $"""
        SELECT {SelectColumns}
        FROM fin.student_charges
        WHERE id = @Id;
        """;

    private const string SelectByIdsSql = $"""
        SELECT {SelectColumns}
        FROM fin.student_charges
        WHERE id IN @Ids;
        """;

    private const string ExistsMonthlySql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM fin.student_charges
            WHERE student_id = @StudentId
              AND charge_type_id = @ChargeTypeId
              AND billing_month = @BillingMonth
              AND status <> 'CANCELLED'
        ) THEN 1 ELSE 0 END;
        """;

    private const string ExistsYearlySql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM fin.student_charges
            WHERE student_id = @StudentId
              AND charge_type_id = @ChargeTypeId
              AND academic_year_id = @AcademicYearId
              AND charge_frequency = 'YEARLY'
              AND status <> 'CANCELLED'
        ) THEN 1 ELSE 0 END;
        """;

    private const string ExistsSemesterSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM fin.student_charges
            WHERE student_id = @StudentId
              AND charge_type_id = @ChargeTypeId
              AND operational_period_id = @OperationalPeriodId
              AND charge_frequency = 'SEMESTER'
              AND status <> 'CANCELLED'
        ) THEN 1 ELSE 0 END;
        """;

    private const string InsertSql = """
        INSERT INTO fin.student_charges (
            id, academic_year_id, academic_year_name, operational_period_id, operational_period_name,
            billing_month, charge_type_id, charge_type_name, charge_frequency,
            student_id, student_name, student_nis, school_level, grade,
            study_group_id, study_group_name, description, unit_amount, quantity,
            gross_amount, discount_amount, discount_reason, net_amount, paid_amount,
            outstanding_amount, status, due_date, cancellation_reason, cancelled_at,
            created_at, updated_at
        ) VALUES (
            @Id, @AcademicYearId, @AcademicYearName, @OperationalPeriodId, @OperationalPeriodName,
            @BillingMonth, @ChargeTypeId, @ChargeTypeName, @ChargeFrequency,
            @StudentId, @StudentName, @StudentNis, @SchoolLevel, @Grade,
            @StudyGroupId, @StudyGroupName, @Description, @UnitAmount, @Quantity,
            @GrossAmount, @DiscountAmount, @DiscountReason, @NetAmount, @PaidAmount,
            @OutstandingAmount, @Status, @DueDate, @CancellationReason, @CancelledAt,
            @CreatedAt, @UpdatedAt
        );
        """;

    private const string UpdateSql = """
        UPDATE fin.student_charges
        SET paid_amount = @PaidAmount,
            outstanding_amount = @OutstandingAmount,
            status = @Status,
            cancellation_reason = @CancellationReason,
            cancelled_at = @CancelledAt,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    public Task<StudentCharge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, null, cancellationToken);

    public async Task<StudentCharge?> GetByIdAsync(
        Guid id,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        if (transaction is not null)
        {
            var row = await transaction.Connection!.QuerySingleOrDefaultAsync<StudentChargeRow>(
                new CommandDefinition(SelectByIdSql, new { Id = id }, transaction: transaction, cancellationToken: cancellationToken));
            return row is null ? null : MapRow(row);
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var directRow = await connection.QuerySingleOrDefaultAsync<StudentChargeRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));
        return directRow is null ? null : MapRow(directRow);
    }

    public Task<IReadOnlyList<StudentCharge>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default) =>
        GetByIdsAsync(ids, null, cancellationToken);

    public async Task<IReadOnlyList<StudentCharge>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return Array.Empty<StudentCharge>();
        }

        if (transaction is not null)
        {
            var rows = await transaction.Connection!.QueryAsync<StudentChargeRow>(
                new CommandDefinition(SelectByIdsSql, new { Ids = idList }, transaction: transaction, cancellationToken: cancellationToken));
            return rows.Select(MapRow).ToList();
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var directRows = await connection.QueryAsync<StudentChargeRow>(
            new CommandDefinition(SelectByIdsSql, new { Ids = idList }, cancellationToken: cancellationToken));
        return directRows.Select(MapRow).ToList();
    }

    public Task<IReadOnlyList<StudentCharge>> GetByStudentIdAsync(
        Guid studentId,
        string? status = null,
        CancellationToken cancellationToken = default) =>
        GetByStudentIdAsync(studentId, status, null, cancellationToken);

    public async Task<IReadOnlyList<StudentCharge>> GetByStudentIdAsync(
        Guid studentId,
        string? status,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM fin.student_charges
            WHERE student_id = @StudentId
              AND (@Status IS NULL OR status = @Status)
            ORDER BY created_at DESC;
            """;

        var parameters = new { StudentId = studentId, Status = status };

        if (transaction is not null)
        {
            var rows = await transaction.Connection!.QueryAsync<StudentChargeRow>(
                new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return rows.Select(MapRow).ToList();
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var directRows = await connection.QueryAsync<StudentChargeRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return directRows.Select(MapRow).ToList();
    }

    public Task<IReadOnlyList<StudentCharge>> GetUnpaidOrPartiallyPaidByStudentIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        GetUnpaidOrPartiallyPaidByStudentIdAsync(studentId, null, cancellationToken);

    public async Task<IReadOnlyList<StudentCharge>> GetUnpaidOrPartiallyPaidByStudentIdAsync(
        Guid studentId,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM fin.student_charges
            WHERE student_id = @StudentId
              AND status IN ('UNPAID', 'PARTIALLY_PAID')
            ORDER BY due_date ASC, created_at ASC;
            """;

        var parameters = new { StudentId = studentId };

        if (transaction is not null)
        {
            var rows = await transaction.Connection!.QueryAsync<StudentChargeRow>(
                new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return rows.Select(MapRow).ToList();
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var directRows = await connection.QueryAsync<StudentChargeRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return directRows.Select(MapRow).ToList();
    }

    public Task<IReadOnlyList<StudentCharge>> GetChargesAsync(
        Guid? academicYearId = null,
        string? billingMonth = null,
        string? schoolLevel = null,
        string? grade = null,
        Guid? studyGroupId = null,
        Guid? studentId = null,
        string? status = null,
        CancellationToken cancellationToken = default) =>
        GetChargesAsync(academicYearId, billingMonth, schoolLevel, grade, studyGroupId, studentId, status, null, cancellationToken);

    public async Task<IReadOnlyList<StudentCharge>> GetChargesAsync(
        Guid? academicYearId,
        string? billingMonth,
        string? schoolLevel,
        string? grade,
        Guid? studyGroupId,
        Guid? studentId,
        string? status,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM fin.student_charges
            WHERE (@AcademicYearId IS NULL OR academic_year_id = @AcademicYearId)
              AND (@BillingMonth IS NULL OR billing_month = @BillingMonth)
              AND (@SchoolLevel IS NULL OR school_level = @SchoolLevel)
              AND (@Grade IS NULL OR grade = @Grade)
              AND (@StudyGroupId IS NULL OR study_group_id = @StudyGroupId)
              AND (@StudentId IS NULL OR student_id = @StudentId)
              AND (@Status IS NULL OR status = @Status)
            ORDER BY created_at DESC;
            """;

        var parameters = new
        {
            AcademicYearId = academicYearId,
            BillingMonth = billingMonth,
            SchoolLevel = schoolLevel,
            Grade = grade,
            StudyGroupId = studyGroupId,
            StudentId = studentId,
            Status = status
        };

        if (transaction is not null)
        {
            var rows = await transaction.Connection!.QueryAsync<StudentChargeRow>(
                new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return rows.Select(MapRow).ToList();
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var directRows = await connection.QueryAsync<StudentChargeRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return directRows.Select(MapRow).ToList();
    }

    public Task<bool> ExistsMonthlyChargeAsync(
        Guid studentId,
        Guid chargeTypeId,
        string billingMonth,
        CancellationToken cancellationToken = default) =>
        ExistsMonthlyChargeAsync(studentId, chargeTypeId, billingMonth, null, cancellationToken);

    public async Task<bool> ExistsMonthlyChargeAsync(
        Guid studentId,
        Guid chargeTypeId,
        string billingMonth,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            StudentId = studentId,
            ChargeTypeId = chargeTypeId,
            BillingMonth = billingMonth
        };

        if (transaction is not null)
        {
            return await transaction.Connection!.ExecuteScalarAsync<bool>(
                new CommandDefinition(ExistsMonthlySql, parameters, transaction: transaction, cancellationToken: cancellationToken));
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(ExistsMonthlySql, parameters, cancellationToken: cancellationToken));
    }

    public Task<bool> ExistsYearlyChargeAsync(
        Guid studentId,
        Guid chargeTypeId,
        Guid academicYearId,
        CancellationToken cancellationToken = default) =>
        ExistsYearlyChargeAsync(studentId, chargeTypeId, academicYearId, null, cancellationToken);

    public async Task<bool> ExistsYearlyChargeAsync(
        Guid studentId,
        Guid chargeTypeId,
        Guid academicYearId,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            StudentId = studentId,
            ChargeTypeId = chargeTypeId,
            AcademicYearId = academicYearId
        };

        if (transaction is not null)
        {
            return await transaction.Connection!.ExecuteScalarAsync<bool>(
                new CommandDefinition(ExistsYearlySql, parameters, transaction: transaction, cancellationToken: cancellationToken));
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(ExistsYearlySql, parameters, cancellationToken: cancellationToken));
    }

    public Task<bool> ExistsSemesterChargeAsync(
        Guid studentId,
        Guid chargeTypeId,
        Guid operationalPeriodId,
        CancellationToken cancellationToken = default) =>
        ExistsSemesterChargeAsync(studentId, chargeTypeId, operationalPeriodId, null, cancellationToken);

    public async Task<bool> ExistsSemesterChargeAsync(
        Guid studentId,
        Guid chargeTypeId,
        Guid operationalPeriodId,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            StudentId = studentId,
            ChargeTypeId = chargeTypeId,
            OperationalPeriodId = operationalPeriodId
        };

        if (transaction is not null)
        {
            return await transaction.Connection!.ExecuteScalarAsync<bool>(
                new CommandDefinition(ExistsSemesterSql, parameters, transaction: transaction, cancellationToken: cancellationToken));
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(ExistsSemesterSql, parameters, cancellationToken: cancellationToken));
    }

    public Task InsertAsync(StudentCharge charge, CancellationToken cancellationToken = default) =>
        InsertAsync(charge, null, cancellationToken);

    public async Task InsertAsync(
        StudentCharge charge,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = ToParameters(charge);

        if (transaction is not null)
        {
            await transaction.Connection!.ExecuteAsync(
                new CommandDefinition(InsertSql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return;
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(InsertSql, parameters, cancellationToken: cancellationToken));
    }

    public Task InsertBatchAsync(
        IEnumerable<StudentCharge> charges,
        CancellationToken cancellationToken = default) =>
        InsertBatchAsync(charges, null, cancellationToken);

    public async Task InsertBatchAsync(
        IEnumerable<StudentCharge> charges,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var parameterList = charges.Select(ToParameters).ToList();
        if (parameterList.Count == 0)
        {
            return;
        }

        if (transaction is not null)
        {
            await transaction.Connection!.ExecuteAsync(
                new CommandDefinition(InsertSql, parameterList, transaction: transaction, cancellationToken: cancellationToken));
            return;
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(InsertSql, parameterList, cancellationToken: cancellationToken));
    }

    public Task UpdateAsync(StudentCharge charge, CancellationToken cancellationToken = default) =>
        UpdateAsync(charge, null, cancellationToken);

    public async Task UpdateAsync(
        StudentCharge charge,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            charge.Id,
            charge.PaidAmount,
            charge.OutstandingAmount,
            charge.Status,
            charge.CancellationReason,
            charge.CancelledAt,
            charge.UpdatedAt
        };

        if (transaction is not null)
        {
            await transaction.Connection!.ExecuteAsync(
                new CommandDefinition(UpdateSql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return;
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(UpdateSql, parameters, cancellationToken: cancellationToken));
    }

    private static object ToParameters(StudentCharge charge) => new
    {
        charge.Id,
        charge.AcademicYearId,
        charge.AcademicYearName,
        charge.OperationalPeriodId,
        charge.OperationalPeriodName,
        charge.BillingMonth,
        charge.ChargeTypeId,
        charge.ChargeTypeName,
        charge.ChargeFrequency,
        charge.StudentId,
        charge.StudentName,
        charge.StudentNis,
        charge.SchoolLevel,
        charge.Grade,
        charge.StudyGroupId,
        charge.StudyGroupName,
        charge.Description,
        charge.UnitAmount,
        charge.Quantity,
        charge.GrossAmount,
        charge.DiscountAmount,
        charge.DiscountReason,
        charge.NetAmount,
        charge.PaidAmount,
        charge.OutstandingAmount,
        charge.Status,
        DueDate = charge.DueDate.HasValue ? charge.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
        charge.CancellationReason,
        charge.CancelledAt,
        charge.CreatedAt,
        charge.UpdatedAt
    };

    private static StudentCharge MapRow(StudentChargeRow row) =>
        StudentCharge.Rehydrate(
            row.id,
            row.academic_year_id,
            row.academic_year_name,
            row.operational_period_id,
            row.operational_period_name,
            row.billing_month,
            row.charge_type_id,
            row.charge_type_name,
            row.charge_frequency,
            row.student_id,
            row.student_name,
            row.student_nis,
            row.school_level,
            row.grade,
            row.study_group_id,
            row.study_group_name,
            row.description,
            row.unit_amount,
            row.quantity,
            row.gross_amount,
            row.discount_amount,
            row.discount_reason,
            row.net_amount,
            row.paid_amount,
            row.outstanding_amount,
            row.status,
            row.due_date.HasValue ? DateOnly.FromDateTime(row.due_date.Value) : null,
            row.cancellation_reason,
            row.cancelled_at,
            row.created_at,
            row.updated_at);

    private sealed record StudentChargeRow(
        Guid id,
        Guid academic_year_id,
        string academic_year_name,
        Guid? operational_period_id,
        string? operational_period_name,
        string? billing_month,
        Guid charge_type_id,
        string charge_type_name,
        string charge_frequency,
        Guid student_id,
        string student_name,
        string student_nis,
        string school_level,
        string grade,
        Guid? study_group_id,
        string? study_group_name,
        string? description,
        decimal unit_amount,
        int quantity,
        decimal gross_amount,
        decimal discount_amount,
        string? discount_reason,
        decimal net_amount,
        decimal paid_amount,
        decimal outstanding_amount,
        string status,
        DateTime? due_date,
        string? cancellation_reason,
        DateTime? cancelled_at,
        DateTime created_at,
        DateTime updated_at);
}
