using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.RemoveLearningUnit;

public sealed class RemoveLearningUnitHandler(ISubjectStageRepository stageRepository)
{
    public async Task<Result> HandleAsync(
        Guid subjectId,
        Guid stageId,
        Guid unitId,
        CancellationToken cancellationToken = default)
    {
        var stage = await stageRepository.GetByIdAsync(stageId, cancellationToken);
        if (stage is null || stage.SubjectId != subjectId)
        {
            return Result.Failure(LearningStructureErrors.SubjectStageNotFound(stageId));
        }

        var removeResult = stage.RemoveLearningUnit(unitId);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        return await stageRepository.UpdateAsync(stage, cancellationToken);
    }
}
