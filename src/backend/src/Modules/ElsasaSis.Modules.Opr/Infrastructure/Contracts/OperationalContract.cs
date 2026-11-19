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

    private const string AcademicYearByIdSql = """
        SELECT id, name, start_date, end_date, status
        FROM opr.academic_years
        WHERE id = @Id;
        """;

    private const string OperationalPeriodByIdSql = """
        SELECT op.id, op.academic_year_id, ay.name AS academic_year_name, op.name, op.start_date, op.end_date, op.status
        FROM opr.operational_periods op
        INNER JOIN opr.academic_years ay ON ay.id = op.academic_year_id
        WHERE op.id = @Id;
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

    public async Task<AcademicYearSummaryDto?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<AcademicYearSummaryRow>(
            new CommandDefinition(AcademicYearByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null
            ? null
            : new AcademicYearSummaryDto(
                row.id,
                row.name,
                DateOnly.FromDateTime(row.start_date),
                DateOnly.FromDateTime(row.end_date),
                row.status);
    }

    public async Task<OperationalPeriodSummaryDto?> GetOperationalPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<OperationalPeriodSummaryRow>(
            new CommandDefinition(OperationalPeriodByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null
            ? null
            : new OperationalPeriodSummaryDto(
                row.id,
                row.academic_year_id,
                row.academic_year_name,
                row.name,
                DateOnly.FromDateTime(row.start_date),
                DateOnly.FromDateTime(row.end_date),
                row.status);
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

    private sealed record AcademicYearSummaryRow(
        Guid id,
        string name,
        DateTime start_date,
        DateTime end_date,
        string status);

    private sealed record OperationalPeriodSummaryRow(
        Guid id,
        Guid academic_year_id,
        string academic_year_name,
        string name,
        DateTime start_date,
        DateTime end_date,
        string status);
}
