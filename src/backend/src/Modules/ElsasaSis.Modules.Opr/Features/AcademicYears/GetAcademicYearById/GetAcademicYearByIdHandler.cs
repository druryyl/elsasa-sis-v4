using Dapper;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Opr.Features.AcademicYears.GetAcademicYearById;

public sealed class GetAcademicYearByIdHandler(ISqlConnectionFactory connectionFactory)
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

    public async Task<Result<AcademicYearDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var yearRow = await connection.QuerySingleOrDefaultAsync<AcademicYearRow>(
            new CommandDefinition(SelectYearSql, new { Id = id }, cancellationToken: cancellationToken));

        if (yearRow is null)
        {
            return Result<AcademicYearDto>.Failure(OprErrors.NotFound(id));
        }

        var periodRows = await connection.QueryAsync<OperationalPeriodRow>(
            new CommandDefinition(SelectPeriodsSql, new { AcademicYearId = id }, cancellationToken: cancellationToken));

        var periods = periodRows
            .Select(p => new OperationalPeriodDto(
                p.id,
                p.academic_year_id,
                p.name,
                DateOnly.FromDateTime(p.start_date),
                DateOnly.FromDateTime(p.end_date),
                p.status,
                p.created_at,
                p.updated_at))
            .ToArray();

        return Result<AcademicYearDto>.Success(new AcademicYearDto(
            yearRow.id,
            yearRow.name,
            DateOnly.FromDateTime(yearRow.start_date),
            DateOnly.FromDateTime(yearRow.end_date),
            yearRow.status,
            yearRow.created_at,
            yearRow.updated_at,
            periods));
    }

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
