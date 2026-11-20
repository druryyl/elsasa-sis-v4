using ElsasaSis.Contracts.Teaching;
using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Dev.Features.Extracurricular.CreateExtracurricularActivity;

public sealed class CreateExtracurricularActivityHandler(
    IExtracurricularActivityRepository repository,
    ITeachingContract teachingContract)
{
    public async Task<Result<ExtracurricularActivityDto>> HandleAsync(
        CreateExtracurricularActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return Result<ExtracurricularActivityDto>.Failure(DevErrors.ActivityNameRequired());
        }

        var isDuplicate = await repository.ExistsByNameAsync(normalizedName, excludeId: null, cancellationToken);
        if (isDuplicate)
        {
            return Result<ExtracurricularActivityDto>.Failure(DevErrors.DuplicateActivityName(normalizedName));
        }

        var supervisor = await teachingContract.GetTeacherByIdAsync(request.SupervisorId, cancellationToken);
        if (supervisor is null)
        {
            return Result<ExtracurricularActivityDto>.Failure(DevErrors.SupervisorNotFound(request.SupervisorId));
        }

        var createResult = ExtracurricularActivity.Create(
            normalizedName,
            request.Description,
            supervisor.Id,
            supervisor.FullName);

        if (createResult.IsFailure)
        {
            return Result<ExtracurricularActivityDto>.Failure(createResult.Error);
        }

        var activity = createResult.Value!;
        await repository.InsertAsync(activity, cancellationToken);

        return Result<ExtracurricularActivityDto>.Success(ExtracurricularActivityDto.FromDomain(activity));
    }
}
