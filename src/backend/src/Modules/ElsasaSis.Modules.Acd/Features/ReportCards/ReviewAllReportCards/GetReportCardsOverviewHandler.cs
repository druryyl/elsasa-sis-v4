using Dapper;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.ReviewAllReportCards;

public sealed class GetReportCardsOverviewHandler(ISqlConnectionFactory connectionFactory)
{
    private const string SelectOverviewItemsSql = """
        SELECT 
            rc.id AS Id,
            rc.student_id AS StudentId,
            rc.student_name AS StudentName,
            rc.student_nis AS StudentNis,
            rc.study_group_id AS StudyGroupId,
            rc.study_group_name AS StudyGroupName,
            rc.homeroom_teacher_id AS HomeroomTeacherId,
            rc.homeroom_teacher_name AS HomeroomTeacherName,
            rc.status AS Status,
            rc.finalized_at AS FinalizedAt,
            ISNULL(ROUND(AVG(rce.numeric_score), 2), 0.00) AS AverageScore,
            COUNT(rce.id) AS TotalSubjects,
            COUNT(CASE WHEN rce.teacher_notes IS NOT NULL AND LEN(LTRIM(RTRIM(rce.teacher_notes))) > 0 THEN 1 END) AS CompletedNotesCount,
            CASE WHEN rc.general_remarks IS NOT NULL AND LEN(LTRIM(RTRIM(rc.general_remarks))) > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsHomeroomRemarksCompleted
        FROM acd.report_cards rc
        LEFT JOIN acd.report_card_entries rce ON rc.id = rce.report_card_id
        WHERE rc.operational_period_id = @OperationalPeriodId
          AND (@StudyGroupId IS NULL OR rc.study_group_id = @StudyGroupId)
        GROUP BY 
            rc.id,
            rc.student_id,
            rc.student_name,
            rc.student_nis,
            rc.study_group_id,
            rc.study_group_name,
            rc.homeroom_teacher_id,
            rc.homeroom_teacher_name,
            rc.status,
            rc.finalized_at,
            rc.general_remarks
        ORDER BY rc.study_group_name, rc.student_name;
        """;

    public async Task<Result<ReportCardsOverviewDto>> HandleAsync(
        Guid operationalPeriodId,
        Guid? studyGroupId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = (await connection.QueryAsync<ReportCardOverviewItemDto>(
            new CommandDefinition(
                SelectOverviewItemsSql,
                new
                {
                    OperationalPeriodId = operationalPeriodId,
                    StudyGroupId = studyGroupId
                },
                cancellationToken: cancellationToken))).ToList();

        var total = rows.Count;
        var draft = rows.Count(r => string.Equals(r.Status, "DRAFT", StringComparison.OrdinalIgnoreCase));
        var finalized = rows.Count(r => string.Equals(r.Status, "FINALIZED", StringComparison.OrdinalIgnoreCase));
        var overallAvg = total > 0 ? Math.Round(rows.Average(r => r.AverageScore), 2) : 0.00m;

        var overview = new ReportCardsOverviewDto(
            total,
            draft,
            finalized,
            overallAvg,
            rows);

        return Result<ReportCardsOverviewDto>.Success(overview);
    }
}
