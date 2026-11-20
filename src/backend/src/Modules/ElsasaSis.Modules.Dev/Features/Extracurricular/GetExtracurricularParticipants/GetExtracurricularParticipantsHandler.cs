using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Dev.Features.Extracurricular.GetExtracurricularParticipants;

public sealed class GetExtracurricularParticipantsHandler(
    IExtracurricularActivityRepository repository)
{
    public async Task<Result<IReadOnlyList<ExtracurricularParticipationDto>>> HandleAsync(
        Guid activityId,
        GetExtracurricularParticipantsQuery query,
        CancellationToken cancellationToken = default)
    {
        var activity = await repository.GetByIdAsync(activityId, cancellationToken);
        if (activity is null)
        {
            return Result<IReadOnlyList<ExtracurricularParticipationDto>>.Failure(DevErrors.ExtracurricularNotFound(activityId));
        }

        var participations = await repository.GetParticipationsByActivityIdAsync(
            activityId,
            query.AcademicYearId,
            cancellationToken);

        var dtos = participations
            .Select(ExtracurricularParticipationDto.FromDomain)
            .ToList();

        return Result<IReadOnlyList<ExtracurricularParticipationDto>>.Success(dtos);
    }
}
