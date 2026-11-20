using Dapper;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Fin.Features.Reports.ReceivablesSummary;

public sealed class GetReceivablesSummaryHandler(ISqlConnectionFactory connectionFactory)
{
    private const string SummarySql = """
        SELECT
            COALESCE(SUM(net_amount), 0) AS TotalBilled,
            COALESCE(SUM(paid_amount), 0) AS TotalCollected,
            COALESCE(SUM(outstanding_amount), 0) AS TotalOutstanding,
            COUNT(DISTINCT student_id) AS TotalStudentsCount,
            COUNT(1) AS TotalChargesCount
        FROM fin.student_charges
        WHERE status <> 'CANCELLED'
          AND (@AcademicYearId IS NULL OR academic_year_id = @AcademicYearId);

        SELECT
            academic_year_id AS AcademicYearId,
            academic_year_name AS AcademicYearName,
            school_level AS SchoolLevel,
            COALESCE(SUM(net_amount), 0) AS TotalBilled,
            COALESCE(SUM(paid_amount), 0) AS TotalCollected,
            COALESCE(SUM(outstanding_amount), 0) AS TotalOutstanding,
            COUNT(DISTINCT student_id) AS StudentCount,
            COUNT(1) AS ChargeCount
        FROM fin.student_charges
        WHERE status <> 'CANCELLED'
          AND (@AcademicYearId IS NULL OR academic_year_id = @AcademicYearId)
        GROUP BY academic_year_id, academic_year_name, school_level
        ORDER BY academic_year_name, school_level;
        """;

    public async Task<Result<ReceivablesSummaryDto>> HandleAsync(
        GetReceivablesSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(SummarySql, new { query.AcademicYearId }, cancellationToken: cancellationToken));

        var totals = await multi.ReadSingleAsync<TotalsRow>();
        var breakdown = (await multi.ReadAsync<ReceivablesSummaryByLevelDto>()).ToList();

        var dto = new ReceivablesSummaryDto(
            totals.TotalBilled,
            totals.TotalCollected,
            totals.TotalOutstanding,
            totals.TotalStudentsCount,
            totals.TotalChargesCount,
            breakdown);

        return Result<ReceivablesSummaryDto>.Success(dto);
    }

    private sealed record TotalsRow(
        decimal TotalBilled,
        decimal TotalCollected,
        decimal TotalOutstanding,
        int TotalStudentsCount,
        int TotalChargesCount);
}
