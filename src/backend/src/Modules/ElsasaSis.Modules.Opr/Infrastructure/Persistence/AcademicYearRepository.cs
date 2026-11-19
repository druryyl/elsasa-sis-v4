using System.Data;
using System.Data.Common;
using Dapper;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Opr.Infrastructure.Persistence;

public sealed class AcademicYearRepository(ISqlConnectionFactory connectionFactory) : IAcademicYearRepository
{
    private const string SelectYearSql = """
        SELECT id, name, start_date, end_date, status, created_at, updated_at
        FROM opr.academic_years
        WHERE id = @Id;
        """;

    private const string SelectPeriodsSql = """
        SELECT id, academic_year_id, name, start_date, end_date, status, created_at, updated_at
        FROM opr.operational_periods
        WHERE academic_year_id = @AcademicYearId
        ORDER BY start_date ASC, end_date ASC, name ASC;
        """;

    private const string ExistsByNameSql = """
        SELECT CASE WHEN EXISTS (
            SELECT 1
            FROM opr.academic_years
            WHERE LOWER(LTRIM(RTRIM(name))) = LOWER(LTRIM(RTRIM(@Name)))
              AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        ) THEN 1 ELSE 0 END;
        """;

    private const string InsertYearSql = """
        INSERT INTO opr.academic_years (id, name, start_date, end_date, status, created_at, updated_at)
        VALUES (@Id, @Name, @StartDate, @EndDate, @Status, @CreatedAt, @UpdatedAt);
        """;

    private const string InsertPeriodSql = """
        INSERT INTO opr.operational_periods (id, academic_year_id, name, start_date, end_date, status, created_at, updated_at)
        VALUES (@Id, @AcademicYearId, @Name, @StartDate, @EndDate, @Status, @CreatedAt, @UpdatedAt);
        """;

    private const string UpdateYearSql = """
        UPDATE opr.academic_years
        SET name = @Name,
            start_date = @StartDate,
            end_date = @EndDate,
            status = @Status,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string DeletePeriodsByYearSql = """
        DELETE FROM opr.operational_periods
        WHERE academic_year_id = @AcademicYearId;
        """;

    private const string DeactivateOtherYearsSql = """
        UPDATE opr.academic_years
        SET status = 'INACTIVE',
            updated_at = @UpdatedAt
        WHERE status = 'ACTIVE' AND id <> @Id;
        """;

    private const string DeactivateOtherYearPeriodsSql = """
        UPDATE opr.operational_periods
        SET status = 'INACTIVE',
            updated_at = @UpdatedAt
        WHERE status = 'ACTIVE' AND academic_year_id <> @Id;
        """;

    private const string UpdateYearStatusSql = """
        UPDATE opr.academic_years
        SET status = @Status,
            updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    private const string UpdatePeriodStatusSql = """
        UPDATE opr.operational_periods
        SET status = @Status,
            updated_at = @UpdatedAt
        WHERE id = @Id AND academic_year_id = @AcademicYearId;
        """;

    public async Task<AcademicYear?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var yearRow = await connection.QuerySingleOrDefaultAsync<AcademicYearRow>(
            new CommandDefinition(SelectYearSql, new { Id = id }, cancellationToken: cancellationToken));

        if (yearRow is null)
        {
            return null;
        }

        var periodRows = await connection.QueryAsync<OperationalPeriodRow>(
            new CommandDefinition(SelectPeriodsSql, new { AcademicYearId = id }, cancellationToken: cancellationToken));

        var periods = periodRows.Select(p => OperationalPeriod.Rehydrate(
            p.id,
            p.academic_year_id,
            p.name,
            DateOnly.FromDateTime(p.start_date),
            DateOnly.FromDateTime(p.end_date),
            Enum.Parse<OperationalPeriodStatus>(p.status, ignoreCase: true),
            p.created_at,
            p.updated_at));

        return AcademicYear.Rehydrate(
            yearRow.id,
            yearRow.name,
            DateOnly.FromDateTime(yearRow.start_date),
            DateOnly.FromDateTime(yearRow.end_date),
            Enum.Parse<AcademicYearStatus>(yearRow.status, ignoreCase: true),
            yearRow.created_at,
            yearRow.updated_at,
            periods);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(ExistsByNameSql, new { Name = name, ExcludeId = excludeId }, cancellationToken: cancellationToken));
    }

    public Task AddAsync(AcademicYear academicYear, CancellationToken cancellationToken = default) =>
        ExecuteInTransactionAsync(async (connection, transaction, ct) =>
        {
            await connection.ExecuteAsync(new CommandDefinition(
                InsertYearSql,
                ToYearParameters(academicYear),
                transaction,
                cancellationToken: ct));

            foreach (var period in academicYear.Periods)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    InsertPeriodSql,
                    ToPeriodParameters(period),
                    transaction,
                    cancellationToken: ct));
            }
        }, cancellationToken);

    public Task UpdateAsync(AcademicYear academicYear, CancellationToken cancellationToken = default) =>
        ExecuteInTransactionAsync(async (connection, transaction, ct) =>
        {
            await connection.ExecuteAsync(new CommandDefinition(
                UpdateYearSql,
                ToYearParameters(academicYear),
                transaction,
                cancellationToken: ct));

            await connection.ExecuteAsync(new CommandDefinition(
                DeletePeriodsByYearSql,
                new { AcademicYearId = academicYear.Id },
                transaction,
                cancellationToken: ct));

            foreach (var period in academicYear.Periods)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    InsertPeriodSql,
                    ToPeriodParameters(period),
                    transaction,
                    cancellationToken: ct));
            }
        }, cancellationToken);

    public Task SaveActivationAsync(AcademicYear academicYear, CancellationToken cancellationToken = default) =>
        ExecuteInTransactionAsync(async (connection, transaction, ct) =>
        {
            await connection.ExecuteAsync(new CommandDefinition(
                DeactivateOtherYearsSql,
                new { academicYear.Id, academicYear.UpdatedAt },
                transaction,
                cancellationToken: ct));

            await connection.ExecuteAsync(new CommandDefinition(
                DeactivateOtherYearPeriodsSql,
                new { academicYear.Id, academicYear.UpdatedAt },
                transaction,
                cancellationToken: ct));

            await connection.ExecuteAsync(new CommandDefinition(
                UpdateYearStatusSql,
                new
                {
                    academicYear.Id,
                    Status = ToStatusString(academicYear.Status),
                    academicYear.UpdatedAt
                },
                transaction,
                cancellationToken: ct));

            foreach (var period in academicYear.Periods)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    UpdatePeriodStatusSql,
                    new
                    {
                        period.Id,
                        period.AcademicYearId,
                        Status = ToStatusString(period.Status),
                        period.UpdatedAt
                    },
                    transaction,
                    cancellationToken: ct));
            }
        }, cancellationToken);

    private async Task ExecuteInTransactionAsync(
        Func<IDbConnection, IDbTransaction, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (connection is DbConnection dbConnection)
        {
            await using var transaction = await dbConnection.BeginTransactionAsync(cancellationToken);
            try
            {
                await action(connection, transaction, cancellationToken);
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
                await action(connection, transaction, cancellationToken);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    private static object ToYearParameters(AcademicYear academicYear) => new
    {
        academicYear.Id,
        academicYear.Name,
        StartDate = academicYear.StartDate.ToDateTime(TimeOnly.MinValue),
        EndDate = academicYear.EndDate.ToDateTime(TimeOnly.MinValue),
        Status = ToStatusString(academicYear.Status),
        academicYear.CreatedAt,
        academicYear.UpdatedAt
    };

    private static object ToPeriodParameters(OperationalPeriod period) => new
    {
        period.Id,
        period.AcademicYearId,
        period.Name,
        StartDate = period.StartDate.ToDateTime(TimeOnly.MinValue),
        EndDate = period.EndDate.ToDateTime(TimeOnly.MinValue),
        Status = ToStatusString(period.Status),
        period.CreatedAt,
        period.UpdatedAt
    };

    private static string ToStatusString(AcademicYearStatus status) => status.ToString().ToUpperInvariant();
    private static string ToStatusString(OperationalPeriodStatus status) => status.ToString().ToUpperInvariant();

    private sealed record AcademicYearRow(
        Guid id,
        string name,
        DateTime start_date,
        DateTime end_date,
        string status,
        DateTime created_at,
        DateTime updated_at);

    private sealed record OperationalPeriodRow(
        Guid id,
        Guid academic_year_id,
        string name,
        DateTime start_date,
        DateTime end_date,
        string status,
        DateTime created_at,
        DateTime updated_at);
}
