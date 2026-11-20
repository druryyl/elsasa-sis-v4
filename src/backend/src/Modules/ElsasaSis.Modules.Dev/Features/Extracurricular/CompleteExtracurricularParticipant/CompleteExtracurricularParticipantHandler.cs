using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Dev.Features.Extracurricular.CompleteExtracurricularParticipant;

public sealed class CompleteExtracurricularParticipantHandler(
    IExtracurricularActivityRepository repository)
{
    public async Task<Result<ExtracurricularParticipationDto>> HandleAsync(
        Guid activityId,
        Guid participationId,
        CancellationToken cancellationToken = default)
    {
        var activity = await repository.GetByIdAsync(activityId, cancellationToken);
        if (activity is null)
        {
            return Result<ExtracurricularParticipationDto>.Failure(DevErrors.ExtracurricularNotFound(activityId));
        }

        var completeResult = activity.CompleteStudent(participationId);
        if (completeResult.IsFailure)
        {
            return Result<ExtracurricularParticipationDto>.Failure(completeResult.Error);
        }

        var participation = activity.Participations.First(p => p.Id == participationId);
        await repository.UpdateParticipationAsync(participation, cancellationToken);
        await repository.UpdateAsync(activity, cancellationToken);

        return Result<ExtracurricularParticipationDto>.Success(ExtracurricularParticipationDto.FromDomain(participation));
    }
}
