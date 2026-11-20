using Dapper;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Fin.Features.Reports.ReceivablesAging;

public sealed class GetReceivablesAgingHandler(ISqlConnectionFactory connectionFactory)
{
    private const string AgingSql = """
        SELECT
            COALESCE(SUM(CASE WHEN due_date IS NULL OR due_date >= @AsOfDate THEN outstanding_amount ELSE 0 END), 0) AS CurrentAmount,
            COALESCE(SUM(CASE WHEN due_date < @AsOfDate AND DATEDIFF(day, due_date, @AsOfDate) BETWEEN 1 AND 30 THEN outstanding_amount ELSE 0 END), 0) AS Days1To30Amount,
            COALESCE(SUM(CASE WHEN due_date < @AsOfDate AND DATEDIFF(day, due_date, @AsOfDate) BETWEEN 31 AND 60 THEN outstanding_amount ELSE 0 END), 0) AS Days31To60Amount,
            COALESCE(SUM(CASE WHEN due_date < @AsOfDate AND DATEDIFF(day, due_date, @AsOfDate) BETWEEN 61 AND 90 THEN outstanding_amount ELSE 0 END), 0) AS Days61To90Amount,
            COALESCE(SUM(CASE WHEN due_date < @AsOfDate AND DATEDIFF(day, due_date, @AsOfDate) > 90 THEN outstanding_amount ELSE 0 END), 0) AS Over90DaysAmount,
            COALESCE(SUM(outstanding_amount), 0) AS TotalOutstanding,
            COUNT(DISTINCT student_id) AS TotalStudentsCount,
            COUNT(1) AS TotalChargesCount
        FROM fin.student_charges
        WHERE status IN ('UNPAID', 'PARTIALLY_PAID')
          AND (@AcademicYearId IS NULL OR academic_year_id = @AcademicYearId)
          AND (@SchoolLevel IS NULL OR school_level = @SchoolLevel)
          AND (@Grade IS NULL OR grade = @Grade)
          AND (@StudyGroupId IS NULL OR study_group_id = @StudyGroupId);

        SELECT
            student_id AS StudentId,
            student_name AS StudentName,
            student_nis AS StudentNis,
            school_level AS SchoolLevel,
            grade AS Grade,
            study_group_id AS StudyGroupId,
            study_group_name AS StudyGroupName,
            COALESCE(SUM(CASE WHEN due_date IS NULL OR due_date >= @AsOfDate THEN outstanding_amount ELSE 0 END), 0) AS CurrentAmount,
            COALESCE(SUM(CASE WHEN due_date < @AsOfDate AND DATEDIFF(day, due_date, @AsOfDate) BETWEEN 1 AND 30 THEN outstanding_amount ELSE 0 END), 0) AS Days1To30Amount,
            COALESCE(SUM(CASE WHEN due_date < @AsOfDate AND DATEDIFF(day, due_date, @AsOfDate) BETWEEN 31 AND 60 THEN outstanding_amount ELSE 0 END), 0) AS Days31To60Amount,
            COALESCE(SUM(CASE WHEN due_date < @AsOfDate AND DATEDIFF(day, due_date, @AsOfDate) BETWEEN 61 AND 90 THEN outstanding_amount ELSE 0 END), 0) AS Days61To90Amount,
            COALESCE(SUM(CASE WHEN due_date < @AsOfDate AND DATEDIFF(day, due_date, @AsOfDate) > 90 THEN outstanding_amount ELSE 0 END), 0) AS Over90DaysAmount,
            COALESCE(SUM(outstanding_amount), 0) AS TotalOutstanding,
            COUNT(1) AS UnpaidChargeCount
        FROM fin.student_charges
        WHERE status IN ('UNPAID', 'PARTIALLY_PAID')
          AND (@AcademicYearId IS NULL OR academic_year_id = @AcademicYearId)
          AND (@SchoolLevel IS NULL OR school_level = @SchoolLevel)
          AND (@Grade IS NULL OR grade = @Grade)
          AND (@StudyGroupId IS NULL OR study_group_id = @StudyGroupId)
        GROUP BY student_id, student_name, student_nis, school_level, grade, study_group_id, study_group_name
        ORDER BY TotalOutstanding DESC, student_name ASC;
        """;

    public async Task<Result<ReceivablesAgingDto>> HandleAsync(
        GetReceivablesAgingQuery query,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var asOfDateTime = asOfDate.ToDateTime(TimeOnly.MinValue);

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            AgingSql,
            new
            {
                query.AcademicYearId,
                query.SchoolLevel,
                query.Grade,
                query.StudyGroupId,
                AsOfDate = asOfDateTime
            },
            cancellationToken: cancellationToken));

        var totals = await multi.ReadSingleAsync<AgingTotalsRow>();
        var students = (await multi.ReadAsync<ReceivablesAgingStudentItemDto>()).ToList();

        var arrearsOver30Days = totals.Days31To60Amount + totals.Days61To90Amount + totals.Over90DaysAmount;

        var summaryDto = new ReceivablesAgingSummaryDto(
            totals.CurrentAmount,
            totals.Days1To30Amount,
            totals.Days31To60Amount,
            totals.Days61To90Amount,
            totals.Over90DaysAmount,
            totals.TotalOutstanding,
            arrearsOver30Days,
            totals.TotalStudentsCount,
            totals.TotalChargesCount);

        var dto = new ReceivablesAgingDto(summaryDto, students);
        return Result<ReceivablesAgingDto>.Success(dto);
    }

    private sealed record AgingTotalsRow(
        decimal CurrentAmount,
        decimal Days1To30Amount,
        decimal Days31To60Amount,
        decimal Days61To90Amount,
        decimal Over90DaysAmount,
        decimal TotalOutstanding,
        int TotalStudentsCount,
        int TotalChargesCount);
}
