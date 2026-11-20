using ElsasaSis.Contracts.Teaching;
using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Dev.Features.Extracurricular.UpdateExtracurricularActivity;

public sealed class UpdateExtracurricularActivityHandler(
    IExtracurricularActivityRepository repository,
    ITeachingContract teachingContract)
{
    public async Task<Result<ExtracurricularActivityDto>> HandleAsync(
        Guid id,
        UpdateExtracurricularActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        var activity = await repository.GetByIdAsync(id, cancellationToken);
        if (activity is null)
        {
            return Result<ExtracurricularActivityDto>.Failure(DevErrors.ExtracurricularNotFound(id));
        }

        var normalizedName = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return Result<ExtracurricularActivityDto>.Failure(DevErrors.ActivityNameRequired());
        }

        var isDuplicate = await repository.ExistsByNameAsync(normalizedName, excludeId: id, cancellationToken);
        if (isDuplicate)
        {
            return Result<ExtracurricularActivityDto>.Failure(DevErrors.DuplicateActivityName(normalizedName));
        }

        var supervisor = await teachingContract.GetTeacherByIdAsync(request.SupervisorId, cancellationToken);
        if (supervisor is null)
        {
            return Result<ExtracurricularActivityDto>.Failure(DevErrors.SupervisorNotFound(request.SupervisorId));
        }

        var updateResult = activity.UpdateDetails(
            normalizedName,
            request.Description,
            supervisor.Id,
            supervisor.FullName,
            request.Status);

        if (updateResult.IsFailure)
        {
            return Result<ExtracurricularActivityDto>.Failure(updateResult.Error);
        }

        await repository.UpdateAsync(activity, cancellationToken);

        return Result<ExtracurricularActivityDto>.Success(ExtracurricularActivityDto.FromDomain(activity));
    }
}
