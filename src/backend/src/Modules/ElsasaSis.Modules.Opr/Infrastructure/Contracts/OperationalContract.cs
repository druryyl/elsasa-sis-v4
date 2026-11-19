using Dapper;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Opr.Infrastructure.Contracts;

public sealed class OperationalContract(ISqlConnectionFactory connectionFactory) : IOperationalContract
{
    private const string ActiveAcademicYearSql = """
        SELECT TOP (1) id, name, start_date, end_date
        FROM opr.academic_years
        WHERE status = 'ACTIVE';
        """;

    private const string ActiveOperationalPeriodSql = """
        SELECT TOP (1) op.id, op.academic_year_id, op.name, op.start_date, op.end_date
        FROM opr.operational_periods op
        INNER JOIN opr.academic_years ay ON ay.id = op.academic_year_id
        WHERE op.status = 'ACTIVE' AND ay.status = 'ACTIVE';
        """;

    public async Task<ActiveAcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ActiveAcademicYearRow>(
            new CommandDefinition(ActiveAcademicYearSql, cancellationToken: cancellationToken));

        return row is null
            ? null
            : new ActiveAcademicYearDto(
                row.id,
                row.name,
                DateOnly.FromDateTime(row.start_date),
                DateOnly.FromDateTime(row.end_date));
    }

    public async Task<ActiveOperationalPeriodDto?> GetActiveOperationalPeriodAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ActiveOperationalPeriodRow>(
            new CommandDefinition(ActiveOperationalPeriodSql, cancellationToken: cancellationToken));

        return row is null
            ? null
            : new ActiveOperationalPeriodDto(
                row.id,
                row.academic_year_id,
                row.name,
                DateOnly.FromDateTime(row.start_date),
                DateOnly.FromDateTime(row.end_date));
    }

    private sealed record ActiveAcademicYearRow(
        Guid id,
        string name,
        DateTime start_date,
        DateTime end_date);

    private sealed record ActiveOperationalPeriodRow(
        Guid id,
        Guid academic_year_id,
        string name,
        DateTime start_date,
        DateTime end_date);
}
