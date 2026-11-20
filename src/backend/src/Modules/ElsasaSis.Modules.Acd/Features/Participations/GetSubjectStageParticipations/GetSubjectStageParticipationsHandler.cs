using Dapper;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Acd.Features.Participations.GetSubjectStageParticipations;

public sealed class GetSubjectStageParticipationsHandler(ISqlConnectionFactory connectionFactory)
{
    private const string QuerySql = """
        SELECT
            id AS Id,
            student_id AS StudentId,
            student_name AS StudentName,
            student_nis AS StudentNis,
            subject_stage_id AS SubjectStageId,
            subject_stage_name AS SubjectStageName,
            subject_id AS SubjectId,
            subject_name AS SubjectName,
            operational_period_id AS OperationalPeriodId,
            operational_period_name AS OperationalPeriodName,
            academic_year_id AS AcademicYearId,
            academic_year_name AS AcademicYearName,
            study_group_id AS StudyGroupId,
            study_group_name AS StudyGroupName,
            status AS Status,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt
        FROM acd.academic_participations
        WHERE (@SubjectStageId IS NULL OR subject_stage_id = @SubjectStageId)
          AND (@StudyGroupId IS NULL OR study_group_id = @StudyGroupId)
          AND (@OperationalPeriodId IS NULL OR operational_period_id = @OperationalPeriodId)
        ORDER BY student_name ASC;
        """;

    public async Task<IReadOnlyList<SubjectStageParticipationDto>> HandleAsync(
        Guid? subjectStageId,
        Guid? studyGroupId,
        Guid? operationalPeriodId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<SubjectStageParticipationDto>(
            new CommandDefinition(
                QuerySql,
                new
                {
                    SubjectStageId = subjectStageId,
                    StudyGroupId = studyGroupId,
                    OperationalPeriodId = operationalPeriodId
                },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }
}
