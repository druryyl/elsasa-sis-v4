using Dapper;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;
using MediatR;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.GetStudentReportCards;

public sealed class GetStudentReportCardsHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
    : IRequestHandler<GetStudentReportCardsQuery, Result<IReadOnlyList<StudentReportCardSummaryDto>>>
{
    private const string VerifyChildSql = """
        SELECT 1 
        FROM auth.user_students 
        WHERE user_id = @UserId AND student_id = @StudentId;
        """;

    private const string SelectFinalizedReportCardsSql = """
        SELECT 
            rc.id AS Id,
            rc.student_id AS StudentId,
            rc.student_name AS StudentName,
            rc.academic_year_name AS AcademicYearName,
            rc.operational_period_name AS OperationalPeriodName,
            rc.study_group_name AS StudyGroupName,
            rc.status AS Status,
            rc.finalized_at AS FinalizedAt
        FROM acd.report_cards rc
        WHERE rc.student_id = @StudentId
          AND rc.status = 'FINALIZED'
        ORDER BY rc.created_at DESC;
        """;

    private const string SelectAllReportCardsSql = """
        SELECT 
            rc.id AS Id,
            rc.student_id AS StudentId,
            rc.student_name AS StudentName,
            rc.academic_year_name AS AcademicYearName,
            rc.operational_period_name AS OperationalPeriodName,
            rc.study_group_name AS StudyGroupName,
            rc.status AS Status,
            rc.finalized_at AS FinalizedAt
        FROM acd.report_cards rc
        WHERE rc.student_id = @StudentId
        ORDER BY rc.created_at DESC;
        """;

    public async Task<Result<IReadOnlyList<StudentReportCardSummaryDto>>> Handle(
        GetStudentReportCardsQuery request,
        CancellationToken cancellationToken) =>
        await HandleAsync(request, cancellationToken);

    public async Task<Result<IReadOnlyList<StudentReportCardSummaryDto>>> HandleAsync(
        GetStudentReportCardsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.StudentId == Guid.Empty)
        {
            return Result<IReadOnlyList<StudentReportCardSummaryDto>>.Failure(
                DomainError.Validation("Acd.ReportCards.StudentIdRequired", "Student ID is required.", nameof(query.StudentId)));
        }

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var isParent = currentUser.Roles.Contains("PARENT", StringComparer.OrdinalIgnoreCase);

        if (isParent)
        {
            var isLinked = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    VerifyChildSql,
                    new { UserId = currentUser.UserId, StudentId = query.StudentId },
                    cancellationToken: cancellationToken));

            if (isLinked is null or 0)
            {
                return Result<IReadOnlyList<StudentReportCardSummaryDto>>.Failure(
                    DomainError.Forbidden("Auth.Forbidden", "You do not have permission to view report cards for this student."));
            }
        }

        var sql = isParent ? SelectFinalizedReportCardsSql : SelectAllReportCardsSql;

        var items = (await connection.QueryAsync<StudentReportCardSummaryDto>(
            new CommandDefinition(sql, new { StudentId = query.StudentId }, cancellationToken: cancellationToken))).ToList();

        return Result<IReadOnlyList<StudentReportCardSummaryDto>>.Success(items);
    }
}
