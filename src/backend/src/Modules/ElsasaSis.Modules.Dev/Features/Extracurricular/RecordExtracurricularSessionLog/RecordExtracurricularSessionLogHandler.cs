using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Dev.Features.Extracurricular.RecordExtracurricularSessionLog;

public sealed class RecordExtracurricularSessionLogHandler(
    IExtracurricularActivityRepository repository,
    ICurrentUser currentUser)
{
    public async Task<Result<ExtracurricularParticipationDto>> HandleAsync(
        Guid activityId,
        Guid participationId,
        RecordExtracurricularSessionLogRequest request,
        CancellationToken cancellationToken = default)
    {
        var activity = await repository.GetByIdAsync(activityId, cancellationToken);
        if (activity is null)
        {
            return Result<ExtracurricularParticipationDto>.Failure(DevErrors.ExtracurricularNotFound(activityId));
        }

        Guid callerSupervisorId;
        if (currentUser.TeacherId.HasValue && activity.SupervisorId == currentUser.TeacherId.Value)
        {
            callerSupervisorId = currentUser.TeacherId.Value;
        }
        else if (activity.SupervisorId == currentUser.UserId)
        {
            callerSupervisorId = currentUser.UserId;
        }
        else
        {
            callerSupervisorId = currentUser.TeacherId ?? currentUser.UserId;
        }

        var logResult = activity.RecordSessionLog(
            participationId,
            callerSupervisorId,
            request.SessionDate,
            request.Log);

        if (logResult.IsFailure)
        {
            return Result<ExtracurricularParticipationDto>.Failure(logResult.Error);
        }

        var participation = activity.Participations.First(p => p.Id == participationId);
        await repository.UpdateParticipationAsync(participation, cancellationToken);
        await repository.UpdateAsync(activity, cancellationToken);

        return Result<ExtracurricularParticipationDto>.Success(ExtracurricularParticipationDto.FromDomain(participation));
    }
}
