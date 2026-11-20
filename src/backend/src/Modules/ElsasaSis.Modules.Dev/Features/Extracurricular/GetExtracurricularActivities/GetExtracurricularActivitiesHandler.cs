using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Dev.Features.Extracurricular.GetExtracurricularActivities;

public sealed class GetExtracurricularActivitiesHandler(
    IExtracurricularActivityRepository repository)
{
    public async Task<Result<IReadOnlyList<ExtracurricularActivityDto>>> HandleAsync(
        GetExtracurricularActivitiesQuery query,
        CancellationToken cancellationToken = default)
    {
        var activities = await repository.GetAllAsync(
            query.Status,
            query.SupervisorId,
            cancellationToken);

        var dtos = activities
            .Select(ExtracurricularActivityDto.FromDomain)
            .ToList();

        return Result<IReadOnlyList<ExtracurricularActivityDto>>.Success(dtos);
    }
}
