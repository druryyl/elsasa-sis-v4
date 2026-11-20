using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateLearningUnit;

public sealed class UpdateLearningUnitHandler(ISubjectStageRepository stageRepository)
{
    public async Task<Result<LearningUnitDto>> HandleAsync(
        Guid subjectId,
        Guid stageId,
        Guid unitId,
        UpdateLearningUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        var stage = await stageRepository.GetByIdAsync(stageId, cancellationToken);
        if (stage is null || stage.SubjectId != subjectId)
        {
            return Result<LearningUnitDto>.Failure(LearningStructureErrors.SubjectStageNotFound(stageId));
        }

        var updateResult = stage.UpdateLearningUnit(
            unitId,
            request.Name,
            request.SequenceOrder,
            request.UnitType,
            request.Description);

        if (updateResult.IsFailure)
        {
            return Result<LearningUnitDto>.Failure(updateResult.Error);
        }

        var saveResult = await stageRepository.UpdateAsync(stage, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<LearningUnitDto>.Failure(saveResult.Error);
        }

        var updatedUnit = stage.LearningUnits.First(u => u.Id == unitId);
        return Result<LearningUnitDto>.Success(updatedUnit.ToDto());
    }
}
