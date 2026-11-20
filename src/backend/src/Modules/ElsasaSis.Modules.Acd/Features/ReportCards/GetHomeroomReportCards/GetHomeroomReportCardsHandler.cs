using Dapper;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.GetHomeroomReportCards;

public sealed class GetHomeroomReportCardsHandler(ISqlConnectionFactory connectionFactory)
{
    private const string SelectHomeroomReportCardsSql = """
        SELECT 
            rc.id AS Id,
            rc.student_id AS StudentId,
            rc.student_name AS StudentName,
            rc.student_nis AS StudentNis,
            rc.student_nisn AS StudentNisn,
            rc.study_group_id AS StudyGroupId,
            rc.study_group_name AS StudyGroupName,
            rc.homeroom_teacher_id AS HomeroomTeacherId,
            rc.homeroom_teacher_name AS HomeroomTeacherName,
            rc.status AS Status,
            rc.finalized_at AS FinalizedAt,
            rc.override_reason AS OverrideReason,
            rc.general_remarks AS GeneralRemarks,
            COUNT(rce.id) AS TotalSubjects,
            COUNT(CASE WHEN rce.teacher_notes IS NOT NULL AND LEN(LTRIM(RTRIM(rce.teacher_notes))) > 0 THEN 1 END) AS CompletedNotesCount,
            CASE WHEN rc.general_remarks IS NOT NULL AND LEN(LTRIM(RTRIM(rc.general_remarks))) > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsHomeroomRemarksCompleted,
            ISNULL(ROUND(AVG(rce.numeric_score), 2), 0.00) AS AverageScore,
            rc.created_at AS CreatedAt,
            rc.updated_at AS UpdatedAt
        FROM acd.report_cards rc
        LEFT JOIN acd.report_card_entries rce ON rc.id = rce.report_card_id
        WHERE rc.study_group_id = @StudyGroupId
          AND rc.operational_period_id = @OperationalPeriodId
        GROUP BY 
            rc.id,
            rc.student_id,
            rc.student_name,
            rc.student_nis,
            rc.student_nisn,
            rc.study_group_id,
            rc.study_group_name,
            rc.homeroom_teacher_id,
            rc.homeroom_teacher_name,
            rc.status,
            rc.finalized_at,
            rc.override_reason,
            rc.general_remarks,
            rc.created_at,
            rc.updated_at
        ORDER BY rc.student_name;
        """;

    public async Task<Result<IReadOnlyList<HomeroomReportCardDto>>> HandleAsync(
        Guid studyGroupId,
        Guid operationalPeriodId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<HomeroomReportCardDto>(
            new CommandDefinition(
                SelectHomeroomReportCardsSql,
                new
                {
                    StudyGroupId = studyGroupId,
                    OperationalPeriodId = operationalPeriodId
                },
                cancellationToken: cancellationToken));

        return Result<IReadOnlyList<HomeroomReportCardDto>>.Success(rows.ToList());
    }
}
