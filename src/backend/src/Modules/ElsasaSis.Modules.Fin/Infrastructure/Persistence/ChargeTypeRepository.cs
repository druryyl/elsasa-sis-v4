using System.Data;
using Dapper;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Fin.Infrastructure.Persistence;

public sealed class ChargeTypeRepository(ISqlConnectionFactory connectionFactory) : IChargeTypeRepository
{
    private const string SelectColumns = """
        id, academic_year_id, academic_year_name, name, frequency, amount,
        school_level, grade, study_group_id, study_group_name, status,
        created_at, updated_at
        """;

    private const string SelectByIdSql = $"""
        SELECT {SelectColumns}
        FROM fin.charge_types
        WHERE id = @Id;
        """;

    private const string ExistsByNameInYearSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM fin.charge_types
            WHERE academic_year_id = @AcademicYearId
              AND LOWER(LTRIM(RTRIM(name))) = LOWER(LTRIM(RTRIM(@Name)))
              AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string InsertSql = """
        INSERT INTO fin.charge_types (
            id, academic_year_id, academic_year_name, name, frequency, amount,
            school_level, grade, study_group_id, study_group_name, status,
            created_at, updated_at
        ) VALUES (
            @Id, @AcademicYearId, @AcademicYearName, @Name, @Frequency, @Amount,
            @SchoolLevel, @Grade, @StudyGroupId, @StudyGroupName, @Status,
            @CreatedAt, @UpdatedAt
        );
        """;

    private const string UpdateSql = """
        UPDATE fin.charge_types
        SET name = @Name,
            frequency = @Frequency,
            amount = @Amount,
            school_level = @SchoolLevel,
            grade = @Grade,
            study_group_id = @StudyGroupId,
            study_group_name = @StudyGroupName,
            status = @Status,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    public Task<ChargeType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, null, cancellationToken);

    public async Task<ChargeType?> GetByIdAsync(
        Guid id,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        if (transaction is not null)
        {
            var row = await transaction.Connection!.QuerySingleOrDefaultAsync<ChargeTypeRow>(
                new CommandDefinition(SelectByIdSql, new { Id = id }, transaction: transaction, cancellationToken: cancellationToken));
            return row is null ? null : MapRow(row);
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var directRow = await connection.QuerySingleOrDefaultAsync<ChargeTypeRow>(
            new CommandDefinition(SelectByIdSql, new { Id = id }, cancellationToken: cancellationToken));
        return directRow is null ? null : MapRow(directRow);
    }

    public Task<IReadOnlyList<ChargeType>> GetByAcademicYearAsync(
        Guid? academicYearId = null,
        string? status = null,
        string? frequency = null,
        CancellationToken cancellationToken = default) =>
        GetByAcademicYearAsync(academicYearId, status, frequency, null, cancellationToken);

    public async Task<IReadOnlyList<ChargeType>> GetByAcademicYearAsync(
        Guid? academicYearId,
        string? status,
        string? frequency,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM fin.charge_types
            WHERE (@AcademicYearId IS NULL OR academic_year_id = @AcademicYearId)
              AND (@Status IS NULL OR status = @Status)
              AND (@Frequency IS NULL OR frequency = @Frequency)
            ORDER BY name ASC;
            """;

        var parameters = new
        {
            AcademicYearId = academicYearId,
            Status = status,
            Frequency = frequency
        };

        if (transaction is not null)
        {
            var rows = await transaction.Connection!.QueryAsync<ChargeTypeRow>(
                new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return rows.Select(MapRow).ToList();
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var directRows = await connection.QueryAsync<ChargeTypeRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return directRows.Select(MapRow).ToList();
    }

    public Task<IReadOnlyList<ChargeType>> GetActiveByScopeAsync(
        Guid academicYearId,
        string? schoolLevel = null,
        string? grade = null,
        Guid? studyGroupId = null,
        CancellationToken cancellationToken = default) =>
        GetActiveByScopeAsync(academicYearId, schoolLevel, grade, studyGroupId, null, cancellationToken);

    public async Task<IReadOnlyList<ChargeType>> GetActiveByScopeAsync(
        Guid academicYearId,
        string? schoolLevel,
        string? grade,
        Guid? studyGroupId,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM fin.charge_types
            WHERE academic_year_id = @AcademicYearId
              AND status = 'ACTIVE'
              AND (school_level IS NULL OR school_level = @SchoolLevel)
              AND (grade IS NULL OR grade = @Grade)
              AND (study_group_id IS NULL OR study_group_id = @StudyGroupId)
            ORDER BY name ASC;
            """;

        var parameters = new
        {
            AcademicYearId = academicYearId,
            SchoolLevel = schoolLevel,
            Grade = grade,
            StudyGroupId = studyGroupId
        };

        if (transaction is not null)
        {
            var rows = await transaction.Connection!.QueryAsync<ChargeTypeRow>(
                new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return rows.Select(MapRow).ToList();
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var directRows = await connection.QueryAsync<ChargeTypeRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return directRows.Select(MapRow).ToList();
    }

    public Task<bool> ExistsByNameInYearAsync(
        string name,
        Guid academicYearId,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        ExistsByNameInYearAsync(name, academicYearId, excludeId, null, cancellationToken);

    public async Task<bool> ExistsByNameInYearAsync(
        string name,
        Guid academicYearId,
        Guid? excludeId,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            Name = name,
            AcademicYearId = academicYearId,
            ExcludeId = excludeId
        };

        if (transaction is not null)
        {
            return await transaction.Connection!.ExecuteScalarAsync<bool>(
                new CommandDefinition(ExistsByNameInYearSql, parameters, transaction: transaction, cancellationToken: cancellationToken));
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(ExistsByNameInYearSql, parameters, cancellationToken: cancellationToken));
    }

    public Task InsertAsync(ChargeType chargeType, CancellationToken cancellationToken = default) =>
        InsertAsync(chargeType, null, cancellationToken);

    public async Task InsertAsync(
        ChargeType chargeType,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            chargeType.Id,
            chargeType.AcademicYearId,
            chargeType.AcademicYearName,
            chargeType.Name,
            chargeType.Frequency,
            chargeType.Amount,
            chargeType.SchoolLevel,
            chargeType.Grade,
            chargeType.StudyGroupId,
            chargeType.StudyGroupName,
            chargeType.Status,
            chargeType.CreatedAt,
            chargeType.UpdatedAt
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

    public Task UpdateAsync(ChargeType chargeType, CancellationToken cancellationToken = default) =>
        UpdateAsync(chargeType, null, cancellationToken);

    public async Task UpdateAsync(
        ChargeType chargeType,
        IDbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            chargeType.Id,
            chargeType.Name,
            chargeType.Frequency,
            chargeType.Amount,
            chargeType.SchoolLevel,
            chargeType.Grade,
            chargeType.StudyGroupId,
            chargeType.StudyGroupName,
            chargeType.Status,
            chargeType.UpdatedAt
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

    private static ChargeType MapRow(ChargeTypeRow row) =>
        ChargeType.Rehydrate(
            row.id,
            row.academic_year_id,
            row.academic_year_name,
            row.name,
            row.frequency,
            row.amount,
            row.school_level,
            row.grade,
            row.study_group_id,
            row.study_group_name,
            row.status,
            row.created_at,
            row.updated_at);

    private sealed record ChargeTypeRow(
        Guid id,
        Guid academic_year_id,
        string academic_year_name,
        string name,
        string frequency,
        decimal amount,
        string? school_level,
        string? grade,
        Guid? study_group_id,
        string? study_group_name,
        string status,
        DateTime created_at,
        DateTime updated_at);
}
