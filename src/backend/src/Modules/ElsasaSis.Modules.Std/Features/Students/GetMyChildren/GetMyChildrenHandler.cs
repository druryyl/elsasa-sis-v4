using Dapper;
using ElsasaSis.Contracts.Operational;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;
using ElsasaSis.SharedKernel.Security;
using MediatR;

namespace ElsasaSis.Modules.Std.Features.Students.GetMyChildren;

public sealed class GetMyChildrenHandler(
    ICurrentUser currentUser,
    ISqlConnectionFactory connectionFactory,
    IOperationalContract operationalContract) : IRequestHandler<GetMyChildrenQuery, Result<IReadOnlyList<LinkedChildDto>>>
{
    private const string Sql = """
        SELECT 
            s.id AS StudentId,
            s.nis AS Nis,
            s.nisn AS Nisn,
            s.full_name AS FullName,
            s.nickname AS Nickname,
            s.date_of_birth AS DateOfBirth,
            s.gender AS Gender,
            sp.study_group_id AS StudyGroupId,
            sp.study_group_name AS StudyGroupName,
            sp.school_level AS SchoolLevel,
            sp.grade AS Grade
        FROM std.students s
        INNER JOIN auth.user_students us ON us.student_id = s.id
        LEFT JOIN std.student_placements sp 
               ON sp.student_id = s.id 
              AND sp.academic_year_id = @ActiveAcademicYearId
        WHERE us.user_id = @CurrentUserId
          AND s.status = 'ACTIVE'
        ORDER BY s.full_name ASC;
        """;

    public async Task<Result<IReadOnlyList<LinkedChildDto>>> Handle(
        GetMyChildrenQuery request,
        CancellationToken cancellationToken)
    {
        return await HandleAsync(request, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<LinkedChildDto>>> HandleAsync(
        GetMyChildrenQuery? request = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = currentUser.UserId;

        var activePeriod = await operationalContract.GetActiveOperationalPeriodAsync(cancellationToken);
        var activeAcademicYearId = activePeriod?.AcademicYearId;

        if (activeAcademicYearId is null)
        {
            var activeAcademicYear = await operationalContract.GetActiveAcademicYearAsync(cancellationToken);
            activeAcademicYearId = activeAcademicYear?.Id;
        }

        using var connection = connectionFactory.CreateConnection();

        var children = await connection.QueryAsync<LinkedChildDto>(
            new CommandDefinition(
                Sql,
                new
                {
                    ActiveAcademicYearId = activeAcademicYearId,
                    CurrentUserId = currentUserId
                },
                cancellationToken: cancellationToken));

        var resultList = (IReadOnlyList<LinkedChildDto>)children.ToList();
        return Result<IReadOnlyList<LinkedChildDto>>.Success(resultList);
    }
}
