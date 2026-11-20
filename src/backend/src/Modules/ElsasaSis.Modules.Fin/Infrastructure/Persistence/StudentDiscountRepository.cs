using System.Data;
using Dapper;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Fin.Infrastructure.Persistence;

public sealed class StudentDiscountRepository(ISqlConnectionFactory connectionFactory) : IStudentDiscountRepository
{
    private const string SelectColumns = """
        id, academic_year_id, academic_year_name, charge_type_id, charge_type_name,
        student_id, student_name, student_nis, discount_type, discount_value,
        reason, is_active, created_at, updated_at
        """;

    private const string SelectByIdSql = $"""
        SELECT {SelectColumns}
        FROM fin.student_discounts
        WHERE id = @Id;
        """;

    private const string SelectActiveDiscountSql = $"""
        SELECT {SelectColumns}
        FROM fin.student_discounts
        WHERE student_id = @StudentId
          AND charge_type_id = @ChargeTypeId
          AND academic_year_id = @AcademicYearId
          AND is_active = 1;
        """;

    private const string ExistsForStudentAndChargeTypeSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM fin.student_discounts
            WHERE student_id = @StudentId
              AND charge_type_id = @ChargeTypeId
              AND academic_year_id = @AcademicYearId
              AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string InsertSql = """
        INSERT INTO fin.student_discounts (
            id, academic_year_id, academic_year_name, charge_type_id, charge_type_name,
            student_id, student_name, student_nis, discount_type, discount_value,
            reason, is_active, created_at, updated_at
        ) VALUES (
            @Id, @AcademicYearId, @AcademicYearName, @ChargeTypeId, @ChargeTypeName,
            @StudentId, @StudentName, @StudentNis, @DiscountType, @DiscountValue,
            @Reason, @IsActive, @CreatedAt, @UpdatedAt
        );
        """;

    private const string UpdateSql = """
        UPDATE fin.student_discounts
        SET is_active = @IsActive,
            reason = @Reason,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    public Task<StudentDiscount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, null, cancellationToken);

    public async Task<StudentDiscount?> GetByIdAsync(
        Guid id,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        if (transaction is not null)
        {
            var row = await transaction.Connection!.QuerySingleOrDefaultAsync<StudentDiscountRow>(
                new CommandDefinition(SelectByIdSql, new { Id = id }, transaction: transaction, cancellationToken: cancellationToken));
            return row is null ? null : MapRow(row);
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var directRow = await connection.QuerySingleOrDefaultAsync<StudentDiscountRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));
        return directRow is null ? null : MapRow(directRow);
    }

    public Task<IReadOnlyList<StudentDiscount>> GetByStudentIdAsync(
        Guid studentId,
        Guid academicYearId,
        CancellationToken cancellationToken = default) =>
        GetByStudentIdAsync(studentId, academicYearId, null, cancellationToken);

    public async Task<IReadOnlyList<StudentDiscount>> GetByStudentIdAsync(
        Guid studentId,
        Guid academicYearId,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM fin.student_discounts
            WHERE student_id = @StudentId
              AND academic_year_id = @AcademicYearId
            ORDER BY created_at DESC;
            """;

        var parameters = new { StudentId = studentId, AcademicYearId = academicYearId };

        if (transaction is not null)
        {
            var rows = await transaction.Connection!.QueryAsync<StudentDiscountRow>(
                new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return rows.Select(MapRow).ToList();
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var directRows = await connection.QueryAsync<StudentDiscountRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return directRows.Select(MapRow).ToList();
    }

    public Task<StudentDiscount?> GetActiveDiscountAsync(
        Guid studentId,
        Guid chargeTypeId,
        Guid academicYearId,
        CancellationToken cancellationToken = default) =>
        GetActiveDiscountAsync(studentId, chargeTypeId, academicYearId, null, cancellationToken);

    public async Task<StudentDiscount?> GetActiveDiscountAsync(
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
            var row = await transaction.Connection!.QuerySingleOrDefaultAsync<StudentDiscountRow>(
                new CommandDefinition(SelectActiveDiscountSql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return row is null ? null : MapRow(row);
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var directRow = await connection.QuerySingleOrDefaultAsync<StudentDiscountRow>(
            new CommandDefinition(SelectActiveDiscountSql, parameters, cancellationToken: cancellationToken));
        return directRow is null ? null : MapRow(directRow);
    }

    public Task<bool> ExistsForStudentAndChargeTypeAsync(
        Guid studentId,
        Guid chargeTypeId,
        Guid academicYearId,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        ExistsForStudentAndChargeTypeAsync(studentId, chargeTypeId, academicYearId, excludeId, null, cancellationToken);

    public async Task<bool> ExistsForStudentAndChargeTypeAsync(
        Guid studentId,
        Guid chargeTypeId,
        Guid academicYearId,
        Guid? excludeId,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            StudentId = studentId,
            ChargeTypeId = chargeTypeId,
            AcademicYearId = academicYearId,
            ExcludeId = excludeId
        };

        if (transaction is not null)
        {
            return await transaction.Connection!.ExecuteScalarAsync<bool>(
                new CommandDefinition(ExistsForStudentAndChargeTypeSql, parameters, transaction: transaction, cancellationToken: cancellationToken));
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(ExistsForStudentAndChargeTypeSql, parameters, cancellationToken: cancellationToken));
    }

    public Task<IReadOnlyList<StudentDiscount>> GetByChargeTypeIdAsync(
        Guid chargeTypeId,
        CancellationToken cancellationToken = default) =>
        GetByChargeTypeIdAsync(chargeTypeId, null, cancellationToken);

    public async Task<IReadOnlyList<StudentDiscount>> GetByChargeTypeIdAsync(
        Guid chargeTypeId,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM fin.student_discounts
            WHERE charge_type_id = @ChargeTypeId
            ORDER BY student_name ASC;
            """;

        var parameters = new { ChargeTypeId = chargeTypeId };

        if (transaction is not null)
        {
            var rows = await transaction.Connection!.QueryAsync<StudentDiscountRow>(
                new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return rows.Select(MapRow).ToList();
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var directRows = await connection.QueryAsync<StudentDiscountRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return directRows.Select(MapRow).ToList();
    }

    public Task<IReadOnlyList<StudentDiscount>> GetDiscountsAsync(
        Guid? academicYearId = null,
        Guid? studentId = null,
        Guid? chargeTypeId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        GetDiscountsAsync(academicYearId, studentId, chargeTypeId, isActive, null, cancellationToken);

    public async Task<IReadOnlyList<StudentDiscount>> GetDiscountsAsync(
        Guid? academicYearId,
        Guid? studentId,
        Guid? chargeTypeId,
        bool? isActive,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM fin.student_discounts
            WHERE (@AcademicYearId IS NULL OR academic_year_id = @AcademicYearId)
              AND (@StudentId IS NULL OR student_id = @StudentId)
              AND (@ChargeTypeId IS NULL OR charge_type_id = @ChargeTypeId)
              AND (@IsActive IS NULL OR is_active = @IsActive)
            ORDER BY created_at DESC;
            """;

        var parameters = new
        {
            AcademicYearId = academicYearId,
            StudentId = studentId,
            ChargeTypeId = chargeTypeId,
            IsActive = isActive
        };

        if (transaction is not null)
        {
            var rows = await transaction.Connection!.QueryAsync<StudentDiscountRow>(
                new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return rows.Select(MapRow).ToList();
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var directRows = await connection.QueryAsync<StudentDiscountRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return directRows.Select(MapRow).ToList();
    }

    public Task InsertAsync(StudentDiscount discount, CancellationToken cancellationToken = default) =>
        InsertAsync(discount, null, cancellationToken);

    public async Task InsertAsync(
        StudentDiscount discount,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            discount.Id,
            discount.AcademicYearId,
            discount.AcademicYearName,
            discount.ChargeTypeId,
            discount.ChargeTypeName,
            discount.StudentId,
            discount.StudentName,
            discount.StudentNis,
            discount.DiscountType,
            discount.DiscountValue,
            discount.Reason,
            discount.IsActive,
            discount.CreatedAt,
            discount.UpdatedAt
        };

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

    public Task UpdateAsync(StudentDiscount discount, CancellationToken cancellationToken = default) =>
        UpdateAsync(discount, null, cancellationToken);

    public async Task UpdateAsync(
        StudentDiscount discount,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            discount.Id,
            discount.IsActive,
            discount.Reason,
            discount.UpdatedAt
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

    private static StudentDiscount MapRow(StudentDiscountRow row) =>
        StudentDiscount.Rehydrate(
            row.id,
            row.academic_year_id,
            row.academic_year_name,
            row.charge_type_id,
            row.charge_type_name,
            row.student_id,
            row.student_name,
            row.student_nis,
            row.discount_type,
            row.discount_value,
            row.reason,
            row.is_active,
            row.created_at,
            row.updated_at);

    private sealed record StudentDiscountRow(
        Guid id,
        Guid academic_year_id,
        string academic_year_name,
        Guid charge_type_id,
        string charge_type_name,
        Guid student_id,
        string student_name,
        string student_nis,
        string discount_type,
        decimal discount_value,
        string reason,
        bool is_active,
        DateTime created_at,
        DateTime updated_at);
}
