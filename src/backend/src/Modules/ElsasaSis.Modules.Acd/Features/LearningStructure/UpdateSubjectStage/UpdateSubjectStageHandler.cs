using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateSubjectStage;

public sealed class UpdateSubjectStageHandler(ISubjectStageRepository stageRepository)
{
    public async Task<Result<SubjectStageDto>> HandleAsync(
        Guid subjectId,
        Guid stageId,
        UpdateSubjectStageRequest request,
        CancellationToken cancellationToken = default)
    {
        var stage = await stageRepository.GetByIdAsync(stageId, cancellationToken);
        if (stage is null || stage.SubjectId != subjectId)
        {
            return Result<SubjectStageDto>.Failure(LearningStructureErrors.SubjectStageNotFound(stageId));
        }

        var trimmedName = request.Name?.Trim() ?? string.Empty;
        if (await stageRepository.ExistsByNameAsync(subjectId, trimmedName, excludeId: stageId, cancellationToken))
        {
            return Result<SubjectStageDto>.Failure(LearningStructureErrors.DuplicateStageName(trimmedName));
        }

        if (await stageRepository.ExistsBySequenceOrderAsync(subjectId, request.SequenceOrder, excludeId: stageId, cancellationToken))
        {
            return Result<SubjectStageDto>.Failure(LearningStructureErrors.DuplicateStageSequence(request.SequenceOrder));
        }

        var updateResult = stage.Update(request.Name, request.SequenceOrder, request.Description);
        if (updateResult.IsFailure)
        {
            return Result<SubjectStageDto>.Failure(updateResult.Error);
        }

        var saveResult = await stageRepository.UpdateAsync(stage, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<SubjectStageDto>.Failure(saveResult.Error);
        }

        return Result<SubjectStageDto>.Success(stage.ToDto());
    }
}
